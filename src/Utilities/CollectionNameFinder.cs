using EasyMongo.Conventions;
using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace EasyMongo
{
    public static class CollectionNameFinder
    {
        private static readonly ConcurrentDictionary<Type, string> dictionary = new ConcurrentDictionary<Type, string>();

        public static string GetCollectionName<TEntity>()
        {
            return GetCollectionName(typeof(TEntity));
        }

        public static string GetCollectionName(Type type)
        {
            return dictionary.GetOrAdd(type, _ =>
            {
                var collectionName = FindNameByAttribute(type);
                return collectionName ?? CollectionNameConvention.GetDefaultConvention().GetCollectionName(type);
            });
        }

        private static string FindNameByAttribute(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            var tableAttribute = typeInfo.GetCustomAttribute<CollectionNameAttribute>(false);
            return tableAttribute?.Name;
        }

        public static IMongoCollection<TDocument> GetCollection<TDocument>(this IMongoDatabase database, MongoCollectionSettings settings = null)
        {
            var name = GetCollectionName<TDocument>();
            return database.GetCollection<TDocument>(name, settings);
        }
    }
}
