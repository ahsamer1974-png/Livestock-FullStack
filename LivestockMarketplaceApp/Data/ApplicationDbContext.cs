using LivestockMarketplaceApp.Models;
using LivestockMarketplaceAppMVC.Models;
using Microsoft.EntityFrameworkCore;

namespace LivestockMarketplaceApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }
        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Listing> Listings { get; set; }
        public DbSet<ListingImage> ListingImages { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<Order> Orders { get; set; }      // لعمليات الشراء
        public DbSet<Review> Reviews { get; set; }    // لتقييم البائعين
        public DbSet<Message> Messages { get; set; }  // للمحادثات بين المستخدمين
        public DbSet<TransportRequest> TransportRequests { get; set; }
        public DbSet<Driver> Drivers { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. حل مشكلة المفضلة 
            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // 2. حل مشكلة الطلبات 
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Buyer)
                .WithMany()
                .HasForeignKey(o => o.BuyerId)
                .OnDelete(DeleteBehavior.NoAction);

            // --- الطريقة الجديدة للتعامل مع المراجعات والرسائل ---

            // 3. تعطيل الحذف التلقائي لأي علاقة (Foreign Key) موجودة داخل جدول المراجعات
            foreach (var foreignKey in modelBuilder.Entity<Review>().Metadata.GetForeignKeys())
            {
                foreignKey.DeleteBehavior = DeleteBehavior.NoAction;
            }

            // 4. تعطيل الحذف التلقائي لأي علاقة (Foreign Key) موجودة داخل جدول الرسائل
            foreach (var foreignKey in modelBuilder.Entity<Message>().Metadata.GetForeignKeys())
            {
                foreignKey.DeleteBehavior = DeleteBehavior.NoAction;
            }
        }
    }

}
