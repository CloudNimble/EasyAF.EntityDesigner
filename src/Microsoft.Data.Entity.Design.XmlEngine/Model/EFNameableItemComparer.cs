// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Data.Entity.Design.XmlEngine.Model
{

    internal class EFNameableItemComparer : IComparer<EFNameableItem>
    {
        //used to sort nameable items
        public int Compare(EFNameableItem left, EFNameableItem right)
        {
            var leftName = left.LocalName.Value;
            var rightName = right.LocalName.Value;
            // sorted in case insensitive alphabetical order.
            return String.Compare(leftName, rightName, StringComparison.CurrentCultureIgnoreCase);
        }
    }

}
