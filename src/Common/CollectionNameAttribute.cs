using System;

namespace EasyMongo
{
    [AttributeUsage(AttributeTargets.Class/*, Inherited = true*/)]
    public sealed class CollectionNameAttribute : Attribute
    {
        public string Name { get; set; }
        public CollectionNameAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Empty collectionname not allowed", nameof(name));
            Name = name;
        }
    }
}
