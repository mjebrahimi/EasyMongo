using EasyMongo.Conventions;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EasyMongo
{
    public class MongoDbContext : IMongoDbContext
    {
        public IMongoClient Client { get; }

        public IMongoDatabase Database { get; }

        #region Ctor
        static MongoDbContext()
        {
            var pack = new ConventionPack() { new IgnoreEmptyArraysConvention() };
            ConventionRegistry.Register("Do not serialize empty lists", pack, _ => true);
        }

        public MongoDbContext(IMongoDatabase mongoDatabase)
        {
            Database = mongoDatabase;
            Client = mongoDatabase.Client;
        }

        public MongoDbContext(IMongoClient client, string databaseName)
        {
            Client = client;
            Database = client.GetDatabase(databaseName);
        }

        public MongoDbContext(string connectionString, string databaseName)
        {
            Client = new MongoClient(connectionString);
            Database = Client.GetDatabase(databaseName);
        }

        public MongoDbContext(string connectionString)
            : this(connectionString, new MongoUrl(connectionString).DatabaseName)
        {
        }
        #endregion

        //public IMongoRepository<TEntity> GetRepository<TEntity>() where TEntity : IEntity<ObjectId>
        //{
        //    return _serviceProvider.GetRequiredService<IMongoRepository<TEntity>>();
        //}

        //public IMongoRepository<TEntity, TKey> GetRepository<TEntity, TKey>() where TEntity : IEntity<TKey> where TKey : IEquatable<TKey>
        //{
        //    return _serviceProvider.GetRequiredService<IMongoRepository<TEntity, TKey>>();
        //}

        #region Collection
        public void DropCollection<TEntity>()
        {
            Database.DropCollection(GetCollectionName<TEntity>());
        }

        public Task DropCollectionAsync<TEntity>(CancellationToken cancellationToken = default)
        {
            return Database.DropCollectionAsync(GetCollectionName<TEntity>(), cancellationToken);
        }

        public IMongoCollection<TEntity> GetCollection<TEntity>(MongoCollectionSettings settings = null)
        {
            return Database.GetCollection<TEntity>(GetCollectionName<TEntity>(), settings);
        }
        #endregion

        #region Transaction/ClientSessionHandle
        public IClientSessionHandle CurrentClientSessionHandle { get; private set; }

        public void UseTransaction(IClientSessionHandle clientSessionHandle)
        {
            CurrentClientSessionHandle = clientSessionHandle;
        }

        public void BeginTransaction(ClientSessionOptions sessionOptions = null, TransactionOptions transactionOptions = null) //StartSession
        {
            if (CurrentClientSessionHandle != null)
                throw new InvalidOperationException("There is already an active transaction");

            CurrentClientSessionHandle = Client.StartSession(sessionOptions);
            CurrentClientSessionHandle.StartTransaction(transactionOptions);
        }

        public void CommitTransaction()
        {
            if (CurrentClientSessionHandle == null)
                throw new InvalidOperationException("There is no active session.");

            CurrentClientSessionHandle.CommitTransaction();
            CurrentClientSessionHandle.Dispose();
            CurrentClientSessionHandle = null;
        }

        public void RollBackTransaction()
        {
            if (CurrentClientSessionHandle == null)
                throw new InvalidOperationException("There is no active session.");

            CurrentClientSessionHandle.AbortTransaction();
            CurrentClientSessionHandle.Dispose();
            CurrentClientSessionHandle = null;
        }

        public void WithTransaction(Action action, ClientSessionOptions sessionOptions = null, TransactionOptions transactionOptions = null)
        {
            BeginTransaction(sessionOptions, transactionOptions);
            try
            {
                action();
                CommitTransaction();
            }
            catch
            {
                RollBackTransaction();
                throw;
            }
        }

        public async Task BeginTransactionAsync(ClientSessionOptions sessionOptions = null, TransactionOptions transactionOptions = null, CancellationToken cancellationToken = default) //StartSession
        {
            if (CurrentClientSessionHandle != null)
                throw new InvalidOperationException();

            CurrentClientSessionHandle = await Client.StartSessionAsync(sessionOptions, cancellationToken).ConfigureAwait(false);
            CurrentClientSessionHandle.StartTransaction(transactionOptions);
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (CurrentClientSessionHandle == null)
                throw new InvalidOperationException("There is no active session.");

            await CurrentClientSessionHandle.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
            CurrentClientSessionHandle.Dispose();
            CurrentClientSessionHandle = null;
        }

        public async Task RollBackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (CurrentClientSessionHandle == null)
                throw new InvalidOperationException("There is no active session.");

            await CurrentClientSessionHandle.AbortTransactionAsync(cancellationToken).ConfigureAwait(false);
            CurrentClientSessionHandle.Dispose();
            CurrentClientSessionHandle = null;
        }

        public async Task WithTransactionAsync(Func<Task> func, ClientSessionOptions sessionOptions = null, TransactionOptions transactionOptions = null, CancellationToken cancellationToken = default)
        {
            await BeginTransactionAsync(sessionOptions, transactionOptions, cancellationToken).ConfigureAwait(false);
            try
            {
                await func().ConfigureAwait(false);
                await CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await RollBackTransactionAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }
        #endregion

        //public void ClientMethods()
        //{
        //    Client.DropDatabase();
        //    Client.DropDatabaseAsync();
        //    Client.GetDatabase();
        //    Client.ListDatabaseNames();
        //    Client.ListDatabaseNamesAsync();
        //    Client.ListDatabases();
        //    Client.ListDatabasesAsync();
        //    Client.StartSession();
        //    Client.StartSessionAsync();
        //    Client.Watch();
        //    Client.WatchAsync();
        //    //-----------
        //    Client.WithReadConcern();
        //    Client.WithReadPreference();
        //    Client.WithWriteConcern();
        //}

        //public void DatabaseMethods()
        //{
        //    Database.Watch();
        //    Database.WatchAsync();
        //    Database.RunCommand();
        //    Database.RunCommandAsync();
        //    Database.RenameCollection();
        //    Database.RenameCollectionAsync();
        //    Database.ListCollectionNames();
        //    Database.ListCollectionNamesAsync();
        //    Database.ListCollections();
        //    Database.ListCollectionsAsync();
        //    //-----------
        //    Database.WithReadPreference(null)
        //    Database.WithReadConcern(null)
        //    Database.WithWriteConcern(null);
        //}

        public string GetCollectionName<TEntity>()
        {
            return CollectionNameFinder.GetCollectionName<TEntity>();
        }
    }
}
