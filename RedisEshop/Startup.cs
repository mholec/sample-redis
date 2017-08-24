using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using RedisEshop.DataServices;
using RedisEshop.Entities;
using RedisEshop.Maintenance;
using RedisEshop.Serialization;
using RedLock;
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
			services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
	        services.AddScoped<CommonListService, CommonListService>();
	        services.AddScoped<RedisBackgroundServices, RedisBackgroundServices>();
	        services.AddScoped<IEshopDataService, EshopDataService>();
	        services.AddScoped<RedisService, RedisService>();
	        services.AddScoped(typeof(IDistributedCacheSerializer<>), typeof(ProtobufDistributedCacheSerializer<>));

	        services.AddResponseCaching();
	        services.AddSession();

			// REDIS
	        var redisConnectionString = Configuration.GetConnectionString("RedisConnection"); 
	        ConfigurationOptions configOptions = ConfigurationOptions.Parse(redisConnectionString);
	        configOptions.AbortOnConnectFail = false;
	        configOptions.ConnectRetry = 3;
	        configOptions.ConnectTimeout = 3000;
	        configOptions.AllowAdmin = true; // na vlastni riziko
	        services.AddSingleton(x => ConnectionMultiplexer.Connect(configOptions));

			// REDLOCK
	        services.AddScoped(x => new RedisLockFactory(configOptions.EndPoints));


			// DISTRIBUTED CACHE
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
	        app.UseSession();

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
