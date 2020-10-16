using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace EasyMongo
{
    public class MongoRepository<TEntity> : MongoRepository<TEntity, ObjectId>//, IMongoRepository<TEntity>
        where TEntity : IEntity<ObjectId>
    {
        public MongoRepository(IMongoDbContext dbContext)
            : base(dbContext)
        {
        }
    }

    public class MongoRepository<TEntity, TKey> //: IMongoRepository<TEntity, TKey>
        where TEntity : IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        public IMongoDbContext DbContext { get; }
        public IMongoCollection<TEntity> Collection { get; }
        public IMongoQueryable<TEntity> Queryable => Collection.AsQueryable();

        public MongoRepository(IMongoDbContext dbContext)
        {
            DbContext = dbContext;
            Collection = dbContext.GetCollection<TEntity>();
        }

        //public MongoRepository(IMongoCollection<TEntity> collection)
        //{
        //    Collection = collection;
        //}

        /////////////////////////////////////////////////////////
        ///             IClientSessionHandle
        ///             Skip, Sort, Limit
        ///
        /// Find(filter) is same as Find(Builders<TDocument>.Filter.Where(filter))
        /// But Queryable.Where(filter) uses Pipeline and it's half as slow
        ///
        /// Collection.Find() is as IQuerable and does not execute (apply limit on database when First method calls)
        /// Collection.FindAsync() is as IEnumerable and execute immediately (apply limit on memory/code when First method calls)
        ///
        /// [Index], [NotMapped], [
        /// [ReadConcerne], [WriteConcerne], [ReadPerfrence]
        /// [Table], [Column], [Key]
        /// [ExtraElements], [CreatedDate], [UpdatedDate], [DeletedDate]
        ///
        /// Support Buckets : https://mongodb.github.io/mongo-csharp-driver/2.9/reference/gridfs/
        /// Special Queries : https://github.com/TurnerSoftware/MongoFramework#special-queries
        /// OnModelCreating : https://github.com/marcrabadan/MongoDbContext#1---inherits-from-the-mongodbcontext-class
        /////////////////////////////////////////////////////////

        #region Add
        //Removed because of overlaps with other overrides
        //public void Add(TEntity entity) => Add(entity, default(bool?), default(CancellationToken));
        public void Add(TEntity entity, bool? bypassValidation = null) => Add(entity, bypassValidation, default);
        public void Add(TEntity entity, CancellationToken cancellationToken = default) => Add(entity, default, cancellationToken);
        public void Add(TEntity entity, bool? bypassValidation, CancellationToken cancellationToken)
        {
            SetId(entity);

            if (_bulkOperationMode)
            {
                AddInsertOneModel(entity);
            }
            else
            {
                var options = new InsertOneOptions { BypassDocumentValidation = bypassValidation };
                if (DbContext.CurrentClientSessionHandle != null)
                    Collection.InsertOne(DbContext.CurrentClientSessionHandle, entity, options, cancellationToken);
                else
                    Collection.InsertOne(entity, options, cancellationToken);
            }
        }

        //public void Add(IEnumerable<TEntity> entities, bool? bypassValidation = false, bool isOrdered = false, CancellationToken cancellationToken = default)
        //{
        //    SetId(entities);
        //    var options = new InsertManyOptions { BypassDocumentValidation = bypassValidation, IsOrdered = isOrdered };
        //    Collection.InsertMany(entities, options, cancellationToken);
        //}

        public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            SetId(entity);
            //InsertOneOptions
            return Collection.InsertOneAsync(entity, new InsertOneOptions(), cancellationToken);
        }

        public Task AddAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            SetId(entities);
            //InsertManyOptions
            return Collection.InsertManyAsync(entities, new InsertManyOptions(), cancellationToken);
        }
        #endregion

        #region Delete
        public void Delete(TEntity entity, CancellationToken cancellationToken = default)
        {
            var result = Collection.DeleteOne(p => p.Id.Equals(entity.Id), cancellationToken);
            if (result.DeletedCount != 1)
                throw new Exception();
        }

        public void Delete(TKey id, CancellationToken cancellationToken = default)
        {
            var result = Collection.DeleteOne(p => p.Id.Equals(id), cancellationToken);
            if (result.DeletedCount != 1)
                throw new Exception();
        }

        public void Delete(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            if (!entities.Any())
                return;
            //DeleteOptions
            var ids = entities.Select(p => p.Id).ToArray();
            var result = Collection.DeleteMany(p => ids.Contains(p.Id), cancellationToken);
            if (result.DeletedCount != entities.Count())
                throw new Exception();
        }

        public void Delete(IEnumerable<TKey> ids, CancellationToken cancellationToken = default)
        {
            if (!ids.Any())
                return;
            //DeleteOptions
            var result = Collection.DeleteMany(p => ids.Contains(p.Id), cancellationToken);
            if (result.DeletedCount != ids.Count())
                throw new Exception();
        }

        public void Delete(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            //DeleteOptions
            var result = Collection.DeleteMany(predicate, cancellationToken);
            if (result.IsAcknowledged == false)
                throw new Exception();
        }

        protected void DeleteAll(CancellationToken cancellationToken = default)
        {
            //DeleteOptions
            var result = Collection.DeleteMany(_ => true, cancellationToken);
            if (result.IsAcknowledged == false)
                throw new Exception();
        }

        public async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            //DeleteOptions
            var result = await Collection.DeleteOneAsync(p => p.Id.Equals(entity.Id), cancellationToken).ConfigureAwait(false);
            if (result.DeletedCount != 1)
                throw new Exception();
        }

        public async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default)
        {
            //DeleteOptions
            var result = await Collection.DeleteOneAsync(p => p.Id.Equals(id), cancellationToken);
            if (result.DeletedCount != 1)
                throw new Exception();
        }

        public async Task DeleteAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            if (!entities.Any())
                return;
            //DeleteOptions
            var ids = entities.Select(p => p.Id).ToArray();
            var result = await Collection.DeleteManyAsync(p => ids.Contains(p.Id), cancellationToken).ConfigureAwait(false);
            if (result.DeletedCount != entities.Count())
                throw new Exception();
        }

        public async Task DeleteAsync(IEnumerable<TKey> ids, CancellationToken cancellationToken = default)
        {
            if (!ids.Any())
                return;
            //DeleteOptions
            var result = await Collection.DeleteManyAsync(p => ids.Contains(p.Id), cancellationToken).ConfigureAwait(false);
            if (result.DeletedCount != ids.Count())
                throw new Exception();
        }

        public async Task DeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            //DeleteOptions
            var result = await Collection.DeleteManyAsync(predicate, cancellationToken).ConfigureAwait(false);
            if (result.IsAcknowledged == false)
                throw new Exception();
        }

        //protected async Task DeleteAllAsync(CancellationToken cancellationToken = default)
        //{
        //    //var result = await Collection.DeleteManyAsync("{}", cancellationToken);
        //    //DeleteOptions
        //    var result = await Collection.DeleteManyAsync(_ => true, cancellationToken);
        //    if (result.IsAcknowledged == false)
        //        throw new Exception();
        //}
        #endregion

        #region Update

        #region Sync
        public void Update(TEntity entity) => Update(entity, default(ReplaceOptions), default);
        public void Update(TEntity entity, ReplaceOptions options) => Update(entity, options, default);
        public void Update(TEntity entity, CancellationToken cancellationToken) => Update(entity, default(ReplaceOptions), cancellationToken);
        public void Update(TEntity entity, ReplaceOptions options, CancellationToken cancellationToken)
        {
            var result = Collection.ReplaceOne(p => p.Id.Equals(entity.Id), entity, options, cancellationToken);
            if (result.IsAcknowledged == false)
                throw new Exception();
        }

        public void Update(IEnumerable<TEntity> entities) => Update(entities, default, default);
        public void Update(IEnumerable<TEntity> entities, BulkWriteOptions options) => Update(entities, options, default);
        public void Update(IEnumerable<TEntity> entities, CancellationToken cancellationToken) => Update(entities, default, cancellationToken);
        public void Update(IEnumerable<TEntity> entities, BulkWriteOptions options, CancellationToken cancellationToken)
        {
            var updateModel = CreateReplaceOneModels(entities);
            var result = Collection.BulkWrite(updateModel, options, cancellationToken);
            if (result.IsAcknowledged == false)
                throw new Exception();
        }

        protected void Update(TEntity entity, UpdateDefinition<TEntity> updateDefinition) => Update(entity, updateDefinition, default, default);
        protected void Update(TEntity entity, UpdateDefinition<TEntity> updateDefinition, UpdateOptions options) => Update(entity, updateDefinition, options, default);
        protected void Update(TEntity entity, UpdateDefinition<TEntity> updateDefinition, CancellationToken cancellationToken) => Update(entity, updateDefinition, default, cancellationToken);
        protected void Update(TEntity entity, UpdateDefinition<TEntity> updateDefinition, UpdateOptions options, CancellationToken cancellationToken)
        {
            var result = Collection.UpdateOne(p => p.Id.Equals(entity.Id), updateDefinition, options, cancellationToken);
            if (result.IsAcknowledged == false)
                throw new Exception();
        }

        protected void Update(TKey id, UpdateDefinition<TEntity> updateDefinition) => Update(id, updateDefinition, default, default);
        protected void Update(TKey id, UpdateDefinition<TEntity> updateDefinition, UpdateOptions options) => Update(id, updateDefinition, options, default);
        protected void Update(TKey id, UpdateDefinition<TEntity> updateDefinition, CancellationToken cancellationToken) => Update(id, updateDefinition, default, cancellationToken);
        protected void Update(TKey id, UpdateDefinition<TEntity> updateDefinition, UpdateOptions options, CancellationToken cancellationToken)
        {
            var result = Collection.UpdateOne(p => p.Id.Equals(id), updateDefinition, options, cancellationToken);
            if (result.IsAcknowledged == false)
                throw new Exception();
        }

        protected void Update<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> field, TProperty value) => Update(entity, field, value, default, default);
        protected void Update<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> field, TProperty value, UpdateOptions options) => Update(entity, field, value, options, default);
        protected void Update<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> field, TProperty value, CancellationToken cancellationToken) => Update(entity, field, value, default, cancellationToken);
        protected void Update<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> field, TProperty value, UpdateOptions options, CancellationToken cancellationToken)
        {
            var result = Collection.UpdateOne(p => p.Id.Equals(entity.Id), Builders<TEntity>.Update.Set(field, value), options, cancellationToken);
            if (result.IsAcknowledged == false)
                throw new Exception();
        }

        protected void Update<TProperty>(TKey id, Expression<Func<TEntity, TProperty>> field, TProperty value) => Update(id, field, value, default, default);
        protected void Update<TProperty>(TKey id, Expression<Func<TEntity, TProperty>> field, TProperty value, UpdateOptions options) => Update(id, field, value, options, default);
        protected void Update<TProperty>(TKey id, Expression<Func<TEntity, TProperty>> field, TProperty value, CancellationToken cancellationToken) => Update(id, field, value, default, cancellationToken);
        protected void Update<TProperty>(TKey id, Expression<Func<TEntity, TProperty>> field, TProperty value, UpdateOptions options, CancellationToken cancellationToken)
        {
            var result = Collection.UpdateOne(p => p.Id.Equals(id), Builders<TEntity>.Update.Set(field, value), options, cancellationToken);
            if (result.IsAcknowledged == false)
                throw new Exception();
        }

        protected void Update(Expression<Func<TEntity, bool>> predicate, UpdateDefinition<TEntity> update) => Update(predicate, update, default, default);
        protected void Update(Expression<Func<TEntity, bool>> predicate, UpdateDefinition<TEntity> update, UpdateOptions options) => Update(predicate, update, options, default);
        protected void Update(Expression<Func<TEntity, bool>> predicate, UpdateDefinition<TEntity> update, CancellationToken cancellationToken) => Update(predicate, update, default, cancellationToken);
        protected void Update(Expression<Func<TEntity, bool>> predicate, UpdateDefinition<TEntity> update, UpdateOptions options, CancellationToken cancellationToken)
        {
            var result = Collection.UpdateMany(predicate, update, options, cancellationToken); //UpdateOne
            if (result.IsAcknowledged == false)
                throw new Exception();
        }

        protected void Update<TProperty>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProperty>> field, TProperty value) => Update(predicate, field, value, default, default);
        protected void Update<TProperty>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProperty>> field, TProperty value, UpdateOptions options) => Update(predicate, field, value, options, default);
        protected void Update<TProperty>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProperty>> field, TProperty value, CancellationToken cancellationToken) => Update(predicate, field, value, default, cancellationToken);
        protected void Update<TProperty>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProperty>> field, TProperty value, UpdateOptions options, CancellationToken cancellationToken)
        {
            var result = Collection.UpdateMany(predicate, Builders<TEntity>.Update.Set(field, value), options, cancellationToken); //UpdateOne
            if (result.IsAcknowledged == false)
                throw new Exception();
        }
        #endregion

        #region Async
        public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            //new ReplaceOptions()
            //new UpdateOptions()
            var result = await Collection.ReplaceOneAsync(p => p.Id.Equals(entity.Id), entity).ConfigureAwait(false);
            if (result.IsAcknowledged == false)
                throw new Exception();
        }

        public async Task UpdateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            var updateModel = CreateReplaceOneModels(entities);
            //BulkWriteOptions
            var result = await Collection.BulkWriteAsync(updateModel, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (result.IsAcknowledged == false)
                throw new Exception();
        }

        protected async Task UpdateAsync(TEntity entity, UpdateDefinition<TEntity> update, CancellationToken cancellationToken = default)
        {
            //new UpdateOptions { IsUpsert = true }
            var result = await Collection.UpdateOneAsync(p => p.Id.Equals(entity.Id), update, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (result.IsAcknowledged == false)
                throw new Exception();
        }

        protected async Task UpdateAsync(TKey id, UpdateDefinition<TEntity> update, CancellationToken cancellationToken = default)
        {
            //new UpdateOptions { IsUpsert = true }
            var result = await Collection.UpdateOneAsync(p => p.Id.Equals(id), update, cancellationToken: cancellationToken).ConfigureAwait(false); //UpdateManyAsync
            if (result.IsAcknowledged == false)
                throw new Exception();
        }

        protected async Task UpdateAsync<TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> field, TProperty value, CancellationToken cancellationToken = default)
        {
            //new UpdateOptions { IsUpsert = true }
            var result = await Collection.UpdateOneAsync(p => p.Id.Equals(entity.Id), Builders<TEntity>.Update.Set(field, value), cancellationToken: cancellationToken).ConfigureAwait(false);
            if (result.IsAcknowledged == false)
                throw new Exception();
        }

        //protected async Task UpdateAsync<TProperty>(TKey key, Expression<Func<TEntity, TProperty>> field, TProperty value, CancellationToken cancellationToken = default)
        //{
        //    var filter = GetFilterByKey(key);
        //    //new UpdateOptions { IsUpsert = true }
        //    var result = await Collection.UpdateOneAsync(filter, Builders<TEntity>.Update.Set(field, value), cancellationToken: cancellationToken);
        //    if (result.IsAcknowledged == false)
        //        throw new Exception();
        //}

        protected async Task UpdateAsync(Expression<Func<TEntity, bool>> predicate, UpdateDefinition<TEntity> update, CancellationToken cancellationToken = default)
        {
            //new UpdateOptions { IsUpsert = true }
            var result = await Collection.UpdateManyAsync(predicate, update, cancellationToken: cancellationToken).ConfigureAwait(false); //UpdateOneAsync
            if (result.IsAcknowledged == false)
                throw new Exception();
        }

        protected async Task UpdateAsync<TProperty>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProperty>> field, TProperty value, CancellationToken cancellationToken = default)
        {
            //new UpdateOptions { IsUpsert = true }
            var result = await Collection.UpdateManyAsync(predicate, Builders<TEntity>.Update.Set(field, value), cancellationToken: cancellationToken).ConfigureAwait(false); //UpdateOneAsync
            if (result.IsAcknowledged == false)
                throw new Exception();
        }
        #endregion

        #endregion

        #region BulkOperation
        private bool _bulkOperationMode = false;
        private readonly List<WriteModel<TEntity>> _bulkOperations = new List<WriteModel<TEntity>>();

        public void BeginBulkOperation()
        {
            if (_bulkOperationMode)
                throw new Exception();

            //check for thread-safety
            _bulkOperationMode = true;
        }

        public void DoBulkOperation() => DoBulkOperation(default, default);
        public void DoBulkOperation(BulkWriteOptions options) => DoBulkOperation(options, default);
        public void DoBulkOperation(CancellationToken cancellationToken) => DoBulkOperation(default, cancellationToken);
        public void DoBulkOperation(BulkWriteOptions options, CancellationToken cancellationToken)
        {
            try
            {
                var result = Collection.BulkWrite(_bulkOperations, options, cancellationToken);
            }
            finally
            {
                _bulkOperationMode = false;
                _bulkOperations.Clear();
            }
        }

        public void WithBulkOperation(Action action) => WithBulkOperation(action, default, default);
        public void WithBulkOperation(Action action, BulkWriteOptions options) => WithBulkOperation(action, options, default);
        public void WithBulkOperation(Action action, CancellationToken cancellationToken) => WithBulkOperation(action, default, cancellationToken);
        public void WithBulkOperation(Action action, BulkWriteOptions options, CancellationToken cancellationToken)
        {
            BeginBulkOperation();
            action();
            DoBulkOperation(options, cancellationToken);
        }

        public Task DoBulkOperationAsync() => DoBulkOperationAsync(default, default);
        public Task DoBulkOperationAsync(BulkWriteOptions options) => DoBulkOperationAsync(options, default);
        public Task DoBulkOperationAsync(CancellationToken cancellationToken) => DoBulkOperationAsync(default, cancellationToken);
        public async Task DoBulkOperationAsync(BulkWriteOptions options, CancellationToken cancellationToken)
        {
            try
            {
                var result = await Collection.BulkWriteAsync(_bulkOperations, options, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _bulkOperationMode = false;
                _bulkOperations.Clear();
            }
        }

        public Task WithBulkOperationAsync(Func<Task> func) => WithBulkOperationAsync(func, default, default);
        public Task WithBulkOperationAsync(Func<Task> func, BulkWriteOptions options) => WithBulkOperationAsync(func, options, default);
        public Task WithBulkOperationAsync(Func<Task> func, CancellationToken cancellationToken) => WithBulkOperationAsync(func, default, cancellationToken);
        public async Task WithBulkOperationAsync(Func<Task> func, BulkWriteOptions options, CancellationToken cancellationToken)
        {
            BeginBulkOperation();
            await func().ConfigureAwait(false);
            await DoBulkOperationAsync(options, cancellationToken).ConfigureAwait(false);
        }
        #endregion

        #region Read

        #region Get

        #region Sync

        #region GetById
        public TEntity GetById(TKey id) => GetById(id, default, default);
        public TEntity GetById(TKey id, FindOptions options) => GetById(id, options, default);
        public TEntity GetById(TKey id, CancellationToken cancellationToken) => GetById(id, default, cancellationToken);
        public TEntity GetById(TKey id, FindOptions options, CancellationToken cancellationToken)
        {
            return Collection.Find(p => p.Id.Equals(id), options).FirstOrDefault(cancellationToken);
        }
        #endregion

        public IAggregateFluent<TNewResult> Join<TForeignDocument, TNewResult>(IMongoCollection<TForeignDocument> foreignCollection, Expression<Func<TEntity, object>> localField, Expression<Func<TForeignDocument, object>> foreignField, Expression<Func<TNewResult, object>> @as, AggregateLookupOptions<TForeignDocument, TNewResult> options = null)
        {
            return Collection.Aggregate()
                .Lookup(foreignCollection, localField, foreignField, @as, options);
        }

        #region GetAll
        public List<TEntity> GetAll() => GetAll(_ => true, default, default);
        public List<TEntity> GetAll(FindOptions options) => GetAll(_ => true, options, default);
        public List<TEntity> GetAll(CancellationToken cancellationToken) => GetAll(_ => true, default, cancellationToken);
        public List<TEntity> GetAll(FindOptions options, CancellationToken cancellationToken) => GetAll(_ => true, options, cancellationToken);
        public List<TEntity> GetAll(Expression<Func<TEntity, bool>> predicate) => GetAll(predicate, default, default);
        public List<TEntity> GetAll(Expression<Func<TEntity, bool>> predicate, FindOptions options) => GetAll(predicate, options, default);
        public List<TEntity> GetAll(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken) => GetAll(predicate, default, cancellationToken);
        public List<TEntity> GetAll(Expression<Func<TEntity, bool>> predicate, FindOptions options, CancellationToken cancellationToken)
        {
            return Collection.Find(predicate, options).ToList(cancellationToken);
        }
        #endregion

        #region First
        public TEntity First() => First(_ => true, default, default);
        public TEntity First(FindOptions options) => First(_ => true, options, default);
        public TEntity First(CancellationToken cancellationToken) => First(_ => true, default, cancellationToken);
        public TEntity First(Expression<Func<TEntity, bool>> predicate) => First(predicate, default, default);
        public TEntity First(Expression<Func<TEntity, bool>> predicate, FindOptions options) => First(predicate, options, default);
        public TEntity First(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken) => First(predicate, default, cancellationToken);
        protected TEntity First(Expression<Func<TEntity, bool>> predicate, FindOptions options, CancellationToken cancellationToken)
        {
            return Collection.Find(predicate, options).First(cancellationToken);
        }
        #endregion

        #region FirstOrDefault
        public TEntity FirstOrDefault() => FirstOrDefault(_ => true, default, default);
        public TEntity FirstOrDefault(FindOptions options) => FirstOrDefault(_ => true, options, default);
        public TEntity FirstOrDefault(CancellationToken cancellationToken) => FirstOrDefault(_ => true, default, cancellationToken);
        public TEntity FirstOrDefault(Expression<Func<TEntity, bool>> predicate) => FirstOrDefault(predicate, default, default);
        public TEntity FirstOrDefault(Expression<Func<TEntity, bool>> predicate, FindOptions options) => FirstOrDefault(predicate, options, default);
        public TEntity FirstOrDefault(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken) => FirstOrDefault(predicate, default, cancellationToken);
        protected TEntity FirstOrDefault(Expression<Func<TEntity, bool>> predicate, FindOptions options, CancellationToken cancellationToken)
        {
            return Collection.Find(predicate, options).FirstOrDefault(cancellationToken);
        }
        #endregion

        #region Single
        public TEntity Single() => Single(_ => true, default, default);
        public TEntity Single(FindOptions options) => Single(_ => true, options, default);
        public TEntity Single(CancellationToken cancellationToken) => Single(_ => true, default, cancellationToken);
        public TEntity Single(Expression<Func<TEntity, bool>> predicate) => Single(predicate, default, default);
        public TEntity Single(Expression<Func<TEntity, bool>> predicate, FindOptions options) => Single(predicate, options, default);
        public TEntity Single(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken) => Single(predicate, default, cancellationToken);
        protected TEntity Single(Expression<Func<TEntity, bool>> predicate, FindOptions options, CancellationToken cancellationToken)
        {
            return Collection.Find(predicate, options).Single(cancellationToken);
        }
        #endregion

        #region SingleOrDefault
        public TEntity SingleOrDefault() => SingleOrDefault(_ => true, default, default);
        public TEntity SingleOrDefault(FindOptions options) => SingleOrDefault(_ => true, options, default);
        public TEntity SingleOrDefault(CancellationToken cancellationToken) => SingleOrDefault(_ => true, default, cancellationToken);
        public TEntity SingleOrDefault(Expression<Func<TEntity, bool>> predicate) => SingleOrDefault(predicate, default, default);
        public TEntity SingleOrDefault(Expression<Func<TEntity, bool>> predicate, FindOptions options) => SingleOrDefault(predicate, options, default);
        public TEntity SingleOrDefault(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken) => SingleOrDefault(predicate, default, cancellationToken);
        protected TEntity SingleOrDefault(Expression<Func<TEntity, bool>> predicate, FindOptions options, CancellationToken cancellationToken)
        {
            return Collection.Find(predicate, options).SingleOrDefault(cancellationToken);
        }
        #endregion

        #region Count
        public long Count() => Count(_ => true, default, default);
        public long Count(FindOptions options) => Count(_ => true, options, default);
        public long Count(CancellationToken cancellationToken) => Count(_ => true, default, cancellationToken);
        public long Count(Expression<Func<TEntity, bool>> predicate) => Count(predicate, default, default);
        public long Count(Expression<Func<TEntity, bool>> predicate, FindOptions options) => Count(predicate, options, default);
        public long Count(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken) => Count(predicate, default, cancellationToken);
        public long Count(Expression<Func<TEntity, bool>> predicate, FindOptions options, CancellationToken cancellationToken)
        {
            //All is as same
            //return Queryable.Count(predicate); //=> int
            //return Queryable.LongCount(predicate);
            return Collection.Find(predicate, options).CountDocuments(cancellationToken);
            //Collection.CountDocuments
        }
        #endregion

        #region Any
        public bool Any() => Any(_ => true, default, default);
        public bool Any(FindOptions options) => Any(_ => true, options, default);
        public bool Any(CancellationToken cancellationToken) => Any(_ => true, default, cancellationToken);
        public bool Any(Expression<Func<TEntity, bool>> predicate) => Any(predicate, default, default);
        public bool Any(Expression<Func<TEntity, bool>> predicate, FindOptions options) => Any(predicate, options, default);
        public bool Any(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken) => Any(predicate, default, cancellationToken);
        public bool Any(Expression<Func<TEntity, bool>> predicate, FindOptions options, CancellationToken cancellationToken)
        {
            //return Queryable.Any(predicate); //select one
            //return Collection.Find(predicate).Any(); //select all
            return Collection.Find(predicate, options).CountDocuments(cancellationToken) > 0; //count all
        }
        #endregion

        #endregion

        #region Async
        public Task<TEntity> ByIdAsync(TKey id, CancellationToken cancellationToken = default)
        {
            //FindOptions
            return Collection.Find(p => p.Id.Equals(id)).FirstOrDefaultAsync(cancellationToken);
        }

        public Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            //FindOptions
            return Collection.Find(predicate).ToListAsync(cancellationToken);
        }

        protected Task<TEntity> FirstAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            //FindOptions
            return Collection.Find(predicate).FirstOrDefaultAsync(cancellationToken);
        }

        protected Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            //FindOptions
            return Collection.Find(predicate).FirstOrDefaultAsync(cancellationToken);
        }

        protected Task<TEntity> SingleAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            //FindOptions
            return Collection.Find(predicate).SingleAsync(cancellationToken);
        }

        protected Task<TEntity> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            //FindOptions
            return Collection.Find(predicate).SingleOrDefaultAsync(cancellationToken);
        }

        public Task<long> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            //FindOptions
            //return Queryable.CountAsync(predicate, cancellationToken); => int
            //return Queryable.LongCountAsync(predicate, cancellationToken);
            return Collection.Find(predicate).CountDocumentsAsync(cancellationToken);
        }

        public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            //return (await Collection.Find(predicate).CountDocumentsAsync(cancellationToken)) > 0;
            //return Queryable.AnyAsync(predicate, cancellationToken);
            return Collection.Find(predicate).AnyAsync(cancellationToken);
        }
        #endregion

        #endregion

        #region GetByMax - GetByMin - GetMaxValue - GetMinValue - GetAverageValue

        #endregion

        #region GetSum (int - decimal)

        #endregion

        #region Project

        #region Sync

        #region ProjectAll
        protected List<TProjection> ProjectAll<TProjection>(Expression<Func<TEntity, TProjection>> projection) => ProjectAll(_ => true, projection, default, default);
        protected List<TProjection> ProjectAll<TProjection>(Expression<Func<TEntity, TProjection>> projection, FindOptions options) => ProjectAll(_ => true, projection, options, default);
        protected List<TProjection> ProjectAll<TProjection>(Expression<Func<TEntity, TProjection>> projection, CancellationToken cancellationToken) => ProjectAll(_ => true, projection, default, cancellationToken);
        protected List<TProjection> ProjectAll<TProjection>(Expression<Func<TEntity, TProjection>> projection, FindOptions options, CancellationToken cancellationToken) => ProjectAll(_ => true, projection, options, cancellationToken);
        protected List<TProjection> ProjectAll<TProjection>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProjection>> projection) => ProjectAll(predicate, projection, default, default);
        protected List<TProjection> ProjectAll<TProjection>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProjection>> projection, FindOptions options) => ProjectAll(predicate, projection, options, default);
        protected List<TProjection> ProjectAll<TProjection>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProjection>> projection, CancellationToken cancellationToken) => ProjectAll(predicate, projection, default, cancellationToken);
        protected List<TProjection> ProjectAll<TProjection>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProjection>> projection, FindOptions options, CancellationToken cancellationToken)
        {
            return Collection.Find(predicate, options).Project(projection).ToList(cancellationToken);
        }
        #endregion

        #region ProjectFirst
        protected TProjection ProjectFirst<TProjection>(Expression<Func<TEntity, TProjection>> projection) => ProjectFirst(_ => true, projection, default, default);
        protected TProjection ProjectFirst<TProjection>(Expression<Func<TEntity, TProjection>> projection, FindOptions options) => ProjectFirst(_ => true, projection, options, default);
        protected TProjection ProjectFirst<TProjection>(Expression<Func<TEntity, TProjection>> projection, CancellationToken cancellationToken) => ProjectFirst(_ => true, projection, default, cancellationToken);
        protected TProjection ProjectFirst<TProjection>(Expression<Func<TEntity, TProjection>> projection, FindOptions options, CancellationToken cancellationToken) => ProjectFirst(_ => true, projection, options, cancellationToken);
        protected TProjection ProjectFirst<TProjection>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProjection>> projection) => ProjectFirst(predicate, projection, default, default);
        protected TProjection ProjectFirst<TProjection>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProjection>> projection, FindOptions options) => ProjectFirst(predicate, projection, options, default);
        protected TProjection ProjectFirst<TProjection>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProjection>> projection, CancellationToken cancellationToken) => ProjectFirst(predicate, projection, default, cancellationToken);
        protected TProjection ProjectFirst<TProjection>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProjection>> projection, FindOptions options, CancellationToken cancellationToken)
        {
            return Collection.Find(predicate, options).Project(projection).First(cancellationToken);
        }
        #endregion

        #region ProjectFirstOrDefault
        protected TProjection ProjectFirstOrDefault<TProjection>(Expression<Func<TEntity, TProjection>> projection) => ProjectFirstOrDefault(_ => true, projection, default, default);
        protected TProjection ProjectFirstOrDefault<TProjection>(Expression<Func<TEntity, TProjection>> projection, FindOptions options) => ProjectFirstOrDefault(_ => true, projection, options, default);
        protected TProjection ProjectFirstOrDefault<TProjection>(Expression<Func<TEntity, TProjection>> projection, CancellationToken cancellationToken) => ProjectFirstOrDefault(_ => true, projection, default, cancellationToken);
        protected TProjection ProjectFirstOrDefault<TProjection>(Expression<Func<TEntity, TProjection>> projection, FindOptions options, CancellationToken cancellationToken) => ProjectFirstOrDefault(_ => true, projection, options, cancellationToken);
        protected TProjection ProjectFirstOrDefault<TProjection>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProjection>> projection) => ProjectFirstOrDefault(predicate, projection, default, default);
        protected TProjection ProjectFirstOrDefault<TProjection>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProjection>> projection, FindOptions options) => ProjectFirstOrDefault(predicate, projection, options, default);
        protected TProjection ProjectFirstOrDefault<TProjection>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProjection>> projection, CancellationToken cancellationToken) => ProjectFirstOrDefault(predicate, projection, default, cancellationToken);
        protected TProjection ProjectFirstOrDefault<TProjection>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProjection>> projection, FindOptions options, CancellationToken cancellationToken)
        {
            return Collection.Find(predicate, options).Project(projection).FirstOrDefault(cancellationToken);
        }
        #endregion

        #region ProjectSingle
        protected TProjection ProjectSingle<TProjection>(Expression<Func<TEntity, TProjection>> projection) => ProjectSingle(_ => true, projection, default, default);
        protected TProjection ProjectSingle<TProjection>(Expression<Func<TEntity, TProjection>> projection, FindOptions options) => ProjectSingle(_ => true, projection, options, default);
        protected TProjection ProjectSingle<TProjection>(Expression<Func<TEntity, TProjection>> projection, CancellationToken cancellationToken) => ProjectSingle(_ => true, projection, default, cancellationToken);
        protected TProjection ProjectSingle<TProjection>(Expression<Func<TEntity, TProjection>> projection, FindOptions options, CancellationToken cancellationToken) => ProjectSingle(_ => true, projection, options, cancellationToken);
        protected TProjection ProjectSingle<TProjection>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProjection>> projection) => ProjectSingle(predicate, projection, default, default);
        protected TProjection ProjectSingle<TProjection>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProjection>> projection, FindOptions options) => ProjectSingle(predicate, projection, options, default);
        protected TProjection ProjectSingle<TProjection>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProjection>> projection, CancellationToken cancellationToken) => ProjectSingle(predicate, projection, default, cancellationToken);
        protected TProjection ProjectSingle<TProjection>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProjection>> projection, FindOptions options, CancellationToken cancellationToken)
        {
            return Collection.Find(predicate, options).Project(projection).Single(cancellationToken);
        }
        #endregion

        #region ProjectSingleOrDefault
        protected TProjection ProjectSingleOrDefault<TProjection>(Expression<Func<TEntity, TProjection>> projection) => ProjectSingleOrDefault(_ => true, projection, default, default);
        protected TProjection ProjectSingleOrDefault<TProjection>(Expression<Func<TEntity, TProjection>> projection, FindOptions options) => ProjectSingleOrDefault(_ => true, projection, options, default);
        protected TProjection ProjectSingleOrDefault<TProjection>(Expression<Func<TEntity, TProjection>> projection, CancellationToken cancellationToken) => ProjectSingleOrDefault(_ => true, projection, default, cancellationToken);
        protected TProjection ProjectSingleOrDefault<TProjection>(Expression<Func<TEntity, TProjection>> projection, FindOptions options, CancellationToken cancellationToken) => ProjectSingleOrDefault(_ => true, projection, options, cancellationToken);
        protected TProjection ProjectSingleOrDefault<TProjection>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProjection>> projection) => ProjectSingleOrDefault(predicate, projection, default, default);
        protected TProjection ProjectSingleOrDefault<TProjection>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProjection>> projection, FindOptions options) => ProjectSingleOrDefault(predicate, projection, options, default);
        protected TProjection ProjectSingleOrDefault<TProjection>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProjection>> projection, CancellationToken cancellationToken) => ProjectSingleOrDefault(predicate, projection, default, cancellationToken);
        protected TProjection ProjectSingleOrDefault<TProjection>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProjection>> projection, FindOptions options, CancellationToken cancellationToken)
        {
            return Collection.Find(predicate, options).Project(projection).SingleOrDefault(cancellationToken);
        }
        #endregion

        #endregion

        #region Async
        protected Task<List<TProjection>> ProjectListAsync<TProjection>(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TProjection>> projection,
            CancellationToken cancellationToken = default)
            where TProjection : class, new()
        {
            //FindOptions
            return Collection.Find(filter).Project(projection).ToListAsync(cancellationToken);
        }

        protected Task<TProjection> ProjectFirstAsync<TProjection>(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TProjection>> projection,
            CancellationToken cancellationToken = default)
            where TProjection : class, new()
        {
            //FindOptions
            return Collection.Find(filter).Project(projection).FirstAsync(cancellationToken);
        }

        protected Task<TProjection> ProjectFirstOrDefaultAsync<TProjection>(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TProjection>> projection,
            CancellationToken cancellationToken = default)
            where TProjection : class, new()
        {
            //FindOptions
            return Collection.Find(filter).Project(projection).FirstOrDefaultAsync(cancellationToken);
        }

        protected Task<TProjection> ProjectSingleAsync<TProjection>(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TProjection>> projection,
            CancellationToken cancellationToken = default)
            where TProjection : class, new()
        {
            //FindOptions
            return Collection.Find(filter).Project(projection).SingleAsync(cancellationToken);
        }

        protected Task<TProjection> ProjectSingleOrDefaultAsync<TProjection>(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, TProjection>> projection,
            CancellationToken cancellationToken = default)
            where TProjection : class, new()
        {
            //FindOptions
            return Collection.Find(filter).Project(projection).SingleOrDefaultAsync(cancellationToken);
        }
        #endregion

        #endregion

        #region GroupBy
        //protected List<TProjection> GroupBy<TGroupKey, TProjection>(
        //    Expression<Func<TEntity, TGroupKey>> grouping,
        //    Expression<Func<IGrouping<TGroupKey, TEntity>, TProjection>> projection,
        //    Expression<Func<TEntity, bool>> filter = null)
        //{
        //    return GroupByAggregate(grouping, projection, filter).ToList();
        //}

        //protected bool GroupByAny<TGroupKey, TProjection>(
        //    Expression<Func<TEntity, TGroupKey>> grouping,
        //    Expression<Func<IGrouping<TGroupKey, TEntity>, TProjection>> projection,
        //    Expression<Func<TEntity, bool>> filter = null)
        //{
        //    return GroupByAggregate(grouping, projection, filter).Any();
        //}

        //protected TProjection GroupByFirst<TGroupKey, TProjection>(
        //    Expression<Func<TEntity, TGroupKey>> grouping,
        //    Expression<Func<IGrouping<TGroupKey, TEntity>, TProjection>> projection,
        //    Expression<Func<TEntity, bool>> filter = null)
        //{
        //    return GroupByAggregate(grouping, projection, filter).First();
        //}

        //protected TProjection GroupByFirstOrDefault<TGroupKey, TProjection>(
        //    Expression<Func<TEntity, TGroupKey>> grouping,
        //    Expression<Func<IGrouping<TGroupKey, TEntity>, TProjection>> projection,
        //    Expression<Func<TEntity, bool>> filter = null)
        //{
        //    return GroupByAggregate(grouping, projection, filter).FirstOrDefault();
        //}

        //protected TProjection GroupBySingle<TGroupKey, TProjection>(
        //    Expression<Func<TEntity, TGroupKey>> grouping,
        //    Expression<Func<IGrouping<TGroupKey, TEntity>, TProjection>> projection,
        //    Expression<Func<TEntity, bool>> filter = null)
        //{
        //    return GroupByAggregate(grouping, projection, filter).Single();
        //}

        //protected TProjection GroupBySingleOrDefault<TGroupKey, TProjection>(
        //    Expression<Func<TEntity, TGroupKey>> grouping,
        //    Expression<Func<IGrouping<TGroupKey, TEntity>, TProjection>> projection,
        //    Expression<Func<TEntity, bool>> filter = null)
        //{
        //    return GroupByAggregate(grouping, projection, filter).SingleOrDefault();
        //}

        //protected Task<List<TProjection>> GroupByListAsync<TGroupKey, TProjection>(
        //    Expression<Func<TEntity, TGroupKey>> grouping,
        //    Expression<Func<IGrouping<TGroupKey, TEntity>, TProjection>> projection,
        //    Expression<Func<TEntity, bool>> filter = null,
        //    CancellationToken cancellationToken = default)
        //{
        //    return GroupByAggregate(grouping, projection, filter).ToListAsync(cancellationToken);
        //}

        //protected Task<bool> GroupByAnyAsync<TGroupKey, TProjection>(
        //    Expression<Func<TEntity, TGroupKey>> grouping,
        //    Expression<Func<IGrouping<TGroupKey, TEntity>, TProjection>> projection,
        //    Expression<Func<TEntity, bool>> filter = null,
        //    CancellationToken cancellationToken = default)
        //{
        //    return GroupByAggregate(grouping, projection, filter).AnyAsync(cancellationToken);
        //}

        //protected Task<TProjection> GroupByFirstAsync<TGroupKey, TProjection>(
        //    Expression<Func<TEntity, TGroupKey>> grouping,
        //    Expression<Func<IGrouping<TGroupKey, TEntity>, TProjection>> projection,
        //    Expression<Func<TEntity, bool>> filter = null,
        //    CancellationToken cancellationToken = default)
        //{
        //    return GroupByAggregate(grouping, projection, filter).FirstAsync(cancellationToken);
        //}

        //protected Task<TProjection> GroupByFirstOrDefaultAsync<TGroupKey, TProjection>(
        //    Expression<Func<TEntity, TGroupKey>> grouping,
        //    Expression<Func<IGrouping<TGroupKey, TEntity>, TProjection>> projection,
        //    Expression<Func<TEntity, bool>> filter = null,
        //    CancellationToken cancellationToken = default)
        //{
        //    return GroupByAggregate(grouping, projection, filter).FirstOrDefaultAsync(cancellationToken);
        //}

        //protected Task<TProjection> GroupBySingleAsync<TGroupKey, TProjection>(
        //    Expression<Func<TEntity, TGroupKey>> grouping,
        //    Expression<Func<IGrouping<TGroupKey, TEntity>, TProjection>> projection,
        //    Expression<Func<TEntity, bool>> filter = null,
        //    CancellationToken cancellationToken = default)
        //{
        //    return GroupByAggregate(grouping, projection, filter).SingleAsync(cancellationToken);
        //}

        //protected Task<TProjection> GroupBySingleOrDefaultAsync<TGroupKey, TProjection>(
        //    Expression<Func<TEntity, TGroupKey>> grouping,
        //    Expression<Func<IGrouping<TGroupKey, TEntity>, TProjection>> projection,
        //    Expression<Func<TEntity, bool>> filter = null,
        //    CancellationToken cancellationToken = default)
        //{
        //    return GroupByAggregate(grouping, projection, filter).SingleOrDefaultAsync(cancellationToken);
        //}

        //private IAggregateFluent<TProjection> GroupByAggregate<TGroupKey, TProjection>(
        //    Expression<Func<TEntity, TGroupKey>> groupingCriteria,
        //    Expression<Func<IGrouping<TGroupKey, TEntity>, TProjection>> groupProjection,
        //    Expression<Func<TEntity, bool>> predicate = null)
        //{
        //    var aggregate = Collection.Aggregate();
        //    if (predicate != null)
        //        aggregate.Match(predicate);

        //    return aggregate.Group(groupingCriteria, groupProjection);
        //}
        #endregion

        #region Paging
        protected List<TEntity> GetPaged(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, object>> sortSelector,
            bool ascending = true,
            int? skipNumber = null,
            int? takeNumber = null,
            FindOptions options = null,
            CancellationToken cancellationToken = default)
        {
            var sorting = ascending
                ? Builders<TEntity>.Sort.Ascending(sortSelector)
                : Builders<TEntity>.Sort.Descending(sortSelector);

            /////////////////////////////
            var result = Collection.AsQueryable().Where(predicate).OrderBy(sortSelector).Skip(skipNumber.Value).Take(takeNumber.Value).ToList();
            /////////////////////////////

            var result2 = Collection
                .Find(predicate, options)
                .Sort(sorting)
                .Skip(skipNumber)
                .Limit(takeNumber)
                //.Project()
                .ToList(cancellationToken);

            return result2;
        }

        protected List<TEntity> GetPaged(
            Expression<Func<TEntity, bool>> predicate,
            SortDefinition<TEntity> sortDefinition,
            int? skipNumber = null,
            int? takeNumber = null,
            FindOptions options = null,
            CancellationToken cancellationToken = default)
        {
            return Collection
                .Find(predicate, options)
                .Sort(sortDefinition)
                .Skip(skipNumber)
                .Limit(takeNumber)
                //.Project()
                .ToList(cancellationToken);
        }

        protected Task<List<TEntity>> GetPagedAsync(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, object>> sortSelector,
            bool ascending = true,
            int? skipNumber = null,
            int? takeNumber = null,
            FindOptions options = null,
            CancellationToken cancellationToken = default)
        {
            var sorting = ascending
                ? Builders<TEntity>.Sort.Ascending(sortSelector)
                : Builders<TEntity>.Sort.Descending(sortSelector);

            return Collection
                .Find(predicate, options) //FindAsync
                .Sort(sorting)
                .Skip(skipNumber)
                .Limit(takeNumber)
                //.Project()
                .ToListAsync(cancellationToken);
        }

        protected Task<List<TEntity>> GetPagedAsync(
            Expression<Func<TEntity, bool>> predicate,
            SortDefinition<TEntity> sortDefinition,
            int? skipNumber = null,
            int? takeNumber = null,
            FindOptions options = null,
            CancellationToken cancellationToken = default)
        {
            //FindOptions

            return Collection
                .Find(predicate, options) //FindAsync
                .Sort(sortDefinition)
                .Skip(skipNumber)
                .Limit(takeNumber)
                //.Project()
                .ToListAsync(cancellationToken);
        }
        #endregion

        #endregion

        #region Index

        #region Sync
        public List<string> GetIndexesNames(CancellationToken cancellationToken = default)
        {
            var indexCursor = Collection.Indexes.List(cancellationToken);
            var indexes = indexCursor.ToList(cancellationToken);
            return indexes.Select(e => e["name"].ToString()).ToList();
        }

        public bool IndexExists(string name, CancellationToken cancellationToken = default)
        {
            return GetIndexesNames(cancellationToken).Contains(name);
        }

        protected string CreateIndex(Expression<Func<TEntity, object>> field, bool descending, bool unique, bool sparse = false, string name = null, CancellationToken cancellationToken = default)
        {
            //TODO: Convert new { p.Field1, p.Field2} to Combined index

            var options = new CreateIndexOptions { Unique = unique, Sparse = sparse, Name = name };
            if (descending)
                return Collection.Indexes.CreateOne(new CreateIndexModel<TEntity>(Builders<TEntity>.IndexKeys.Descending(field), options), null, cancellationToken);
            else
                return Collection.Indexes.CreateOne(new CreateIndexModel<TEntity>(Builders<TEntity>.IndexKeys.Ascending(field), options), null, cancellationToken);
        }

        #region AscendingIndex
        //CreateUniqueAscendingIndex
        //public string CreateAscendingIndex(Expression<Func<TEntity, object>> field, bool unique, bool sparse = false, string name = null) => CreateAscendingIndex(field, default(CreateIndexOptions), default(CancellationToken));
        public string CreateAscendingIndex(Expression<Func<TEntity, object>> field) => CreateAscendingIndex(field, default, default);
        public string CreateAscendingIndex(Expression<Func<TEntity, object>> field, CreateIndexOptions options) => CreateAscendingIndex(field, options, default);
        public string CreateAscendingIndex(Expression<Func<TEntity, object>> field, CancellationToken cancellationToken) => CreateAscendingIndex(field, default, cancellationToken);
        public string CreateAscendingIndex(Expression<Func<TEntity, object>> field, CreateIndexOptions options, CancellationToken cancellationToken)
        {
            return Collection.Indexes.CreateOne(
                new CreateIndexModel<TEntity>(
                    Builders<TEntity>.IndexKeys.Ascending(field), options
                ), null, cancellationToken);
        }
        #endregion

        #region DescendingIndex
        //CreateUniqueDescendingIndex
        public string CreateDescendingIndex(Expression<Func<TEntity, object>> field) => CreateDescendingIndex(field, default, default);
        public string CreateDescendingIndex(Expression<Func<TEntity, object>> field, CreateIndexOptions options) => CreateDescendingIndex(field, options, default);
        public string CreateDescendingIndex(Expression<Func<TEntity, object>> field, CancellationToken cancellationToken) => CreateDescendingIndex(field, default, cancellationToken);
        public string CreateDescendingIndex(Expression<Func<TEntity, object>> field, CreateIndexOptions options, CancellationToken cancellationToken)
        {
            return Collection.Indexes.CreateOne(
                new CreateIndexModel<TEntity>(
                    Builders<TEntity>.IndexKeys.Descending(field), options
                ), null, cancellationToken);
        }
        #endregion

        #region TextIndex
        //CreateUniqueTextIndex
        public string CreateTextIndex(Expression<Func<TEntity, object>> field) => CreateTextIndex(field, default, default);
        public string CreateTextIndex(Expression<Func<TEntity, object>> field, CreateIndexOptions options) => CreateTextIndex(field, options, default);
        public string CreateTextIndex(Expression<Func<TEntity, object>> field, CancellationToken cancellationToken) => CreateTextIndex(field, default, cancellationToken);
        public string CreateTextIndex(Expression<Func<TEntity, object>> field, CreateIndexOptions options, CancellationToken cancellationToken)
        {
            return Collection.Indexes.CreateOne(
                new CreateIndexModel<TEntity>(
                    Builders<TEntity>.IndexKeys.Text(field), options
                ), null, cancellationToken);
        }
        #endregion

        #region HashedIndex
        //CreateUniqueHashedIndex
        public string CreateHashedIndex(Expression<Func<TEntity, object>> field) => CreateHashedIndex(field, default, default);
        public string CreateHashedIndex(Expression<Func<TEntity, object>> field, CreateIndexOptions options) => CreateHashedIndex(field, options, default);
        public string CreateHashedIndex(Expression<Func<TEntity, object>> field, CancellationToken cancellationToken) => CreateHashedIndex(field, default, cancellationToken);
        public string CreateHashedIndex(Expression<Func<TEntity, object>> field, CreateIndexOptions options, CancellationToken cancellationToken)
        {
            return Collection.Indexes.CreateOne(
                new CreateIndexModel<TEntity>(
                    Builders<TEntity>.IndexKeys.Hashed(field), options
                ), null, cancellationToken);
        }
        #endregion

        #region CombinedAscendingIndex
        //CreateUniqueCombinedAscendingIndex
        protected string CreateCombinedAscendingIndex(IEnumerable<Expression<Func<TEntity, object>>> fields) => CreateCombinedAscendingIndex(fields, default, default);
        protected string CreateCombinedAscendingIndex(IEnumerable<Expression<Func<TEntity, object>>> fields, CreateIndexOptions options) => CreateCombinedAscendingIndex(fields, options, default);
        protected string CreateCombinedAscendingIndex(IEnumerable<Expression<Func<TEntity, object>>> fields, CancellationToken cancellationToken) => CreateCombinedAscendingIndex(fields, default, cancellationToken);
        protected string CreateCombinedAscendingIndex(IEnumerable<Expression<Func<TEntity, object>>> fields, CreateIndexOptions options, CancellationToken cancellationToken)
        {
            var indexKeys = new List<IndexKeysDefinition<TEntity>>();
            foreach (var field in fields)
                indexKeys.Add(Builders<TEntity>.IndexKeys.Ascending(field));

            return Collection.Indexes.CreateOne(
                new CreateIndexModel<TEntity>(
                    Builders<TEntity>.IndexKeys.Combine(indexKeys), options
                ), null, cancellationToken);
        }
        #endregion

        #region CombinedDescendingIndex
        //CreateUniqueCombinedDescendingIndex
        protected string CreateCombinedDescendingIndex(IEnumerable<Expression<Func<TEntity, object>>> fields) => CreateCombinedDescendingIndex(fields, default, default);
        protected string CreateCombinedDescendingIndex(IEnumerable<Expression<Func<TEntity, object>>> fields, CreateIndexOptions options) => CreateCombinedDescendingIndex(fields, options, default);
        protected string CreateCombinedDescendingIndex(IEnumerable<Expression<Func<TEntity, object>>> fields, CancellationToken cancellationToken) => CreateCombinedDescendingIndex(fields, default, cancellationToken);
        protected string CreateCombinedDescendingIndex(IEnumerable<Expression<Func<TEntity, object>>> fields, CreateIndexOptions options, CancellationToken cancellationToken)
        {
            var indexKeys = new List<IndexKeysDefinition<TEntity>>();
            foreach (var field in fields)
                indexKeys.Add(Builders<TEntity>.IndexKeys.Descending(field));

            return Collection.Indexes.CreateOne(
                new CreateIndexModel<TEntity>(
                    Builders<TEntity>.IndexKeys.Combine(indexKeys), options
                ), null, cancellationToken);
        }
        #endregion

        #region CombinedTextIndex
        //CreateUniqueCombinedTextIndex
        public string CreateCombinedTextIndex(IEnumerable<Expression<Func<TEntity, object>>> fields) => CreateCombinedTextIndex(fields, default, default);
        public string CreateCombinedTextIndex(IEnumerable<Expression<Func<TEntity, object>>> fields, CreateIndexOptions options) => CreateCombinedTextIndex(fields, options, default);
        public string CreateCombinedTextIndex(IEnumerable<Expression<Func<TEntity, object>>> fields, CancellationToken cancellationToken) => CreateCombinedTextIndex(fields, default, cancellationToken);
        public string CreateCombinedTextIndex(IEnumerable<Expression<Func<TEntity, object>>> fields, CreateIndexOptions options, CancellationToken cancellationToken)
        {
            var indexKeys = new List<IndexKeysDefinition<TEntity>>();
            foreach (var field in fields)
                indexKeys.Add(Builders<TEntity>.IndexKeys.Text(field));

            return Collection.Indexes.CreateOne(
                new CreateIndexModel<TEntity>(
                    Builders<TEntity>.IndexKeys.Combine(indexKeys), options
                ), null, cancellationToken);
        }
        #endregion

        #region CombinedHashedIndex
        //CreateUniqueCombinedHashedIndex
        protected string CreateCombinedHashedIndex(IEnumerable<Expression<Func<TEntity, object>>> fields) => CreateCombinedHashedIndex(fields, default, default);
        protected string CreateCombinedHashedIndex(IEnumerable<Expression<Func<TEntity, object>>> fields, CreateIndexOptions options) => CreateCombinedHashedIndex(fields, options, default);
        protected string CreateCombinedHashedIndex(IEnumerable<Expression<Func<TEntity, object>>> fields, CancellationToken cancellationToken) => CreateCombinedHashedIndex(fields, default, cancellationToken);
        protected string CreateCombinedHashedIndex(IEnumerable<Expression<Func<TEntity, object>>> fields, CreateIndexOptions options, CancellationToken cancellationToken)
        {
            var indexKeys = new List<IndexKeysDefinition<TEntity>>();
            foreach (var field in fields)
                indexKeys.Add(Builders<TEntity>.IndexKeys.Hashed(field));

            return Collection.Indexes.CreateOne(
                new CreateIndexModel<TEntity>(
                    Builders<TEntity>.IndexKeys.Combine(indexKeys), options
                ), null, cancellationToken);
        }
        #endregion

        #region CombinedIndex
        //CreateUniqueCombinedIndex
        protected string CreateCombinedIndex(IEnumerable<MongoIndex<TEntity>> indexes) => CreateCombinedIndex(indexes, default, default);
        protected string CreateCombinedIndex(IEnumerable<MongoIndex<TEntity>> indexes, CreateIndexOptions options) => CreateCombinedIndex(indexes, options, default);
        protected string CreateCombinedIndex(IEnumerable<MongoIndex<TEntity>> indexes, CancellationToken cancellationToken) => CreateCombinedIndex(indexes, default, cancellationToken);
        protected string CreateCombinedIndex(IEnumerable<MongoIndex<TEntity>> indexes, CreateIndexOptions options, CancellationToken cancellationToken)
        {
            var indexKeys = GetIndexKeysDefinitions(indexes);
            return Collection.Indexes.CreateOne(
                new CreateIndexModel<TEntity>(
                    Builders<TEntity>.IndexKeys.Combine(indexKeys), options
                ), null, cancellationToken);
        }
        #endregion

        public void DropIndex(string indexName, CancellationToken cancellationToken = default)
        {
            Collection.Indexes.DropOne(indexName, cancellationToken);
        }
        //DropAllIndex()

        #endregion

        #region Async
        protected Task<string> CreateIndexAsync(Expression<Func<TEntity, object>> field, bool descending, bool unique, bool sparse = false, string name = null, CancellationToken cancellationToken = default)
        {
            //TODO: Convert new { p.Field1, p.Field2} to Combined index

            var options = new CreateIndexOptions { Unique = unique, Sparse = sparse, Name = name };
            if (descending)
                return Collection.Indexes.CreateOneAsync(new CreateIndexModel<TEntity>(Builders<TEntity>.IndexKeys.Descending(field), options), cancellationToken: cancellationToken);
            else
                return Collection.Indexes.CreateOneAsync(new CreateIndexModel<TEntity>(Builders<TEntity>.IndexKeys.Ascending(field), options), cancellationToken: cancellationToken);
        }

        public async Task<List<string>> GetIndexesNamesAsync(CancellationToken cancellationToken = default)
        {
            var indexCursor = await Collection.Indexes.ListAsync(cancellationToken).ConfigureAwait(false);
            var indexes = await indexCursor.ToListAsync(cancellationToken).ConfigureAwait(false);
            return indexes.Select(e => e["name"].ToString()).ToList();
        }

        //CreateUniqueTextIndexAsync
        public Task<string> CreateTextIndexAsync(Expression<Func<TEntity, object>> field, CreateIndexOptions options = null, CancellationToken cancellationToken = default)
        {
            return Collection.Indexes.CreateOneAsync(
                new CreateIndexModel<TEntity>(
                    Builders<TEntity>.IndexKeys.Text(field), options
                ), cancellationToken: cancellationToken);
        }

        //CreateUniqueAscendingIndexAsync
        public Task<string> CreateAscendingIndexAsync(Expression<Func<TEntity, object>> field, CreateIndexOptions options = null, CancellationToken cancellationToken = default)
        {
            return Collection.Indexes.CreateOneAsync(
                new CreateIndexModel<TEntity>(
                    Builders<TEntity>.IndexKeys.Ascending(field), options
                ), cancellationToken: cancellationToken);
        }

        //CreateUniqueDescendingIndexAsync
        public Task<string> CreateDescendingIndexAsync(Expression<Func<TEntity, object>> field, CreateIndexOptions options = null, CancellationToken cancellationToken = default)
        {
            return Collection.Indexes.CreateOneAsync(
                new CreateIndexModel<TEntity>(
                    Builders<TEntity>.IndexKeys.Descending(field), options
                ), cancellationToken: cancellationToken);
        }

        //CreateUniqueHashedIndexAsync
        public Task<string> CreateHashedIndexAsync(Expression<Func<TEntity, object>> field, CreateIndexOptions options = null, CancellationToken cancellationToken = default)
        {
            return Collection.Indexes.CreateOneAsync(
                new CreateIndexModel<TEntity>(
                    Builders<TEntity>.IndexKeys.Hashed(field), options
                ), cancellationToken: cancellationToken);
        }

        //CreateUniqueCombinedTextIndexAsync
        public Task<string> CreateCombinedTextIndexAsync(IEnumerable<Expression<Func<TEntity, object>>> fields, CreateIndexOptions options = null, CancellationToken cancellationToken = default)
        {
            var indexKeys = new List<IndexKeysDefinition<TEntity>>();
            foreach (var field in fields)
                indexKeys.Add(Builders<TEntity>.IndexKeys.Text(field));

            return Collection.Indexes.CreateOneAsync(
                new CreateIndexModel<TEntity>(
                    Builders<TEntity>.IndexKeys.Combine(indexKeys), options
                ), cancellationToken: cancellationToken);
        }

        //CreateUniqueCombinedAscendingIndexAsync
        protected Task<string> CreateCombinedAscendingIndexAsync(IEnumerable<Expression<Func<TEntity, object>>> fields, CreateIndexOptions options = null, CancellationToken cancellationToken = default)
        {
            var indexKeys = new List<IndexKeysDefinition<TEntity>>();
            foreach (var field in fields)
                indexKeys.Add(Builders<TEntity>.IndexKeys.Ascending(field));

            return Collection.Indexes.CreateOneAsync(
                new CreateIndexModel<TEntity>(
                    Builders<TEntity>.IndexKeys.Combine(indexKeys), options
                ), cancellationToken: cancellationToken);
        }

        //CreateUniqueCombinedDescendingIndexAsync
        protected Task<string> CreateCombinedDescendingIndexAsync(IEnumerable<Expression<Func<TEntity, object>>> fields, CreateIndexOptions options = null, CancellationToken cancellationToken = default)
        {
            var indexKeys = new List<IndexKeysDefinition<TEntity>>();
            foreach (var field in fields)
                indexKeys.Add(Builders<TEntity>.IndexKeys.Descending(field));

            return Collection.Indexes.CreateOneAsync(
                new CreateIndexModel<TEntity>(
                    Builders<TEntity>.IndexKeys.Combine(indexKeys), options
                ), cancellationToken: cancellationToken);
        }

        //CreateUniqueCombinedHashedIndexAsync
        protected Task<string> CreateCombinedHashedIndexAsync(IEnumerable<Expression<Func<TEntity, object>>> fields, CreateIndexOptions options = null, CancellationToken cancellationToken = default)
        {
            var indexKeys = new List<IndexKeysDefinition<TEntity>>();
            foreach (var field in fields)
                indexKeys.Add(Builders<TEntity>.IndexKeys.Hashed(field));

            return Collection.Indexes.CreateOneAsync(
                new CreateIndexModel<TEntity>(
                    Builders<TEntity>.IndexKeys.Combine(indexKeys), options
                ), cancellationToken: cancellationToken);
        }

        //CreateUniqueCombinedIndexAsync
        protected Task<string> CreateCombinedIndexAsync(IEnumerable<MongoIndex<TEntity>> indexes, CreateIndexOptions options = null, CancellationToken cancellationToken = default)
        {
            var indexKeys = GetIndexKeysDefinitions(indexes);
            return Collection.Indexes.CreateOneAsync(
                new CreateIndexModel<TEntity>(
                    Builders<TEntity>.IndexKeys.Combine(indexKeys), options
                ), cancellationToken: cancellationToken);
        }

        public Task DropIndexAsync(string indexName, CancellationToken cancellationToken = default)
        {
            return Collection.Indexes.DropOneAsync(indexName, cancellationToken);
        }
        //DropAllIndexAsync()
        #endregion

        #endregion

        #region Utilities
        private void SetId(IEnumerable<TEntity> entities)
        {
            entities.NotNull(nameof(entities));

            foreach (var entity in entities)
                SetId(entity);
        }

        private void SetId(TEntity entity)
        {
            entity.NotNull(nameof(entity));

            if (entity.Id.Equals(default))
                entity.Id = IdGenerator.GenerateNewId<TKey>();

            //var defaultTKey = default(TKey);
            //if (entity.Id == null || (defaultTKey != null && defaultTKey.Equals(entity.Id)))
            //    entity.Id = IdGenerator.GenerateNewId<TKey>();
        }

        //private FilterDefinition<TEntity> GetFilterByKey(IEnumerable<TEntity> entities)
        //{
        //    var keys = entities.Select(p => p.Id);
        //    return GetFilterByKey(keys);
        //}

        //private FilterDefinition<TEntity> GetFilterByKey(IEnumerable<TKey> keys)
        //{
        //    //return Builders<TEntity>.Filter.Where(p => keys.Contains(p.Id));
        //    return Builders<TEntity>.Filter.In(p => p.Id, keys);
        //}

        private IEnumerable<ReplaceOneModel<TEntity>> CreateReplaceOneModels(IEnumerable<TEntity> entities)
        {
            entities.NotNull(nameof(entities));

            foreach (var entity in entities)
            {
                if (/*entity.Id == null ||*/ entity.Id.Equals(default))
                    continue;
                var filter = Builders<TEntity>.Filter.Where(p => p.Id.Equals(entity.Id));
                yield return new ReplaceOneModel<TEntity>(filter, entity);
            }
        }

        private void AddInsertOneModel(TEntity entity)
        {
            entity.NotNull(nameof(entity));
            var model = new InsertOneModel<TEntity>(entity);
            _bulkOperations.Add(model);
        }

        private void AddDeleteOneModel(Expression<Func<TEntity, bool>> predicate)
        {
            var filter = Builders<TEntity>.Filter.Where(predicate);
            var model = new DeleteOneModel<TEntity>(filter);
            _bulkOperations.Add(model);
        }

        private void AddDeleteManyModel(Expression<Func<TEntity, bool>> predicate)
        {
            var filter = Builders<TEntity>.Filter.Where(predicate);
            var model = new DeleteManyModel<TEntity>(filter);
            _bulkOperations.Add(model);
        }

        private void AddReplaceOneModel(Expression<Func<TEntity, bool>> predicate, TEntity entity)
        {
            entity.NotNull(nameof(entity));
            var filter = Builders<TEntity>.Filter.Where(predicate);
            var model = new ReplaceOneModel<TEntity>(filter, entity);
            _bulkOperations.Add(model);
        }

        private void AddUpdateOneModel(Expression<Func<TEntity, bool>> predicate, UpdateDefinition<TEntity> updateDefinition)
        {
            updateDefinition.NotNull(nameof(updateDefinition));
            var filter = Builders<TEntity>.Filter.Where(predicate);
            var model = new UpdateOneModel<TEntity>(filter, updateDefinition);
            _bulkOperations.Add(model);
        }

        private void AddUpdateManyModel(Expression<Func<TEntity, bool>> predicate, UpdateDefinition<TEntity> updateDefinition)
        {
            updateDefinition.NotNull(nameof(updateDefinition));
            var filter = Builders<TEntity>.Filter.Where(predicate);
            var model = new UpdateManyModel<TEntity>(filter, updateDefinition);
            _bulkOperations.Add(model);
        }

        private IEnumerable<IndexKeysDefinition<TEntity>> GetIndexKeysDefinitions(IEnumerable<MongoIndex<TEntity>> indexes)
        {
            indexes.NotNull(nameof(indexes));

            foreach (var field in indexes)
            {
                yield return field.Type switch
                {
                    IndexType.Ascending => Builders<TEntity>.IndexKeys.Ascending(field.Field),
                    IndexType.Descending => Builders<TEntity>.IndexKeys.Descending(field.Field),
                    IndexType.Text => Builders<TEntity>.IndexKeys.Text(field.Field),
                    IndexType.Hashed => Builders<TEntity>.IndexKeys.Hashed(field.Field),
                    IndexType.Geo2D => Builders<TEntity>.IndexKeys.Geo2D(field.Field),
                    IndexType.Geo2DSphere => Builders<TEntity>.IndexKeys.Geo2DSphere(field.Field),
                    IndexType.GeoHaystack => Builders<TEntity>.IndexKeys.GeoHaystack(field.Field),
                    IndexType.Wildcard => Builders<TEntity>.IndexKeys.Wildcard(field.Field),
                    _ => throw null,
                };
            }
        }

        private Expression<Func<TDocument, object>> ConvertToObjectExpression<TDocument, TValue>(Expression<Func<TDocument, TValue>> expression)
        {
            expression.NotNull(nameof(expression));

            var param = expression.Parameters[0];
            var body = expression.Body;
            var convert = Expression.Convert(body, typeof(object));
            return Expression.Lambda<Func<TDocument, object>>(convert, param);
        }
        #endregion
    }
}
