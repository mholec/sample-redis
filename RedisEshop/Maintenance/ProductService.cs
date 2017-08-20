using RedisEshop.Entities;

namespace RedisEshop.Maintenance
{
    public class ProductService
    {
	    private readonly AppDbContext _db;
	    private readonly RedisBackgroundServices _redisBackgroundServices;

	    public ProductService(AppDbContext db, RedisBackgroundServices redisBackgroundServices)
	    {
		    _db = db;
		    _redisBackgroundServices = redisBackgroundServices;
	    }

	    public void AddNewProduct(Product product)
	    {
		    _db.Products.Add(product);
		    _db.SaveChanges();

			_redisBackgroundServices.AddNewProduct(product);
	    }
    }
}
