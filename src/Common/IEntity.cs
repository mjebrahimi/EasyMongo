using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace EasyMongo
{
    public interface IEntity : IEntity<ObjectId>
    {
    }

    public interface IEntity<TKey>
         where TKey : IEquatable<TKey>
    {
        [BsonId]
        //[Key]
        //[DataMember]
        //[BsonRepresentation(BsonType.ObjectId)]
        TKey Id { get; set; }
    }
}
