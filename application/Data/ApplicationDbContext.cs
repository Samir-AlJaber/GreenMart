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



        public DbSet<Review> Reviews { get; set; }

    }
}