using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace EasyMongo
{
    public interface IBulkMongoRepository<TEntity> : IBulkMongoRepository<TEntity, ObjectId>
        where TEntity : IEntity<ObjectId>
    {
    }

    public interface IBulkMongoRepository<TEntity, TKey>
        where TEntity : IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        void BeginBulkOperation();
        void DoBulkOperation(BulkWriteOptions options = null);
        void WithBulkOperation(Action action, BulkWriteOptions options = null);
        Task DoBulkOperationAsync(BulkWriteOptions options = null);
        Task WithBulkOperationAsync(Func<Task> func, BulkWriteOptions options = null);
    }
}
