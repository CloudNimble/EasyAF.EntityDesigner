// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Data.Tools.XmlDesignerBase.Base.Util;

namespace Microsoft.Data.Entity.Design.Model.Visitor
{
    internal abstract class MissedItemCollectingVisitor : Visitor
    {
        protected int _missedCount = -1;

        // use a hash-set here because containment checks on lists are too expensive. 
        protected HashSet<EFElement> _missed = [];

        internal int MissedCount
        {
            get { return _missedCount; }
        }

        internal ICollection<EFElement> Missed
        {
            get { return new ReadOnlyCollection<EFElement>(_missed); }
        }

        internal void ResetMissedCount()
        {
            _missedCount = 0;
        }
    }
}
