using MongoDB.Bson;
using System;

namespace EasyMongo
{
    public interface IMongoRepository<TEntity> :
        IMongoRepository<TEntity, ObjectId>,
        IReadMongoRepository<TEntity>,
        ICreateMongoRepsitory<TEntity>,
        IUpdateMongoRepository<TEntity>,
        IDeleteMongoRepository<TEntity>,
        IIndexMongoRepository<TEntity>,
        IBulkMongoRepository<TEntity>
        where TEntity : IEntity<ObjectId>
    {
    }

    public interface IMongoRepository<TEntity, TKey> :
        IReadMongoRepository<TEntity, TKey>,
        ICreateMongoRepsitory<TEntity, TKey>,
        IUpdateMongoRepository<TEntity, TKey>,
        IDeleteMongoRepository<TEntity, TKey>,
        IIndexMongoRepository<TEntity, TKey>,
        IBulkMongoRepository<TEntity, TKey>
        where TEntity : IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        IMongoDbContext DbContext { get; }
    }
}
