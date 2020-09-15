using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace EasyMongo
{
    public interface IIndexMongoRepository<TEntity> : IBulkMongoRepository<TEntity, ObjectId>
        where TEntity : IEntity<ObjectId>
    {
    }

    public interface IIndexMongoRepository<TEntity, TKey>
        where TEntity : IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        List<string> GetIndexesNames();
        string CreateTextIndex(Expression<Func<TEntity, object>> field, CreateIndexOptions options = null);
        string CreateAscendingIndex(Expression<Func<TEntity, object>> field, CreateIndexOptions options = null);
        string CreateDescendingIndex(Expression<Func<TEntity, object>> field, CreateIndexOptions options = null);
        string CreateHashedIndex(Expression<Func<TEntity, object>> field, CreateIndexOptions options = null);
        string CreateCombinedTextIndex(IEnumerable<Expression<Func<TEntity, object>>> fields, CreateIndexOptions options = null);
        void DropIndex(string indexName);

        Task<List<string>> GetIndexesNamesAsync(CancellationToken cancellationToken = default);
        Task<string> CreateTextIndexAsync(Expression<Func<TEntity, object>> field, CreateIndexOptions options = null, CancellationToken cancellationToken = default);
        Task<string> CreateAscendingIndexAsync(Expression<Func<TEntity, object>> field, CreateIndexOptions options = null, CancellationToken cancellationToken = default);
        Task<string> CreateDescendingIndexAsync(Expression<Func<TEntity, object>> field, CreateIndexOptions options = null, CancellationToken cancellationToken = default);
        Task<string> CreateHashedIndexAsync(Expression<Func<TEntity, object>> field, CreateIndexOptions options = null, CancellationToken cancellationToken = default);
        Task<string> CreateCombinedTextIndexAsync(IEnumerable<Expression<Func<TEntity, object>>> fields, CreateIndexOptions options = null, CancellationToken cancellationToken = default);
        Task DropIndexAsync(string indexName, CancellationToken cancellationToken = default);
    }
}
