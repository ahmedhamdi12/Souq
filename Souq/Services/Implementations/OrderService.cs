using Stripe;
using Stripe.Checkout;
using Souq.Models;
using Souq.Models.Enums;
using Souq.Services.Interfaces;
using Souq.UnitOfWork;
using Souq.ViewModels.Checkout;
using Microsoft.Extensions.Configuration;
using Stripe;
using Souq.ViewModels.Orders;
using Microsoft.AspNetCore.Identity;

namespace Souq.Services.Implementations
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICartService _cartService;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderService(IUnitOfWork uow,
                           ICartService cartService,
                           IConfiguration config,
                           IEmailService emailService,
                           UserManager<ApplicationUser> userManager)
        {
            _uow = uow;
            _cartService = cartService;
            _config = config;
            _emailService = emailService;
            _userManager = userManager;
        }


        // ── Create Order ─────────────────────────────────────
        public async Task<Order> CreateOrderAsync(
            CheckoutViewModel model, string userId)
        {
            /*
                STEP 1 — Get the cart.
                We only create orders for logged-in users.
                Guest checkout is not supported in v1.
            */
            var cart = await _cartService.GetCartAsync(userId, null);

            if (cart.IsEmpty)
                throw new InvalidOperationException("Cart is empty");

            /*
                STEP 2 — Calculate totals.
                Platform takes 10% of each order.
                Vendor gets 90%.
                These percentages come from config ideally,
                hardcoded for now.
            */
            const decimal platformFeePercent = 0.10m;
            var subTotal = cart.Subtotal;
            var platformFee = Math.Round(subTotal * platformFeePercent, 2);
            var total = subTotal;

            /*
                STEP 3 — Generate unique order number.
                Format: SOUQ-{timestamp}{random4digits}
                e.g. SOUQ-20240506-1234
            */
            var orderNumber = $"SOUQ-{DateTime.UtcNow:yyyyMMdd}-" +
                              $"{new Random().Next(1000, 9999)}";

            /*
                STEP 4 — Build the Order entity.
                We snapshot product names and prices here.
                If vendor changes price tomorrow, this order
                still shows what customer paid today.
            */
            var order = new Order
            {
                UserId = userId,
                OrderNumber = orderNumber,
                Status = OrderStatus.Draft,
                SubTotal = subTotal,
                PlatformFee = platformFee,
                Total = total,
                CreatedAt = DateTime.UtcNow,
                ShippingFullName = model.FullName,
                ShippingAddressLine = model.Address,
                ShippingCity = model.City,
                ShippingCountry = model.Country,
                ShippingPhone = model.Phone,
                OrderItems = cart.Items.Select(item => new OrderItem
                {
                    VariationId = item.VariationId,
                    VendorId = GetVendorIdForVariation(item.VariationId),
                    ProductName = item.ProductName,
                    VariationName = item.VariationName,
                    ImageUrl = item.ImageUrl,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    /*
                        Vendor earns 90% of their items' total.
                        Platform keeps 10%.
                    */
                    VendorEarnings = Math.Round(
                        item.LineTotal * (1 - platformFeePercent), 2),
                    Status = OrderStatus.Paid
                }).ToList()
            };

            await _uow.Orders.AddAsync(order);
            await _uow.SaveAsync();

            return order;
        }

        // ── Create Stripe Session ────────────────────────────
        public async Task<string> CreateStripeSessionAsync(
            Order order, string successUrl, string cancelUrl)
        {
            /*
                We load the full order with items to build
                the Stripe line items list.
            */
            var fullOrder = await _uow.Orders.GetOrderWithItemsAsync(order.Id);

            /*
                Build Stripe line items — one per order item.
                Stripe displays these on their checkout page.
                Amount is in CENTS (multiply by 100).
            */
            var lineItems = fullOrder!.OrderItems.Select(item =>
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        UnitAmount = (long)(item.UnitPrice * 100),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"{item.ProductName} — {item.VariationName}",
                            /*
                                Stripe requires fully-qualified absolute URLs
                                for product images (https://...).
                                Our DB stores relative paths like /uploads/...,
                                so we skip images that aren't valid absolute URLs.
                            */
                            Images = item.ImageUrl != null
                                      && Uri.IsWellFormedUriString(item.ImageUrl, UriKind.Absolute)
                                ? new List<string> { item.ImageUrl }
                                : null
                        }
                    },
                    Quantity = item.Quantity
                }).ToList();

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = lineItems,
                Mode = "payment",

                /*
                    We append the orderId to the success URL.
                    After payment, Stripe redirects to:
                    /checkout/success?orderId=123&session_id={CHECKOUT_SESSION_ID}
                    {CHECKOUT_SESSION_ID} is replaced by Stripe automatically.
                */
                SuccessUrl = $"{successUrl}?orderId={order.Id}" +
                             "&session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = cancelUrl,

                /*
                    Store our orderId in Stripe's metadata.
                    This lets us find the order when webhook fires.
                */
                Metadata = new Dictionary<string, string>
                {
                    { "orderId", order.Id.ToString() }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            /*
                Save the Stripe session ID to our order.
                We need this to verify the webhook later.
            */
            fullOrder.StripeSessionId = session.Id;
            _uow.Orders.Update(fullOrder);
            await _uow.SaveAsync();

            return session.Url;
        }

        // ── Handle Webhook ───────────────────────────────────
        public async Task HandleWebhookAsync(
            string json, string stripeSignature)
        {
            /*
                Stripe signs every webhook with a secret key.
                We verify this signature to ensure the request
                actually came from Stripe and not an attacker.
            */
            var webhookSecret = _config["Stripe:WebhookSecret"];

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    json, stripeSignature, webhookSecret);
            }
            catch (StripeException)
            {
                throw new InvalidOperationException("Invalid webhook signature");
            }

            /*
                We only care about one event type:
                checkout.session.completed = payment succeeded.
                There are many other events (refunds, disputes etc)
                we'll handle in a later phase.
            */
            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;
                if (session == null) return;

                /*
                    Find our order using the Stripe session ID
                    we saved when creating the session.
                */
                var order = await _uow.Orders
                    .GetByStripeSessionAsync(session.Id);

                if (order == null) return;

                // Mark as paid
                order.Status = OrderStatus.Paid;
                order.StripePaymentIntentId = session.PaymentIntentId;
                order.PaidAt = DateTime.UtcNow;

                _uow.Orders.Update(order);
                await _uow.SaveAsync();

                // Clear the user's cart after successful payment
                await _cartService.ClearCartAsync(order.UserId);

                /*
                    Send confirmation email to customer.
                    We wrap in try-catch so email failure
                    never breaks the payment flow.
                */
                try
                {
                    var customer = await _userManager.FindByIdAsync(order.UserId);
                    if (customer == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Webhook] Email Failed: User with ID {order.UserId} not found.");
                    }
                    else if (customer.Email != null)
                    {
                        await _emailService.SendOrderConfirmedAsync(
                            toEmail: customer.Email,
                            customerName: customer.FirstName,
                            orderNumber: order.OrderNumber,
                            total: order.Total,
                            city: order.ShippingCity,
                            country: order.ShippingCountry);

                        System.Diagnostics.Debug.WriteLine($"[Webhook] Confirmation email sent to {customer.Email}");
                    }

                    // Send new sale emails to vendors
                    var fullOrder = await _uow.Orders.GetOrderWithItemsAsync(order.Id);
                    if (fullOrder != null)
                    {
                        var vendorGroups = fullOrder.OrderItems.GroupBy(i => i.VendorId);
                        foreach (var group in vendorGroups)
                        {
                            var vendor = await _uow.Vendors.GetByIdAsync(group.Key);
                            if (vendor?.User?.Email != null)
                            {
                                var earnings = group.Sum(i => i.VendorEarnings);
                                var itemNames = group.Select(i => $"{i.ProductName} x{i.Quantity}").ToList();

                                await _emailService.SendNewSaleAsync(
                                    toEmail: vendor.User.Email,
                                    vendorStoreName: vendor.StoreName,
                                    orderNumber: order.OrderNumber,
                                    vendorEarnings: earnings,
                                    itemNames: itemNames);

                                System.Diagnostics.Debug.WriteLine($"[Webhook] Vendor sale email sent to {vendor.User.Email}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Webhook] EMAIL ERROR: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Webhook] INNER ERROR: {ex.InnerException.Message}");
                    }
                }
            }
        }

        // ── Get Order Success Data ───────────────────────────
        public async Task<OrderSuccessViewModel?> GetOrderSuccessAsync(
            int orderId)
        {
            var order = await _uow.Orders.GetOrderWithItemsAsync(orderId);
            if (order == null) return null;

            return new OrderSuccessViewModel
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Total = order.Total,
                IsPaid = order.Status == OrderStatus.Paid
            };
        }

        // ── Check if order is paid (for polling) ────────────
        public async Task<bool> IsOrderPaidAsync(int orderId)
        {
            var order = await _uow.Orders.GetByIdAsync(orderId);
            return order?.Status == OrderStatus.Paid;
        }

        // ── Private helper ───────────────────────────────────
        private int GetVendorIdForVariation(int variationId)
        {
            /*
                We need the VendorId for each OrderItem.
                Since CartItemViewModel doesn't carry VendorId,
                we look it up synchronously here.
                Not ideal — but acceptable for now.
                In a later refactor, CartItemViewModel
                should include VendorId.
            */
            var variation = _uow.Variations
                .FindAsync(v => v.Id == variationId)
                .Result
                .FirstOrDefault();

            return variation?.Product?.VendorId ?? 0;
        }

        public async Task<List<OrderListItemViewModel>> GetCustomerOrderAsync(string userId)
        {
            var orders = await _uow.Orders.GetOrdersByUserAsync(userId);

            return orders.Select(o => new OrderListItemViewModel
            {
                OrderId = o.Id,
                OrderNumber = o.OrderNumber,
                CreatedAt = o.CreatedAt,
                Status = o.Status.ToString(),
                StatusColor = GetStatusColor(o.Status),
                Total = o.Total,
                ItemCount = o.OrderItems.Sum(i => i.Quantity),
                CustomerName = $"{o.ShippingFullName}"
            }).ToList();
        }

        public async Task<OrderDetailViewModel?> GetCustomerOrderDetailAsync(int orderId, string userId)
        {
            var order = await _uow.Orders.GetOrderWithItemsAsync(orderId);

            if (order == null || order.UserId != userId)
                return null;

            return MapToOrderDetail(order, null);
        }

        public async Task<List<OrderListItemViewModel>> GetVendorOrdersAsync(int vendorId)
        {
            var orders = await _uow.Orders.GetOrdersByVendorAsync(vendorId);

            return orders.Select(o => new OrderListItemViewModel
            {
                OrderId = o.Id,
                OrderNumber = o.OrderNumber,
                CreatedAt = o.CreatedAt,
                Status = o.Status.ToString(),
                StatusColor = GetStatusColor(o.Status),
                Total = o.OrderItems
                    .Where(i => i.VendorId == vendorId)
                    .Sum(i => i.VendorEarnings),
                ItemCount = o.OrderItems
                    .Where(i => i.VendorId == vendorId)
                    .Sum(i => i.Quantity),
                CustomerName = o.User.FirstName + " " + o.User.LastName,
            }).ToList();
        }

        public async Task<OrderDetailViewModel?> GetVendorOrderDetailAsync(int orderId, int vendorId)
        {
            var order = await _uow.Orders.GetOrderWithItemsAsync(orderId);
            if (order == null) return null;

            var hasItems = order.OrderItems.Any(i => i.VendorId == vendorId);
            if (!hasItems) return null;

            return MapToOrderDetail(order, vendorId);
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, int vendorId, OrderStatus newStatus)
        {
            var order = await _uow.Orders.GetOrderWithItemsAsync(orderId);
            if (order == null) return false;

            var hasItems = order.OrderItems.Any(i => i.VendorId == vendorId);
            if (!hasItems) return false;

            /*
        Only allow valid transitions:
        Paid → Processing
        Processing → Shipped
        Shipped → Delivered
        Vendor cannot cancel after Shipped.
    */
            var validTransitions = GetValidTransitions(order.Status);
            if (!validTransitions.Contains(newStatus))
                return false;

            order.Status = newStatus;
            await _uow.SaveAsync();
            return true;
        }

        // ── Private Helpers ──────────────────────────────────────
        private OrderDetailViewModel MapToOrderDetail(
            Souq.Models.Order order, int? vendorId)
        {
            /*
                If vendorId is provided → show only their items
                and their earnings.
                If null → customer view → show all items.
            */
            var items = vendorId.HasValue
                ? order.OrderItems.Where(i => i.VendorId == vendorId.Value).ToList()
                : order.OrderItems.ToList();

            var vendorEarnings = items.Sum(i => i.VendorEarnings);
            var validTransitions = vendorId.HasValue
                ? GetValidTransitions(order.Status)
                : new List<OrderStatus>();

            return new OrderDetailViewModel
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                CreatedAt = order.CreatedAt,
                PaidAt = order.PaidAt,
                Status = order.Status.ToString(),
                StatusColor = GetStatusColor(order.Status),

                ShippingFullName = order.ShippingFullName,
                ShippingAddress = order.ShippingAddressLine,
                ShippingCity = order.ShippingCity,
                ShippingCountry = order.ShippingCountry,
                ShippingPhone = order.ShippingPhone,

                Items = items.Select(i => new OrderItemViewModel
                {
                    OrderItemId = i.Id,
                    ImageUrl = i.ImageUrl,
                    ProductName = i.ProductName,
                    VariationName = i.VariationName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    VendorEarnings = i.VendorEarnings,
                    VendorName = i.Vendor?.StoreName ?? ""
                }).ToList(),

                SubTotal = items.Sum(i => i.UnitPrice * i.Quantity),
                Total = order.Total,
                VendorEarnings = vendorEarnings,
                CanUpdateStatus = vendorId.HasValue && validTransitions.Any(),
                AvailableStatusTransitions = validTransitions
                    .Select(s => s.ToString()).ToList()
            };
        }

        private string GetStatusColor(Souq.Models.Enums.OrderStatus status)
        {
            /*
                Returns Tailwind CSS classes for each status.
                Used in both list and detail views.
            */
            return status switch
            {
                Souq.Models.Enums.OrderStatus.Draft => "bg-gray-100 text-gray-600",
                Souq.Models.Enums.OrderStatus.Paid => "bg-blue-100 text-blue-700",
                Souq.Models.Enums.OrderStatus.Processing => "bg-amber-100 text-amber-700",
                Souq.Models.Enums.OrderStatus.Shipped => "bg-purple-100 text-purple-700",
                Souq.Models.Enums.OrderStatus.Delivered => "bg-green-100 text-green-700",
                Souq.Models.Enums.OrderStatus.Cancelled => "bg-red-100 text-red-700",
                _ => "bg-gray-100 text-gray-600"
            };
        }

        private List<Souq.Models.Enums.OrderStatus> GetValidTransitions(
            Souq.Models.Enums.OrderStatus current)
        {
            return current switch
            {
                Souq.Models.Enums.OrderStatus.Paid =>
                    new List<Souq.Models.Enums.OrderStatus>
                    {
                Souq.Models.Enums.OrderStatus.Processing,
                Souq.Models.Enums.OrderStatus.Cancelled
                    },
                Souq.Models.Enums.OrderStatus.Processing =>
                    new List<Souq.Models.Enums.OrderStatus>
                    {
                Souq.Models.Enums.OrderStatus.Shipped
                    },
                Souq.Models.Enums.OrderStatus.Shipped =>
                    new List<Souq.Models.Enums.OrderStatus>
                    {
                Souq.Models.Enums.OrderStatus.Delivered
                    },
                _ => new List<Souq.Models.Enums.OrderStatus>()
            };
        }
    }
}