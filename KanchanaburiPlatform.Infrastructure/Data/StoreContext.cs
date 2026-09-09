using Core.Entities;
using KanchanaburiPlatform.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public class StoreContext(DbContextOptions<StoreContext> options) : IdentityDbContext<AppUser>(options)
{
    //ร้านค้า
    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<ShopCategory> ShopCategories => Set<ShopCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<MerchantPayout> MerchantPayouts => Set<MerchantPayout>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Report> Reports => Set<Report>();
    //คอนเท้น
    public DbSet<Content> Contents => Set<Content>();
    public DbSet<ContentCategory> ContentCategories => Set<ContentCategory>();
    public DbSet<ContentRelation> ContentRelations => Set<ContentRelation>();
    public DbSet<ContentTag> ContentTags => Set<ContentTag>();
    public DbSet<ContentView> ContentViews => Set<ContentView>();
    public DbSet<ContentFavorite> ContentFavorites => Set<ContentFavorite>();
    public DbSet<DataSource> DataSources => Set<DataSource>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<District> Districts => Set<District>();
    public DbSet<SubDistrict> SubDistricts => Set<SubDistrict>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureShopEntities(modelBuilder);

        modelBuilder.Entity<District>(e => { e.HasKey(x => x.DistrictId); e.Property(x => x.DistrictName).HasMaxLength(150).IsRequired(); });
        modelBuilder.Entity<SubDistrict>(e =>
        {
            e.HasKey(x => x.SubDistrictId); e.Property(x => x.SubDistrictName).HasMaxLength(150).IsRequired(); e.Property(x => x.PostalCode).HasMaxLength(10);
            e.HasOne(x => x.District).WithMany(x => x.SubDistricts).HasForeignKey(x => x.DistrictId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductCategory>(e => { e.HasKey(x => x.ProductCategoryId); e.Property(x => x.CategoryName).HasMaxLength(150).IsRequired(); e.Property(x => x.Status).HasMaxLength(30).IsRequired(); });
        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(x => x.ProductId); e.Property(x => x.ProductName).HasMaxLength(250).IsRequired(); e.Property(x => x.Price).HasPrecision(18, 2); e.Property(x => x.Status).HasMaxLength(30).IsRequired();
            e.HasOne(x => x.Shop).WithMany(x => x.Products).HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ProductCategory).WithMany(x => x.Products).HasForeignKey(x => x.ProductCategoryId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Cart>(e => { e.HasKey(x => x.CartId); e.Property(x => x.UserId).HasMaxLength(450).IsRequired(); e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict); e.HasIndex(x => x.UserId).IsUnique(); });
        modelBuilder.Entity<CartItem>(e =>
        {
            e.HasKey(x => x.CartItemId); e.Property(x => x.UnitPrice).HasPrecision(18, 2); e.HasOne(x => x.Cart).WithMany(x => x.Items).HasForeignKey(x => x.CartId).OnDelete(DeleteBehavior.Cascade); e.HasOne(x => x.Product).WithMany(x => x.CartItems).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<MerchantPayout>(e =>
        {
            e.HasKey(x => x.PayoutId); e.Property(x => x.TotalAmount).HasPrecision(18, 2); e.Property(x => x.Status).HasMaxLength(30).IsRequired();
            e.HasOne(x => x.Shop).WithMany().HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(x => x.OrderId); e.Property(x => x.UserId).HasMaxLength(450).IsRequired(); e.Property(x => x.OrderNumber).HasMaxLength(50).IsRequired(); e.HasIndex(x => x.OrderNumber).IsUnique(); e.Property(x => x.Subtotal).HasPrecision(18, 2); e.Property(x => x.ShippingFee).HasPrecision(18, 2); e.Property(x => x.TotalAmount).HasPrecision(18, 2); e.Property(x => x.ShippingMethod).HasMaxLength(30).IsRequired(); e.Property(x => x.OrderStatus).HasMaxLength(30).IsRequired(); e.Property(x => x.PaymentStatus).HasMaxLength(30).IsRequired(); e.Property(x => x.PayoutStatus).HasMaxLength(30).IsRequired(); e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.Shop).WithMany(x => x.Orders).HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.MerchantPayout).WithMany(x => x.Orders).HasForeignKey(x => x.MerchantPayoutId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<OrderItem>(e => { e.HasKey(x => x.OrderItemId); e.Property(x => x.ProductName).HasMaxLength(250).IsRequired(); e.Property(x => x.UnitPrice).HasPrecision(18, 2); e.Property(x => x.TotalPrice).HasPrecision(18, 2); e.HasOne(x => x.Order).WithMany(x => x.Items).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade); e.HasOne(x => x.Product).WithMany(x => x.OrderItems).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<Payment>(e => { e.HasKey(x => x.PaymentId); e.Property(x => x.PaymentMethod).HasMaxLength(50).IsRequired(); e.Property(x => x.TransactionRef).HasMaxLength(150); e.Property(x => x.Amount).HasPrecision(18, 2); e.Property(x => x.PaymentStatus).HasMaxLength(30).IsRequired(); e.HasOne(x => x.Order).WithMany(x => x.Payments).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<Shipment>(e => { e.HasKey(x => x.ShipmentId); e.Property(x => x.ShippingProvider).HasMaxLength(100); e.Property(x => x.TrackingNumber).HasMaxLength(150); e.Property(x => x.ShippingStatus).HasMaxLength(30).IsRequired(); e.HasOne(x => x.Order).WithMany(x => x.Shipments).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<UserAddress>(e => { e.HasKey(x => x.UserAddressId); e.Property(x => x.UserId).HasMaxLength(450).IsRequired(); e.Property(x => x.RecipientName).HasMaxLength(150).IsRequired(); e.Property(x => x.RecipientPhone).HasMaxLength(30).IsRequired(); e.Property(x => x.AddressLine).HasMaxLength(500).IsRequired(); e.Property(x => x.PostalCode).HasMaxLength(10); e.HasIndex(x => x.UserId); e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade); });

        modelBuilder.Entity<ContentCategory>(e => { e.HasKey(x => x.ContentCategoryId); e.Property(x => x.CategoryName).HasMaxLength(150).IsRequired(); e.Property(x => x.Status).HasMaxLength(30).IsRequired(); });
        modelBuilder.Entity<Content>(e =>
        {
            e.HasKey(x => x.ContentId); e.Property(x => x.CreatedByUserId).HasMaxLength(450).IsRequired(); e.Property(x => x.Title).HasMaxLength(300).IsRequired(); e.Property(x => x.Status).HasMaxLength(30).IsRequired(); e.Property(x => x.YoutubeUrl).HasMaxLength(500); e.Property(x => x.Latitude).HasPrecision(9, 6); e.Property(x => x.Longitude).HasPrecision(9, 6); e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.Shop).WithMany().HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.ContentCategory).WithMany(x => x.Contents).HasForeignKey(x => x.ContentCategoryId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.District).WithMany(x => x.Contents).HasForeignKey(x => x.DistrictId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.SubDistrict).WithMany(x => x.Contents).HasForeignKey(x => x.SubDistrictId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Tag>(e => { e.HasKey(x => x.TagId); e.Property(x => x.TagName).HasMaxLength(100).IsRequired(); e.Property(x => x.Status).HasMaxLength(30).IsRequired(); e.HasIndex(x => x.TagName).IsUnique(); });
        modelBuilder.Entity<ContentTag>(e => { e.HasKey(x => new { x.ContentId, x.TagId }); e.HasOne(x => x.Content).WithMany(x => x.ContentTags).HasForeignKey(x => x.ContentId).OnDelete(DeleteBehavior.Cascade); e.HasOne(x => x.Tag).WithMany(x => x.ContentTags).HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<ContentRelation>(e => { e.HasKey(x => x.ContentRelationId); e.HasIndex(x => new { x.ContentId, x.RelatedContentId }).IsUnique(); e.HasOne(x => x.Content).WithMany(x => x.RelatedContents).HasForeignKey(x => x.ContentId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.RelatedContent).WithMany(x => x.RelatedToContents).HasForeignKey(x => x.RelatedContentId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<Schedule>(e => { e.HasKey(x => x.ScheduleId); e.Property(x => x.Title).HasMaxLength(250).IsRequired(); e.Property(x => x.Status).HasMaxLength(30).IsRequired(); e.Property(x => x.Latitude).HasPrecision(9, 6); e.Property(x => x.Longitude).HasPrecision(9, 6); e.HasOne(x => x.Content).WithMany(x => x.Schedules).HasForeignKey(x => x.ContentId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<DataSource>(e => { e.HasKey(x => x.DataSourceId); e.Property(x => x.SourceName).HasMaxLength(250).IsRequired(); e.Property(x => x.SourceType).HasMaxLength(100); e.HasOne(x => x.Content).WithMany(x => x.DataSources).HasForeignKey(x => x.ContentId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<ContentView>(e => { e.HasKey(x => x.ContentViewId); e.Property(x => x.UserId).HasMaxLength(450); e.Property(x => x.SessionId).HasMaxLength(150); e.HasOne(x => x.Content).WithMany(x => x.Views).HasForeignKey(x => x.ContentId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<ContentFavorite>(e => { e.HasKey(x => x.ContentFavoriteId); e.Property(x => x.UserId).HasMaxLength(450).IsRequired(); e.HasIndex(x => new { x.UserId, x.ContentId }).IsUnique(); e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade); e.HasOne(x => x.Content).WithMany(x => x.Favorites).HasForeignKey(x => x.ContentId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<Review>(e =>
        {
            e.HasKey(x => x.ReviewId);
            e.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            e.Property(x => x.Comment).HasMaxLength(2000).IsRequired();
            e.Property(x => x.Reply).HasMaxLength(2000);
            e.Property(x => x.Status).HasMaxLength(30).IsRequired();
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Content).WithMany(x => x.Reviews).HasForeignKey(x => x.ContentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Product).WithMany(x => x.Reviews).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Shop).WithMany(x => x.Reviews).HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Report>(e =>
        {
            e.HasKey(x => x.ReportId);
            e.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            e.Property(x => x.Reason).HasMaxLength(300).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Status).HasMaxLength(30).IsRequired();
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Content).WithMany(x => x.Reports).HasForeignKey(x => x.ContentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Review).WithMany(x => x.Reports).HasForeignKey(x => x.ReviewId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureShopEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShopCategory>(entity =>
        {
            entity.HasKey(x => x.ShopCategoryId);
            entity.Property(x => x.CategoryName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.ImageData).HasColumnType("varbinary(max)");
            entity.Property(x => x.ImageContentType).HasMaxLength(100);
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
        });

        modelBuilder.Entity<Shop>(entity =>
        {
            entity.HasKey(x => x.ShopId);
            entity.Property(x => x.OwnerUserId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.ShopName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.CoverImageUrl).HasMaxLength(500);
            entity.Property(x => x.BackgroundImageUrl).HasMaxLength(500);
            entity.Property(x => x.OpeningTime).HasMaxLength(5);
            entity.Property(x => x.ClosingTime).HasMaxLength(5);
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Latitude).HasPrecision(9, 6);
            entity.Property(x => x.Longitude).HasPrecision(9, 6);
            entity.HasOne<AppUser>().WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ShopCategory).WithMany(x => x.Shops).HasForeignKey(x => x.ShopCategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.District).WithMany(x => x.Shops).HasForeignKey(x => x.DistrictId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SubDistrict).WithMany(x => x.Shops).HasForeignKey(x => x.SubDistrictId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.OwnerUserId).IsUnique();
        });
    }
}
