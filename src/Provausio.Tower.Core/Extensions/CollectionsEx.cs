using System.Collections.Generic;
using System.Linq;

namespace Provausio.Tower.Core.Extensions
{
    public static class CollectionsEx
    {
        public static List<T> Clone<T>(this IList<T> source)
        {
            var sourceArray = source.ToArray();
            var newArray = new T[sourceArray.Length];
            sourceArray.CopyTo(newArray, 0);

            return new List<T>(newArray);
        }
    }
}
