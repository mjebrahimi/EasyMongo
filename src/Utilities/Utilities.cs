using System;

namespace EasyMongo
{
    internal static class Utilities
    {
        internal static T NotNull<T>(this T obj, string name, string message = null)
        {
            if (obj is null)
                throw new ArgumentNullException($"{name} : {typeof(T)}", message);
            return obj;
        }
    }
}
