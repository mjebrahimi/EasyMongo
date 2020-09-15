using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace EasyMongo
{
    public interface IDeleteMongoRepository<TEntity> : IBulkMongoRepository<TEntity, ObjectId>
        where TEntity : IEntity<ObjectId>
    {
    }

    public interface IDeleteMongoRepository<TEntity, TKey>
        where TEntity : IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        void Delete(TEntity entity);
        void Delete(IEnumerable<TEntity> entities);
        void Delete(TKey id);
        void Delete(IEnumerable<TKey> keys);
        void Delete(Expression<Func<TEntity, bool>> predicate);

        Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task DeleteAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
        Task DeleteAsync(TKey id, CancellationToken cancellationToken = default);
        Task DeleteAsync(IEnumerable<TKey> keys, CancellationToken cancellationToken = default);
        Task DeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    }
}
