using System;

namespace EasyMongo.Conventions
{
    public class SingularCamelCaseCollectionNameConvention : CollectionNameConvention
    {
        public override string GetCollectionName(Type type) => type.Name.Singularize(false).Camelize();
    }
}
