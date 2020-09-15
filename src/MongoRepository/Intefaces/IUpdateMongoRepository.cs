using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EasyMongo
{
    public interface IUpdateMongoRepository<TEntity> : IBulkMongoRepository<TEntity, ObjectId>
        where TEntity : IEntity<ObjectId>
    {
    }

    public interface IUpdateMongoRepository<TEntity, TKey>
        where TEntity : IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        void Update(TEntity entity);
        void Update(IEnumerable<TEntity> entities);

        Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task UpdateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    }
}
