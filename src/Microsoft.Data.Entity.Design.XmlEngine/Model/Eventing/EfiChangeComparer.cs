// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Data.Entity.Design.XmlEngine.Model.Eventing
{

    internal abstract class EfiChangeComparer : IComparer<EfiChangeStableSortItem>
    {
        /// <summary>
        ///     Sort changes so that Deletes are first, creates are second and updates are third.
        ///     For example: Sort updates to process EntityType first, then Associaiton, then others.
        ///     Sort is stable (i.e. it is preserving order of items that have equal values).
        ///     This is acomplished by comparing original position of items for which GetVal() returned same value.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int Compare(EfiChangeStableSortItem x, EfiChangeStableSortItem y)
        {
            var xval = GetVal(x.EfiChange);
            var yval = GetVal(y.EfiChange);
            if (xval > yval)
            {
                return 1;
            }
            else if (xval < yval)
            {
                return -1;
            }
            else
            {
                if (x.Position > y.Position)
                {
                    return 1;
                }
                else if (x.Position < y.Position)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            }
        }

        protected abstract int GetVal(EfiChange change);
    }

}
