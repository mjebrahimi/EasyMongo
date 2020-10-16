using System;

namespace EasyMongo.Conventions
{
    public abstract class CollectionNameConvention
    {
        private static CollectionNameConvention defaultConvention = PluralCamelCase;
        public static CollectionNameConvention PluralCamelCase => new PluralCamelCaseCollectionNameConvention();
        public static CollectionNameConvention PluralLowerCase => new PluralLowerCaseCollectionNameConvention();
        public static CollectionNameConvention SingularLowerCase => new SingularLowerCaseCollectionNameConvention();
        public static CollectionNameConvention SingularCamelCase => new SingularCamelCaseCollectionNameConvention();
        public static void SetDefaultConvention(CollectionNameConvention convention) => defaultConvention = convention;
        public static CollectionNameConvention GetDefaultConvention() => defaultConvention;

        public abstract string GetCollectionName(Type type);
    }
}
