using System;

namespace EasyMongo
{
    public class IndexAttribute : Attribute
    {
        public bool Unique { get; set; }
        public bool Sparse { get; set; }
        public string Name { get; set; }
        public IndexType IndexType { get; set; }
        //CreateIndexOptions
    }

    public interface IEntityTypeConfiguration<TEntity, TKey>
        where TEntity : IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        void Configure(IIndexMongoRepository<TEntity, TKey> indexMongoRepository);
    }
}
