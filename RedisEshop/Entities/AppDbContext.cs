using Microsoft.EntityFrameworkCore;

namespace RedisEshop.Entities
{
    public class AppDbContext : DbContext
    {
	    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
	    {
	    }

		public DbSet<PostalCode> PostalCodes { get; set; }
		public DbSet<Product> Products { get; set; }
		public DbSet<Tag> Tags { get; set; }
		public DbSet<ProductTag> ProductTags { get; set; }
		public DbSet<Order> Orders { get; set; }
		public DbSet<OrderItem> OrderItems { get; set; }

	    protected override void OnModelCreating(ModelBuilder modelBuilder)
	    {
		    // table names
			modelBuilder.Entity<Product>().ToTable("Products");
		    modelBuilder.Entity<Tag>().ToTable("Tags");
		    modelBuilder.Entity<ProductTag>().ToTable("ProductTags");
		    modelBuilder.Entity<OrderItem>().ToTable("OrderItems");
		    modelBuilder.Entity<Order>().ToTable("Orders");
		    modelBuilder.Entity<PostalCode>().ToTable("PostalCodes");

			// relationships
		    modelBuilder.Entity<Product>().HasMany(x => x.ProductTags).WithOne(x => x.Product).HasForeignKey(x => x.ProductId);
		    modelBuilder.Entity<Product>().HasMany(x => x.OrderedItems).WithOne(x => x.Product).HasForeignKey(x => x.ProductId);
		    modelBuilder.Entity<Tag>().HasMany(x => x.ProductTags).WithOne(x => x.Tag).HasForeignKey(x => x.TagId);
		    modelBuilder.Entity<Order>().HasMany(x => x.OrderItems).WithOne(x => x.Order).HasForeignKey(x => x.OrderId);
		    modelBuilder.Entity<OrderItem>().HasOne(x => x.Order).WithMany(x => x.OrderItems).HasForeignKey(x => x.OrderId);
		    modelBuilder.Entity<OrderItem>().HasOne(x => x.Product).WithMany(x => x.OrderedItems).HasForeignKey(x => x.ProductId);

			// pks
		    modelBuilder.Entity<Product>().HasKey(x => x.ProductId);
		    modelBuilder.Entity<PostalCode>().HasKey(x => new {x.Code, x.Name });
		    modelBuilder.Entity<Tag>().HasKey(x => x.TagId);
		    modelBuilder.Entity<OrderItem>().HasKey(x => x.OrderItemId);
		    modelBuilder.Entity<Order>().HasKey(x => x.OrderId);
		    modelBuilder.Entity<ProductTag>().HasKey(x => new { x.ProductId, x.TagId});
		    modelBuilder.Entity<PostalCode>().Property(x => x.Code).ValueGeneratedNever();
	    }
    }
}
