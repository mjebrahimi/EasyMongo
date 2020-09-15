using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace EasyMongo
{
    public interface IReadMongoRepository<TEntity> : IBulkMongoRepository<TEntity, ObjectId>
        where TEntity : IEntity<ObjectId>
    {
    }

    public interface IReadMongoRepository<TEntity, TKey>
        where TEntity : IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        IMongoCollection<TEntity> Collection { get; }
        IMongoQueryable<TEntity> Queryable { get; }

        TEntity GetById(TKey id);
        List<TEntity> GetAll(Expression<Func<TEntity, bool>> predicate);
        long GetCount(Expression<Func<TEntity, bool>> predicate);
        bool GetAny(Expression<Func<TEntity, bool>> predicate);
        //long GetCount();
        //bool GetAny();

        Task<TEntity> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
        Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
        Task<long> GetCountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
        Task<bool> GetAnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
        //Task<long> GetCountAsync(CancellationToken cancellationToken = default);
        //Task<bool> GetAnyAsync(CancellationToken cancellationToken = default);
    }
}
