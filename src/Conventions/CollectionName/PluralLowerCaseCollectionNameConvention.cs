using System;

namespace EasyMongo.Conventions
{
    public class PluralLowerCaseCollectionNameConvention : CollectionNameConvention
    {
        public override string GetCollectionName(Type type) => type.Name.Pluralize().ToLowerInvariant();
    }
}
