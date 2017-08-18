using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RedisEshop.DataServices;
using RedisEshop.DataServices.WithRedis;
using RedisEshop.Entities;
using RedisEshop.Maintenance;
using StackExchange.Redis;

namespace RedisEshop
{
    public class Startup
    {
		public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
			// SERVICES
	        services.AddScoped<CommonListService, CommonListService>();
	        services.AddScoped<RedisBackgroundServices, RedisBackgroundServices>();
	        services.AddScoped<IEshopDataService, EshopRedisDataService>();
	        services.AddScoped<RedisService, RedisService>();

	        services.AddResponseCaching();

			// REDIS
	        var redisConnectionString = Configuration.GetConnectionString("RedisConnection"); 
	        ConfigurationOptions opt = ConfigurationOptions.Parse(redisConnectionString);
	        opt.AbortOnConnectFail = false;
	        opt.ConnectRetry = 3;
	        opt.ConnectTimeout = 3000;
	        opt.AllowAdmin = true; // na vlastni riziko
	        services.AddSingleton(x => ConnectionMultiplexer.Connect(opt));

			services.AddDistributedRedisCache(options =>
			{
				options.Configuration = redisConnectionString;
				options.InstanceName = "distributedcache:"; // tvoří prefix klíčů (hashů)
			});

			// DBCONTEXT (ENTITY FRAMEWORK)
			services.AddDbContext<AppDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));
            
			// MVC
			services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

	        app.UseResponseCaching();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

			InitializeDatabase(app);
        }

	    private void InitializeDatabase(IApplicationBuilder app)
	    {
		    using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
		    {
			    serviceScope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
		    }
	    }
    }
}
