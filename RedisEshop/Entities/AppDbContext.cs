using Microsoft.EntityFrameworkCore;

namespace RedisEshop.Entities
{
    public class AppDbContext : DbContext
    {
	    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
	    {
	    }

		public DbSet<Product> Products { get; set; }
		public DbSet<Tag> Tags { get; set; }
		public DbSet<ProductTag> ProductTags { get; set; }

	    protected override void OnModelCreating(ModelBuilder modelBuilder)
	    {
		    // table names
			modelBuilder.Entity<Product>().ToTable("Products");
		    modelBuilder.Entity<Tag>().ToTable("Tags");
		    modelBuilder.Entity<ProductTag>().ToTable("ProductTags");

			// relationships
		    modelBuilder.Entity<Product>().HasMany(x => x.ProductTags).WithOne(x => x.Product).HasForeignKey(x => x.ProductId);
		    modelBuilder.Entity<Tag>().HasMany(x => x.ProductTags).WithOne(x => x.Tag).HasForeignKey(x => x.TagId);

		    modelBuilder.Entity<Product>().HasKey(x => x.ProductId);
		    modelBuilder.Entity<Tag>().HasKey(x => x.TagId);
		    modelBuilder.Entity<ProductTag>().HasKey(x => new { x.ProductId, x.TagId});
	    }
    }
}
