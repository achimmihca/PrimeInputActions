using System.Collections.Generic;

namespace PrimeInputActions
{
    public static class ExtensionMethods
    {
        // Implements string.IsNullOrEmpty(...) as Extension Method.
        // This way it can be called as myString.IsNullOrEmpty(); instead of string.IsNullOrEmpty(myString);
        public static bool IsNullOrEmpty(this string txt)
        {
            return string.IsNullOrEmpty(txt);
        }
        
        // Returns true if and only if the given collection is null or does not contain any values.
        public static bool IsNullOrEmpty<T>(this IReadOnlyCollection<T> collection)
        {
            return (collection == null) || (collection.Count == 0);
        }
    }
}
