using System;
using System.Linq.Expressions;

namespace EasyMongo
{
    public class MongoIndex<TEntity>
    {
        public IndexType Type { get; set; }
        public Expression<Func<TEntity, object>> Field { get; set; }
    }

    public enum IndexType
    {
        Ascending,
        Descending,
        Text,
        Hashed,
        /////////////////
        Geo2D,
        Geo2DSphere,
        GeoHaystack,
        Wildcard
    }
}
