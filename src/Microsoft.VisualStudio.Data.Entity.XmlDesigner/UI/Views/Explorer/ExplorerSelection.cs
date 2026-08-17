// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.Views.Explorer
{
    internal class ExplorerSelection : Selection
    {
        public ExplorerSelection()
        {
        }

        internal ExplorerSelection(IEnumerable<EFObject> selectedObjects)
            : base(selectedObjects)
        {
        }

        internal ExplorerSelection(IEnumerable<EFObject> selectedObjects, Predicate<EFObject> match)
            : base(selectedObjects, match)
        {
        }

        internal ExplorerSelection(IEnumerable selectedObjects)
            : base(selectedObjects)
        {
        }

        internal ExplorerSelection(IEnumerable selectedObjects, Predicate<EFObject> match)
            : base(selectedObjects, match)
        {
        }

        internal ExplorerSelection(params EFObject[] selectedObjects)
            : base(selectedObjects)
        {
        }

        internal override Type ItemType
        {
            get { return typeof(ExplorerSelection); }
        }
    }
}
