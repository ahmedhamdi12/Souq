using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Souq.Data;
using Souq.Models;
using Souq.Repositories.Interfaces;
using Souq.Repositories.Implemntations;
using Souq.UnitOfWork;
using Souq.Services.Interfaces;
using Souq.Services.Implementations;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    sqlOptions => sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

//Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.SignIn.RequireConfirmedAccount = false;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders()
    .AddRoles<IdentityRole>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

// Repositories and Unit of Work
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IVendorRepository, VendorRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IVariationRepository, VariationRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IProductImageRepository, ProductImageRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IVendorService, VendorService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
// Add services to the container.

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        /*
            PropertyNameCaseInsensitive = true means
            "variationId" matches "VariationId" correctly.
            This is the standard setting for ASP.NET APIs.
        */
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => false;
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
});
Stripe.StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];
var app = builder.Build();

using(var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.
        GetRequiredService<RoleManager<IdentityRole>>();
    
    var userManager = scope.ServiceProvider.
        GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = { "Admin", "Vendor", "Customer"};
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    var adminEmail = "admin@souq.com";
    if(await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "Admin",
            LastName = "Souq",
            CreatedAt = DateTime.UtcNow
        };
        var result = await userManager.CreateAsync(adminUser, "Admin@123456");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(adminUser, "Admin");
    }

    //--------------------------------------------------------------------------------------------------
    //--------------------------------------------------------------------------------------------------

    // ?? Seed Departments & Categories ?????????????????????????
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (!dbContext.Departments.Any())
    {
        /*
            We check Any() first — if departments already exist
            we skip this entire block. Safe to run repeatedly.
        */
        var departments = new List<Department>
    {
        new Department
        {
            Name      = "Electronics",
            Slug      = "electronics",
            Description = "Phones, laptops, gadgets and accessories",
            SortOrder = 1,
            Categories = new List<Category>
            {
                new Category { Name = "Phones",    Slug = "phones",    SortOrder = 1 },
                new Category { Name = "Laptops",   Slug = "laptops",   SortOrder = 2 },
                new Category { Name = "Accessories", Slug = "accessories", SortOrder = 3 }
            }
        },
        new Department
        {
            Name      = "Fashion",
            Slug      = "fashion",
            Description = "Clothing, shoes and accessories",
            SortOrder = 2,
            Categories = new List<Category>
            {
                new Category { Name = "Men",    Slug = "men",    SortOrder = 1 },
                new Category { Name = "Women",  Slug = "women",  SortOrder = 2 },
                new Category { Name = "Kids",   Slug = "kids",   SortOrder = 3 }
            }
        },
        new Department
        {
            Name      = "Home & Living",
            Slug      = "home-living",
            Description = "Furniture, decor and kitchen essentials",
            SortOrder = 3,
            Categories = new List<Category>
            {
                new Category { Name = "Furniture", Slug = "furniture", SortOrder = 1 },
                new Category { Name = "Kitchen",   Slug = "kitchen",   SortOrder = 2 },
                new Category { Name = "Decor",     Slug = "decor",     SortOrder = 3 }
            }
        },
        new Department
        {
            Name      = "Sports",
            Slug      = "sports",
            Description = "Equipment, clothing and accessories for sports",
            SortOrder = 4,
            Categories = new List<Category>
            {
                new Category { Name = "Fitness",  Slug = "fitness",  SortOrder = 1 },
                new Category { Name = "Outdoor",  Slug = "outdoor",  SortOrder = 2 },
                new Category { Name = "Football", Slug = "football", SortOrder = 3 }
            }
        },
        new Department
        {
            Name      = "Books",
            Slug      = "books",
            Description = "Books, magazines and educational material",
            SortOrder = 5,
            Categories = new List<Category>
            {
                new Category { Name = "Fiction",    Slug = "fiction",    SortOrder = 1 },
                new Category { Name = "Technology", Slug = "technology-books", SortOrder = 2 },
                new Category { Name = "Business",   Slug = "business",   SortOrder = 3 }
            }
        }
    };

        await dbContext.Departments.AddRangeAsync(departments);
        await dbContext.SaveChangesAsync();
    }

    // ?? Seed a test Vendor + Products ??????????????????????????
    /*
        We need a vendor to attach products to.
        First create a vendor user, then create their VendorProfile,
        then attach products to that profile.
    */
    if (!dbContext.Products.Any())
    {
        // Create vendor user if not exists
        var vendorEmail = "vendor@souq.com";
        var vendorUser = await userManager.FindByEmailAsync(vendorEmail);

        if (vendorUser == null)
        {
            vendorUser = new ApplicationUser
            {
                FirstName = "Test",
                LastName = "Vendor",
                UserName = vendorEmail,
                Email = vendorEmail,
                CreatedAt = DateTime.UtcNow
            };
            var vendorResult = await userManager.CreateAsync(vendorUser, "Vendor@12345");
            if (vendorResult.Succeeded)
                await userManager.AddToRoleAsync(vendorUser, "Vendor");
        }

        // Create vendor profile if not exists
        var vendorProfile = dbContext.VendorProfiles
            .FirstOrDefault(v => v.UserId == vendorUser.Id);

        if (vendorProfile == null)
        {
            vendorProfile = new VendorProfile
            {
                UserId = vendorUser.Id,
                StoreName = "Tech Haven",
                StoreSlug = "tech-haven",
                Description = "Your one-stop shop for the latest tech products",
                Status = Souq.Models.Enums.VendorStatus.Approved
            };
            dbContext.VendorProfiles.Add(vendorProfile);
            await dbContext.SaveChangesAsync();
        }

        // Get category IDs we just seeded
        var phonesCategory = dbContext.Categories
            .First(c => c.Slug == "phones");
        var laptopsCategory = dbContext.Categories
            .First(c => c.Slug == "laptops");
        var fitnessCategory = dbContext.Categories
            .First(c => c.Slug == "fitness");

        // Seed products
        var products = new List<Product>
    {
        new Product
        {
            VendorId    = vendorProfile.Id,
            CategoryId  = phonesCategory.Id,
            Name        = "Wireless Pro Headphones",
            Slug        = "wireless-pro-headphones",
            Description = "Premium wireless headphones with active noise cancellation, 30hr battery life and studio-quality sound.",
            BasePrice   = 89.99m,
            HasVariations = true,
            IsApproved  = true,
            IsActive    = true,
            MetaTitle   = "Wireless Pro Headphones - Best Sound Quality",
            CreatedAt   = DateTime.UtcNow,
            Images = new List<ProductImage>
            {
                new ProductImage
                {
                    ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=400",
                    IsMain   = true,
                    SortOrder = 1
                }
            },
            Variations = new List<ProductVariation>
            {
                new ProductVariation
                {
                    Name          = "Black",
                    SKU           = "WPH-BLK",
                    Price         = 89.99m,
                    StockQuantity = 50,
                    Color         = "Black"
                },
                new ProductVariation
                {
                    Name          = "White",
                    SKU           = "WPH-WHT",
                    Price         = 89.99m,
                    StockQuantity = 30,
                    Color         = "White"
                },
                new ProductVariation
                {
                    Name          = "Silver",
                    SKU           = "WPH-SLV",
                    Price         = 94.99m,
                    StockQuantity = 20,
                    Color         = "Silver"
                }
            }
        },
        new Product
        {
            VendorId    = vendorProfile.Id,
            CategoryId  = laptopsCategory.Id,
            Name        = "UltraBook Pro 15",
            Slug        = "ultrabook-pro-15",
            Description = "Thin and light laptop with Intel Core i7, 16GB RAM, 512GB SSD. Perfect for professionals on the go.",
            BasePrice   = 1199.99m,
            HasVariations = true,
            IsApproved  = true,
            IsActive    = true,
            CreatedAt   = DateTime.UtcNow,
            Images = new List<ProductImage>
            {
                new ProductImage
                {
                    ImageUrl = "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=400",
                    IsMain   = true,
                    SortOrder = 1
                }
            },
            Variations = new List<ProductVariation>
            {
                new ProductVariation
                {
                    Name          = "16GB / 512GB",
                    SKU           = "UBP-16-512",
                    Price         = 1199.99m,
                    StockQuantity = 15,
                    Size          = "16GB RAM"
                },
                new ProductVariation
                {
                    Name          = "32GB / 1TB",
                    SKU           = "UBP-32-1TB",
                    Price         = 1599.99m,
                    StockQuantity = 8,
                    Size          = "32GB RAM"
                }
            }
        },
        new Product
        {
            VendorId    = vendorProfile.Id,
            CategoryId  = phonesCategory.Id,
            Name        = "SmartWatch Series X",
            Slug        = "smartwatch-series-x",
            Description = "Advanced smartwatch with health monitoring, GPS, 7-day battery and beautiful AMOLED display.",
            BasePrice   = 249.99m,
            HasVariations = false,
            IsApproved  = true,
            IsActive    = true,
            CreatedAt   = DateTime.UtcNow,
            Images = new List<ProductImage>
            {
                new ProductImage
                {
                    ImageUrl = "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=400",
                    IsMain   = true,
                    SortOrder = 1
                }
            },
            Variations = new List<ProductVariation>()
        },
        new Product
        {
            VendorId    = vendorProfile.Id,
            CategoryId  = fitnessCategory.Id,
            Name        = "Pro Yoga Mat",
            Slug        = "pro-yoga-mat",
            Description = "Extra thick non-slip yoga mat with alignment lines. Eco-friendly material, includes carry strap.",
            BasePrice   = 34.99m,
            HasVariations = true,
            IsApproved  = true,
            IsActive    = true,
            CreatedAt   = DateTime.UtcNow,
            Images = new List<ProductImage>
            {
                new ProductImage
                {
                    ImageUrl = "https://images.unsplash.com/photo-1592432678016-e910b452f9a2?w=400",
                    IsMain   = true,
                    SortOrder = 1
                }
            },
            Variations = new List<ProductVariation>
            {
                new ProductVariation
                {
                    Name          = "Purple",
                    SKU           = "YM-PRP",
                    Price         = 34.99m,
                    StockQuantity = 100,
                    Color         = "Purple"
                },
                new ProductVariation
                {
                    Name          = "Blue",
                    SKU           = "YM-BLU",
                    Price         = 34.99m,
                    StockQuantity = 80,
                    Color         = "Blue"
                },
                new ProductVariation
                {
                    Name          = "Black",
                    SKU           = "YM-BLK",
                    Price         = 29.99m,
                    StockQuantity = 120,
                    Color         = "Black"
                }
            }
        },
        new Product
        {
            VendorId    = vendorProfile.Id,
            CategoryId  = laptopsCategory.Id,
            Name        = "Mechanical Gaming Keyboard",
            Slug        = "mechanical-gaming-keyboard",
            Description = "RGB mechanical keyboard with Cherry MX switches, full N-key rollover and programmable macros.",
            BasePrice   = 79.99m,
            HasVariations = true,
            IsApproved  = true,
            IsActive    = true,
            CreatedAt   = DateTime.UtcNow,
            Images = new List<ProductImage>
            {
                new ProductImage
                {
                    ImageUrl = "https://images.unsplash.com/photo-1601445638532-1f0a5b0a0870?w=400",
                    IsMain   = true,
                    SortOrder = 1
                }
            },
            Variations = new List<ProductVariation>
            {
                new ProductVariation
                {
                    Name          = "Red Switches",
                    SKU           = "MKB-RED",
                    Price         = 79.99m,
                    StockQuantity = 40,
                    Color         = "Red"
                },
                new ProductVariation
                {
                    Name          = "Blue Switches",
                    SKU           = "MKB-BLU",
                    Price         = 79.99m,
                    StockQuantity = 35,
                    Color         = "Blue"
                }
            }
        },
        new Product
        {
            VendorId    = vendorProfile.Id,
            CategoryId  = phonesCategory.Id,
            Name        = "Portable Bluetooth Speaker",
            Slug        = "portable-bluetooth-speaker",
            Description = "Waterproof portable speaker with 360 sound, 24hr battery and built-in powerbank.",
            BasePrice   = 49.99m,
            HasVariations = false,
            IsApproved  = true,
            IsActive    = true,
            CreatedAt   = DateTime.UtcNow,
            Images = new List<ProductImage>
            {
                new ProductImage
                {
                    ImageUrl = "https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?w=400",
                    IsMain   = true,
                    SortOrder = 1
                }
            },
            Variations = new List<ProductVariation>()
        },
        new Product
        {
            VendorId    = vendorProfile.Id,
            CategoryId  = fitnessCategory.Id,
            Name        = "Adjustable Dumbbell Set",
            Slug        = "adjustable-dumbbell-set",
            Description = "Space-saving adjustable dumbbells from 5 to 52.5 lbs. Replace 15 sets of weights.",
            BasePrice   = 299.99m,
            HasVariations = false,
            IsApproved  = true,
            IsActive    = true,
            CreatedAt   = DateTime.UtcNow,
            Images = new List<ProductImage>
            {
                new ProductImage
                {
                    ImageUrl = "https://images.unsplash.com/photo-1590771998996-8589ec9b5ac6?w=400",
                    IsMain   = true,
                    SortOrder = 1
                }
            },
            Variations = new List<ProductVariation>()
        },
        new Product
        {
            VendorId    = vendorProfile.Id,
            CategoryId  = laptopsCategory.Id,
            Name        = "USB-C Hub 7-in-1",
            Slug        = "usb-c-hub-7-in-1",
            Description = "Compact USB-C hub with HDMI 4K, 3x USB 3.0, SD card, PD charging and ethernet.",
            BasePrice   = 39.99m,
            HasVariations = false,
            IsApproved  = true,
            IsActive    = true,
            CreatedAt   = DateTime.UtcNow,
            Images = new List<ProductImage>
            {
                new ProductImage
                {
                    ImageUrl = "https://images.unsplash.com/photo-1625948515291-ee7b3f6fc49f?w=400",
                    IsMain   = true,
                    SortOrder = 1
                }
            },
            Variations = new List<ProductVariation>()
        }
    };

        await dbContext.Products.AddRangeAsync(products);
        await dbContext.SaveChangesAsync();
    }
}



// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCookiePolicy();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "vendorApply",
    pattern: "vendor/apply",
    defaults: new { controller = "VendorApplication", action = "Apply" });
app.MapControllerRoute(
    name: "adminVendors",
    pattern: "admin/vendors",
    defaults: new { controller = "Admin", action = "Vendors" });

app.MapControllerRoute(
    name: "adminProducts",
    pattern: "admin/products",
    defaults: new { controller = "Admin", action = "Products" });

app.MapControllerRoute(
    name: "admin",
    pattern: "admin",
    defaults: new { controller = "Admin", action = "Index" });
app.MapControllerRoute(
    name: "vendorDashboard",
    pattern: "vendor/dashboard",
    defaults: new { controller = "Vendor", action = "Dashboard" });

app.MapControllerRoute(
    name: "vendorProducts",
    pattern: "vendor/products",
    defaults: new { controller = "Vendor", action = "Products" });

app.MapControllerRoute(
    name: "vendorCreateProduct",
    pattern: "vendor/products/create",
    defaults: new { controller = "Vendor", action = "CreateProduct" });

app.MapControllerRoute(
    name: "vendorSaveProduct",
    pattern: "vendor/products/save",
    defaults: new { controller = "Vendor", action = "SaveProduct" });

app.MapControllerRoute(
    name: "vendorEditProduct",
    pattern: "vendor/products/edit/{id}",
    defaults: new { controller = "Vendor", action = "EditProduct" });

