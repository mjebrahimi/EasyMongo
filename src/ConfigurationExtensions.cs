using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace EasyMongo
{
    public static class ConfigurationExtensions
    {
        public static IServiceCollection AddMongoDbContext(this IServiceCollection services, string connectionString)
        {
            var client = new MongoClient(connectionString);
            return services.AddMongoDbContext(client, new MongoUrl(connectionString).DatabaseName);
        }

        public static IServiceCollection AddMongoDbContext(this IServiceCollection services, MongoUrl url)
        {
            var client = new MongoClient(url);
            return services.AddMongoDbContext(client, url.DatabaseName);
        }

        public static IServiceCollection AddMongoDbContext(this IServiceCollection services, MongoClientSettings setting, string database)
        {
            var client = new MongoClient(setting);
            return services.AddMongoDbContext(client, database);
        }

        public static IServiceCollection AddMongoDbContext(this IServiceCollection services, MongoClient client, string database)
        {
            //BsonDefaults.GuidRepresentation = GuidRepresentation.Standard;

            services.AddSingleton(client);
            //MongoDatabaseSettings
            services.AddSingleton(client.GetDatabase(database));

            //services.AddSingleton<IMongoDbContext>(provider => new MongoDbContext(provider.GetRequiredService<IMongoDatabase>(), provider));
            //services.AddSingleton(typeof(IMongoRepository<>), typeof(MongoRepository<>));
            //services.AddSingleton(typeof(IMongoRepository<,>), typeof(MongoRepository<,>));

            services.AddScoped<IMongoDbContext, MongoDbContext>();
            //services.AddScoped<IMongoDbContext>(provider => new MongoDbContext(provider.GetRequiredService<IMongoDatabase>(), provider));

            services.AddScoped(typeof(MongoRepository<>), typeof(MongoRepository<>));
            services.AddScoped(typeof(MongoRepository<,>), typeof(MongoRepository<,>));

            //services.AddScoped(typeof(IMongoRepository<>), typeof(MongoRepository<>));
            //services.AddScoped(typeof(IMongoRepository<,>), typeof(MongoRepository<,>));

            return services;
        }
    }
}
