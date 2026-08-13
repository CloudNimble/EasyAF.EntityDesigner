// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.UI;

namespace Microsoft.VisualStudio.Data.Entity.Design.UI.Views.EntityDesigner
{
    internal class EntityDesignerSelection : Selection
    {
        public EntityDesignerSelection()
        {
        }

        internal EntityDesignerSelection(IEnumerable<EFObject> selectedObjects)
            : base(selectedObjects)
        {
        }

        internal EntityDesignerSelection(IEnumerable<EFObject> selectedObjects, Predicate<EFObject> match)
            : base(selectedObjects, match)
        {
        }

        internal EntityDesignerSelection(IEnumerable selectedObjects)
            : base(selectedObjects)
        {
        }

        internal EntityDesignerSelection(IEnumerable selectedObjects, Predicate<EFObject> match)
            : base(selectedObjects, match)
        {
        }

        internal EntityDesignerSelection(params EFObject[] selectedObjects)
            : base(selectedObjects)
        {
        }

        internal override Type ItemType
        {
            get { return typeof(EntityDesignerSelection); }
        }
    }
}
