using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Rendering;
using Souq.Models;
using Souq.Services.Interfaces;
using Souq.UnitOfWork;
using Souq.ViewModels.Vendor;

namespace Souq.Services.Implementations
{
    public class VendorService : IVendorService
    {
        private readonly IUnitOfWork _uow;

        public VendorService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        

        public async Task<VendorDashboardViewModel?> GetDashboardAsync(string userId)
        {
            var vendor = await _uow.Vendors.GetByUserIdAsync(userId);
            if(vendor == null) return null;

            var products = await _uow.Products.GetProductsByVendorIdAsync(vendor.Id);
            var orders = await _uow.Orders.GetOrdersByVendorAsync(vendor.Id);
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);

            var allItems = orders.
                SelectMany(o => o.OrderItems
                .Where(i => i.VendorId == vendor.Id))
                .ToList();
            var monthItems = orders.
                 Where(o => o.CreatedAt >= monthStart)
                .SelectMany(o => o.OrderItems
                .Where(i => i.VendorId == vendor.Id))
                .ToList();

            return new VendorDashboardViewModel
            {
                StoreName = vendor.StoreName,
                TotalProducts = products.Count(),
                PendingApproval = products.Count(p => !p.IsApproved),
                TotalOrders = orders.Count(),
                PendingOrders = orders.Count(o => o.Status == Models.Enums.OrderStatus.Paid),
                TotalEarnings = allItems.Sum(i => i.VendorEarnings),
                MonthEarnings = monthItems.Sum(i => i.VendorEarnings),
                RecentOrders = orders
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(5)
                    .Select(o => new RecentOrderItem
                    {
                        OrderId = o.Id,
                        OrderNumber = o.OrderNumber,
                        CustomerName = o.User.FirstName + " " + o.User.LastName,
                        Earnings = o.OrderItems.Where(i => i.VendorId == vendor.Id).Sum(i => i.VendorEarnings),
                        Status = o.Status.ToString(),
                        StatusColor = GetStatusColor(o.Status),
                        CreatedAt = o.CreatedAt
                    })
                    .ToList()
            };
        }

