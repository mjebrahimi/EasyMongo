using MongoDB.Bson;
using System;

namespace EasyMongo
{
    public static class IdGenerator
    {
        private static readonly Random _random = new Random();

        /// <summary>
        /// Generates a random value of a given type.
        /// </summary>
        /// <typeparam name="TKey">The type of the value to generate.</typeparam>
        /// <returns>A value of type TKey.</returns>
        public static TKey GenerateNewId<TKey>()
        {
            var idTypeName = typeof(TKey).Name;
            return idTypeName switch
            {
                "Guid" => (TKey)(object)Guid.NewGuid(),
                "Int16" => (TKey)(object)_random.Next(1, short.MaxValue),
                "Int32" => (TKey)(object)_random.Next(1, int.MaxValue),
                "Int64" => (TKey)(object)(_random.NextLong(1, long.MaxValue)),
                "String" => (TKey)(object)Guid.NewGuid().ToString(),
                "ObjectId" => (TKey)(object)ObjectId.GenerateNewId(),
                _ => throw new ArgumentException($"{idTypeName} is not a supported Id type, the Id of the document cannot be set."),
            };
        }
    }
}
