using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace EasyMongo
{
    public abstract class BaseEntity : BaseEntity<ObjectId>, IEntity
    {
    }

    //[DataContract]
    //[Serializable]
    //[BsonIgnoreExtraElements(Inherited = true)]
    //[BsonKnownTypes]
    //[BsonDiscriminator]
    public abstract class BaseEntity<TKey> : IEntity<TKey>
         where TKey : IEquatable<TKey>
    {
        public BaseEntity()
        {
            Id = IdGenerator.GenerateNewId<TKey>();
        }

        [BsonId]
        //[BsonElement("_id")]
        //[Key]
        //[DataMember]
        //[BsonRepresentation(BsonType.ObjectId)]
        public TKey Id { get; set; }

        //[BsonIgnore]
        //[BsonRequired]
        //[BsonIgnoreIfDefault]
        //[BsonIgnoreIfNull]
        //[BsonDefaultValue("")]
        //[BsonExtraElements]
        //public IDictionary<string, object> ExtraElements { get; set; }

        //[CreatedDate]
        //public DateTime CreatedDate { get; set; }
        //[UpdatedDate]
        //public DateTime UpdatedDate { get; set; }
        //[DeletedDate]
        //public DateTime DeletedDate { get; set; }
        //----------
        //[DateMutator]
        //public DateTimeOffset CreatedOn { get; set; } //CreatedAt - CreateDate, InsertDate
        //[DateMutator]
        //public DateTimeOffset UpdatedOn { get; set; } //UpdatedAt - UpdateDate
        //[DateMutator]
        //public DateTimeOffset DeletedOn { get; set; } //DeletedAt - DeletedDate
        //----------
        //public ObjectId CreatedBy { get; set; } //InsertUserId
        //public ObjectId UpdatedBy { get; set; } //UpdateUserId
        //public ObjectId DeletedBy { get; set; } //DeleteUserId
    }

    //[AttributeUsage(AttributeTargets.Property)]
    //public class DateMutatorAttribute : Attribute
    //{
    //    public override void OnInsert(object target, IEntityProperty property)
    //    {
    //        //Do your mutation here! The "target" is the entity in question.
    //    }
    //    public override void OnUpdate(object target, IEntityProperty property)
    //    {
    //        base.OnUpdate(target, property);
    //    }
    //}
}