app.MapControllerRoute(
    name: "vendorDeleteProduct",
    pattern: "vendor/products/delete",
    defaults: new { controller = "Vendor", action = "DeleteProduct" });
app.MapControllerRoute(
    name: "productSearch",
    pattern: "products/search",
    defaults: new { controller = "Products", action = "Search" });

app.MapControllerRoute(
    name: "productDepartment",
    pattern: "products/department/{slug}",
    defaults: new { controller = "Products", action = "Department" });

app.MapControllerRoute(
    name: "productDetail",
    pattern: "products/{slug}",
    defaults: new { controller = "Products", action = "Details" });

app.MapControllerRoute(
    name: "products",
    pattern: "products",
    defaults: new { controller = "Products", action = "Index" });
app.MapControllerRoute(
    name: "productDetail",
    pattern: "products/{slug}",
    defaults: new { controller = "Products", action = "Details" });
app.MapControllerRoute(
    name: "vendorOrders",
    pattern: "vendor/orders/{id?}",
    defaults: new { controller = "Vendor", action = "Orders" });

app.MapControllerRoute(
    name: "vendorOrderDetail",
    pattern: "vendor/orders/{id}",
    defaults: new { controller = "Vendor", action = "OrderDetail" });

app.MapControllerRoute(
    name: "orderDetail",
    pattern: "orders/{id}",
    defaults: new { controller = "Orders", action = "Detail" });
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
