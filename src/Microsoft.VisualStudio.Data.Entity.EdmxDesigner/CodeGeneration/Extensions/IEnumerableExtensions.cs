// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration.Extensions
{
    internal static class IEnumerableExtensions
    {
        public static bool MoreThan<TSource>(this IEnumerable<TSource> source, int count)
        {
            Debug.Assert(source != null, "source is null.");

            if (source is ICollection<TSource> genericCollection)
            {
                return genericCollection.Count > count;
            }

            if (source is ICollection collection)
            {
                return collection.Count > count;
            }

            using (var enumerator = source.GetEnumerator())
            {
                var elementCount = 0;
                while (enumerator.MoveNext())
                {
                    elementCount++;

                    if (elementCount > count)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
