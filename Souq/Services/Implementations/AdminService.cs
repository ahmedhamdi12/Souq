using Microsoft.AspNetCore.Identity;
using Souq.Models;
using Souq.Models.Enums;
using Souq.Services.Interfaces;
using Souq.UnitOfWork;
using Souq.ViewModels.Admin;

namespace Souq.Services.Implementations
{
    public class AdminService : IAdminService
    {
        private readonly IUnitOfWork _uow;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminService(IUnitOfWork uow, UserManager<ApplicationUser> userManager)
        {
            _uow = uow;
            _userManager = userManager;
        }

        public async Task<AdminDashboardViewModel> GetDashboardAsync()
        {
            var allUsers = _userManager.Users.ToList();
            var allVendors = await _uow.Vendors.GetAllAsync();
            var allProducts = await _uow.Products.GetAllAsync();
            var allOrders = await _uow.Orders.GetAllAsync();

            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);

            var paidOrders = allOrders.
                Where(o => o.Status != OrderStatus.Draft)
                .ToList();

            var monthOrders = paidOrders
                .Where(o => o.CreatedAt >= monthStart)
                .ToList();

            var platformEarnings = paidOrders
                .Sum(o => o.PlatformFee);

            var recentOrders = paidOrders
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .Select( o =>new RecentActivityItem
                {
                    Type = "Order",
                    Title = $"New Order {o.OrderNumber}",
                    Subtitle = $"${o.Total:0.00}",
                    Color ="bg-blue-100 text-blue-600",
                    CreatedAt = o.CreatedAt

                }).ToList();
            var recentVendors = allVendors
                .OrderByDescending(v => v.CreatedAt)
                .Take(3)
                .Select(v => new RecentActivityItem
                {
                    Type = "Vendor",
                    Title = $"{v.StoreName} applied",
                    Subtitle = v.Status.ToString(),
                    Color = "bg-purple-100 text-purple-600",
                    CreatedAt = v.CreatedAt
                }).ToList();

            var activity = recentOrders
                .Concat(recentVendors)
                .OrderByDescending(a => a.CreatedAt)
                .Take(8)
                .ToList();

            return new AdminDashboardViewModel
            {
                TotalUsers = allUsers.Count,
                TotalVendors = allVendors.Count(),
                PendingVendors = allVendors.Count(v => v.Status == VendorStatus.Pending),
                TotalProducts = allProducts.Count(),
                PendingProducts = allProducts.Count(p => !p.IsApproved),
                TotalOrders = paidOrders.Count,
                TotalRevenue = paidOrders.Sum(o => o.Total),
                MonthRevenue = monthOrders.Sum(o => o.Total),
                PlatformEarnings = platformEarnings,
                RecentActivity = activity
            };
        }

        public async Task<List<AdminVendorViewModel>> GetAllVendorsAsync()
        {
            var vendors = await _uow.Vendors.GetAllAsync();
            var result = new List<AdminVendorViewModel>();

            foreach (var v in vendors)
            {
                var products = await _uow.Products.GetProductsByVendorIdAsync(v.Id);

                var orders = await _uow.Orders.GetOrdersByVendorAsync(v.Id);

                var earnings = orders
                    .SelectMany(o => o.OrderItems.Where(oi => oi.VendorId == v.Id))
                    .Sum(oi => oi.VendorEarnings);

                result.Add(new AdminVendorViewModel
                {
                    Id = v.Id,
                    StoreName = v.StoreName,
                    StoreSlug = v.StoreSlug,
                    OwnerName = v.User?.FirstName + " " + v.User?.LastName,
                    OwnerEmail = v.User?.Email ?? "",
                    Status = v.Status,
                    StatusColor = GetVendorStatusColor(v.Status),
                    ProductCount = products.Count(),
                    TotalEarnings = earnings,
                    CreatedAt = v.CreatedAt
                }); 
            }

            return result.OrderByDescending(v => v.CreatedAt).ToList();
        }
        

        public async Task<bool> ApproveVendorAsync(int vendorId)
        {
            var vendor = await _uow.Vendors.GetByIdAsync(vendorId);
            if (vendor == null) return false;

            vendor.Status = VendorStatus.Approved;
            _uow.Vendors.Update(vendor);
            await _uow.SaveAsync();
            return true;
        }

        public async Task<bool> RejectVendorAsync(int vendorId)
        {
            var vendor = await _uow.Vendors.GetByIdAsync(vendorId);
            if (vendor == null) return false;

            vendor.Status = VendorStatus.Rejected;
            _uow.Vendors.Update(vendor);
            await _uow.SaveAsync();
            return true;
        }

        public async Task<List<AdminProductViewModel>> GetAllProductsAsync()
        {
            var products = await _uow.Products.GetApprovedProductAsync();

            var allProducts = await _uow.Products.FindAsync(p => true);

            return allProducts
                .Select(p => new AdminProductViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Slug = p.Slug,
                    ImageUrl = p.Images.FirstOrDefault(i => i.IsMain)?.ImageUrl
                                ?? p.Images.FirstOrDefault()?.ImageUrl,
                    VendorName = p.Vendor?.StoreName ?? "Unknown Vendor",
                    CategoryName = p.Category?.Name ?? "Uncategorized",
                    BasePrice = p.BasePrice,
                    IsApproved = p.IsApproved,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt
                })
                .OrderByDescending(p => p.CreatedAt)
                .ToList();
        }

        public async Task<bool> ApproveProductAsync(int productId)
        {
            var product = await _uow.Products.GetByIdAsync(productId);
            if(product == null) return false;

            product.IsApproved = true;
            _uow.Products.Update(product);
            await _uow.SaveAsync();
            return true;
        }



        public async Task<bool> RejectProductAsync(int productId)
        {
            var product = await _uow.Products.GetByIdAsync(productId);
            if (product == null) return false;

            product.IsApproved = false;
            product.IsActive = false; // Also deactivate the product if rejected
            _uow.Products.Update(product);
            await _uow.SaveAsync();
            return true;
        }

        

        // ── Private helpers ──────────────────────────────────
        private string GetVendorStatusColor(VendorStatus status) =>
            status switch
            {
                VendorStatus.Pending => "bg-amber-100 text-amber-700",
                VendorStatus.Approved => "bg-green-100 text-green-700",
                VendorStatus.Rejected => "bg-red-100 text-red-700",
                VendorStatus.Suspended => "bg-gray-100 text-gray-700",
                _ => "bg-gray-100 text-gray-600"
            };
    }
}
