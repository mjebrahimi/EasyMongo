using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EasyMongo
{
    public interface ICreateMongoRepsitory<TEntity> : IBulkMongoRepository<TEntity, ObjectId>
        where TEntity : IEntity<ObjectId>
    {
    }

    public interface ICreateMongoRepsitory<TEntity, TKey>
        where TEntity : IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        void Add(TEntity entity);
        void Add(IEnumerable<TEntity> entities);

        Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task AddAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    }
}
