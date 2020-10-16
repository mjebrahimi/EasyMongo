using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EasyMongo
{
    public interface IMongoDbContext
    {
        IMongoClient Client { get; }
        IMongoDatabase Database { get; }
        IClientSessionHandle CurrentClientSessionHandle { get; }

        void BeginTransaction(ClientSessionOptions sessionOptions = null, TransactionOptions transactionOptions = null);
        Task BeginTransactionAsync(ClientSessionOptions sessionOptions = null, TransactionOptions transactionOptions = null, CancellationToken cancellationToken = default);
        string GetCollectionName<TEntity>();
        void CommitTransaction();
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        void DropCollection<TEntity>();
        Task DropCollectionAsync<TEntity>(CancellationToken cancellationToken = default);
        IMongoRepository<TEntity> GetRepository<TEntity>() where TEntity : IEntity<ObjectId>;
        IMongoRepository<TEntity, TKey> GetRepository<TEntity, TKey>() where TEntity : IEntity<TKey> where TKey : IEquatable<TKey>;
        IMongoCollection<TEntity> GetCollection<TEntity>(MongoCollectionSettings settings = null);
        void RollBackTransaction();
        Task RollBackTransactionAsync(CancellationToken cancellationToken = default);
        void UseTransaction(IClientSessionHandle clientSessionHandle);
        void WithTransaction(Action action, ClientSessionOptions sessionOptions = null, TransactionOptions transactionOptions = null);
        Task WithTransactionAsync(Func<Task> func, ClientSessionOptions sessionOptions = null, TransactionOptions transactionOptions = null, CancellationToken cancellationToken = default);
    }
}