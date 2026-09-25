using Microsoft.EntityFrameworkCore;
using GreenMart.Models;

namespace GreenMart.Data
{
    public class ApplicationDbContext : DbContext
    {

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options
        )
            : base(options)
        {
        }



        public DbSet<User> Users { get; set; }



        public DbSet<Product> Products { get; set; }



        public DbSet<Category> Categories { get; set; }



        public DbSet<Cart> Carts { get; set; }



        public DbSet<CartItem> CartItems { get; set; }



        public DbSet<Order> Orders { get; set; }



        public DbSet<OrderItem> OrderItems { get; set; }

        public DbSet<Payment> Payments { get; set; }

        public DbSet<SellerPayoutAccount> SellerPayoutAccounts { get; set; }

        public DbSet<SellerEarning> SellerEarnings { get; set; }

        public DbSet<SellerPayout> SellerPayouts { get; set; }



        public DbSet<Review> Reviews { get; set; }



        public DbSet<DeliveryManApplication> DeliveryManApplications { get; set; }



        public DbSet<DeliveryAssignment> DeliveryAssignments { get; set; }



        public DbSet<DeliveryRating> DeliveryRatings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Payment>()
                .HasIndex(x => x.TransactionId)
                .IsUnique();

            modelBuilder.Entity<Payment>()
                .HasIndex(x => x.OrderId)
                .IsUnique();

            modelBuilder.Entity<Payment>()
                .HasOne(x => x.Order)
                .WithOne(x => x.Payment)
                .HasForeignKey<Payment>(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SellerPayoutAccount>()
                .HasIndex(x => x.SellerId)
                .IsUnique();

            modelBuilder.Entity<SellerEarning>()
                .HasIndex(x => x.DeliveryAssignmentId)
                .IsUnique();

            modelBuilder.Entity<SellerPayout>()
                .HasIndex(x => x.SellerEarningId)
                .IsUnique();

            modelBuilder.Entity<SellerPayout>()
                .HasIndex(x => x.ExternalReference)
                .IsUnique()
                .HasFilter("[ExternalReference] IS NOT NULL");

            modelBuilder.Entity<SellerPayout>()
                .HasOne(x => x.Earning)
                .WithOne(x => x.Payout)
                .HasForeignKey<SellerPayout>(x => x.SellerEarningId)
                .OnDelete(DeleteBehavior.Cascade);
        }

    }
}