        public async Task<ProductFormViewModel> GetProductsFormAsync(int? productId, int vendorId)
        {
            var categories = await _uow.Departments.GetAllAsync();
            var allCategories =new List<SelectListItem>();

            foreach (var dept in categories)
            {
                var deptwithCats = await _uow.Departments.GetByIdAsync(dept.Id);
                var cats = await _uow.Categories.FindAsync(c => c.DepartmentId == dept.Id);
                foreach(var cat in cats)
                {
                    allCategories.Add(new SelectListItem
                    {
                        Value = cat.Id.ToString(),
                        Text = $"{dept.Name} > {cat.Name}"
                    });
                }
            }
            if (productId == null || productId == 0)
            {
                return new ProductFormViewModel
                {
                    Categories = allCategories
                };
            }

            //edit moad load existing Prooducts
            var product = await _uow.Products.GetProductWithDetailsAsync(productId.Value);
            if (product == null || product.VendorId != vendorId)
            {
                return new ProductFormViewModel
                {
                    Categories = allCategories
                };
            }

            return new ProductFormViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Slug = product.Slug,
                Description = product.Description ?? "",
                CategoryId = product.CategoryId,
                BasePrice = product.BasePrice,
                HasVariations = product.HasVariations,
                MetaTitle = product.MetaTitle,
                MetaDescription = product.MetaDescription,
                Categories = allCategories,
                VariationsJson = JsonSerializer.Serialize(product.Variations.Select(v => new VariationFormItem
                {
                    Id = v.Id,
                    Name = v.Name,
                    SKU = v.SKU,
                    Price = v.Price,
                    StockQuantity = v.StockQuantity,
                    Color = v.Color,
                    Size = v.Size
                })),
                ExistingImages = product.Images.Select(i => new ExistingImageViewModel
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    IsMain = i.IsMain
                }).ToList()
            };
        }

        public async Task<List<ProductListItemViewModel>> GetVendorProductsAsync(int vendorId)
        {
            var products = await _uow.Products.GetProductsByVendorIdAsync(vendorId);
            return products.Select(p => new ProductListItemViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                ImageUrl = p.Images.FirstOrDefault(i => i.IsMain)?.ImageUrl
                            ?? p.Images.FirstOrDefault()?.ImageUrl,
                BasePrice = p.BasePrice,
                HasVariations = p.HasVariations,
                IsApproved = p.IsApproved,
                IsActive = p.IsActive,
                TotalStock = p.Variations.Sum(v => v.AvailableStock),
                CategoryName = p.Category.Name,
                CreatedAt = p.CreatedAt
            }).ToList();
        }

        public async Task<bool> SaveProductsAsync(ProductFormViewModel model, int vendorId, IWebHostEnvironment env)
        {
            /*
                Generate slug from product name.
                Lowercase, replace spaces with hyphens,
                remove special characters.
            */
            var slug = GenerateSlug(model.Name);

            var uploadedImagesUrl = await SaveImagesAsync(model.NewImages,vendorId,env);

            if (model.IsEdit)
            {
                return await UpdateProductAsync(model, vendorId, slug, uploadedImagesUrl);
            }
            else
            {
                return await CreateProductAsync(model, vendorId, slug, uploadedImagesUrl);
            }

        }
        public async Task<bool> DeleteProductsAsync(int productId, int vendorId)
        {
            var product = await _uow.Products.GetProductWithDetailsAsync(productId);
            if (product == null || product.VendorId != vendorId)
                return false;

            var hasOrders = await _uow.Variations.HasOrderItemsAsync(productId);

            if (hasOrders)
            {
                product.IsActive = false;
                product.IsApproved = false;
                _uow.Products.Update(product);
                await _uow.SaveAsync();
                return true;
            }

            // 1. Remove cart items for all variations
            foreach (var variation in product.Variations)
            {
                var cartItems = await _uow.Cart
                    .FindAsync(c => c.VariationId == variation.Id);

                foreach (var cartItem in cartItems)
                    _uow.Cart.Remove(cartItem);
            }

            // 2. Remove reviews
            var reviews = await _uow.Reviews
                .FindAsync(r => r.ProductId == productId);

            foreach (var review in reviews)
                _uow.Reviews.Remove(review);

            // 3. Remove variations
            foreach (var variation in product.Variations)
                _uow.Variations.Remove(variation);

            // 4. Remove images
            foreach (var image in product.Images)
                _uow.ProductImages.Remove(image);

            // 5. Remove the product itself
            _uow.Products.Remove(product);

            
            await _uow.SaveAsync();
            return true;
        }

        //add product
        // ── Private: Create product ───────────────────────────
        private async Task<bool> CreateProductAsync(
            ProductFormViewModel model,
            int vendorId,
            string slug,
            List<string> imageUrls)
        {
            var variations = ParseVariations(model.VariationsJson);

            var product = new Product
            {
                VendorId = vendorId,
                CategoryId = model.CategoryId,
                Name = model.Name,
                Slug = slug,
                Description = model.Description,
                BasePrice = model.BasePrice,
                HasVariations = model.HasVariations,
                IsApproved = false,  // needs admin approval
                IsActive = true,
                MetaTitle = model.MetaTitle,
                MetaDescription = model.MetaDescription,
                CreatedAt = DateTime.UtcNow,
                Variations = variations,
                Images = imageUrls.Select((url, idx) =>
                    new ProductImage
                    {
                        ImageUrl = url,
                        IsMain = idx == 0,
                        SortOrder = idx
                    }).ToList()
            };

            await _uow.Products.AddAsync(product);
            await _uow.SaveAsync();
            return true;
        }

        //update product
        private async Task<bool> UpdateProductAsync(
            ProductFormViewModel model,
            int vendorId,
            string slug,
            List<string> newImageUrls)
        {
            var product = await _uow.Products
                .GetProductWithDetailsAsync(model.Id);

            if (product == null || product.VendorId != vendorId)
                return false;

            product.Name = model.Name;
            product.Slug = slug;
            product.Description = model.Description;
            product.CategoryId = model.CategoryId;
            product.BasePrice = model.BasePrice;
            product.HasVariations = model.HasVariations;
            product.MetaTitle = model.MetaTitle;
            product.MetaDescription = model.MetaDescription;

            // Update variations
            var variations = ParseVariations(model.VariationsJson);
            foreach (var v in variations)
            {
                if (v.Id > 0)
                {
                    // Update existing
                    var existing = product.Variations
                        .FirstOrDefault(pv => pv.Id == v.Id);
                    if (existing != null)
                    {
                        existing.Name = v.Name;
                        existing.SKU = v.SKU;
                        existing.Price = v.Price;
                        existing.StockQuantity = v.StockQuantity;
                        existing.Color = v.Color;
                        existing.Size = v.Size;
                    }
                }
                else
                {
                    // New variation
                    product.Variations.Add(v);
                }
            }

            // Add new images
            foreach (var (url, idx) in newImageUrls.Select((u, i) => (u, i)))
            {
                product.Images.Add(new ProductImage
                {
                    ImageUrl = url,
                    IsMain = !product.Images.Any(),
                    SortOrder = product.Images.Count + idx
                });
            }

            _uow.Products.Update(product);
            await _uow.SaveAsync();
            return true;
        }

        //Helpers
        private string GetStatusColor(Models.Enums.OrderStatus status) =>
            status switch
            {
                Models.Enums.OrderStatus.Draft => "bg-gray-100 text-gray-600",
                Models.Enums.OrderStatus.Paid => "bg-blue-100 text-blue-700",
                Models.Enums.OrderStatus.Processing => "bg-amber-100 text-amber-700",
                Models.Enums.OrderStatus.Shipped => "bg-purple-100 text-purple-700",
                Models.Enums.OrderStatus.Delivered => "bg-green-100 text-green-700",
                Models.Enums.OrderStatus.Cancelled => "bg-red-100 text-red-700",
                _ => "bg-gray-100 text-gray-600"
            };

        // ── Private: Parse variations JSON ────────────────────
        private List<ProductVariation> ParseVariations(string json)
        {
            if (string.IsNullOrEmpty(json) || json == "[]")
                return new List<ProductVariation>();

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var items = JsonSerializer.Deserialize
                    <List < VariationFormItem >> (json,options)
                     ?? new List<VariationFormItem>();

                return items
                    .Where(v => !v.ShouldDelete)
                    .Select(v => new ProductVariation
                    {
                        Id = v.Id,
                        Name = v.Name,
                        SKU = v.SKU,
                        Price = v.Price,
                        StockQuantity = v.StockQuantity,
                        Color = v.Color,
                        Size = v.Size,
                        IsActive = true
                    }).ToList();
            }
            catch
            {
                return new List<ProductVariation>();
            }
        }

        private string GenerateSlug(string name)
        {
            return name.ToLower()
                .Replace(" ", "-")
                .Replace("'", "")
                .Replace("\"", "")
                .Replace(".", "")
                .Replace(",", "")
                .Replace("!", "")
                .Replace("?", "")
                .Replace("&", "and")
                .Replace("/", "-")
                + "-" + Guid.NewGuid().ToString("N")[..6];
            /*
                We append 6 random chars to prevent slug conflicts.
                Two vendors could sell products with the same name.
            */
        }

        private async Task<List<string>> SaveImagesAsync(
            List<IFormFile>? files,
            int vendorId,
            IWebHostEnvironment env)
        {
            var urls = new List<string>();
            if (files == null || !files.Any()) return urls;

            /*
                Save to wwwroot/images/products/{vendorId}/
                Generate a unique filename using GUID.
                Return the relative URL path.
            */
            var folderPath = Path.Combine(
                env.WebRootPath, "images", "products",
                vendorId.ToString());

            Directory.CreateDirectory(folderPath);

            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var extension = Path.GetExtension(file.FileName).ToLower();

                if (!allowedExtensions.Contains(extension)) continue;

                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(folderPath, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                urls.Add($"/images/products/{vendorId}/{fileName}");
            }

            return urls;
        }
    }
}
