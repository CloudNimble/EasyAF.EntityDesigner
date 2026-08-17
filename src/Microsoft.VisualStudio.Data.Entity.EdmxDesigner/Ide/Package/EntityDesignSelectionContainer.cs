// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.VisualStudio.Package;
using Microsoft.Data.Entity.Design.XmlEngine.Context;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.Data.Entity.Design.XmlEngine.UI;
using Microsoft.Data.Entity.Design.XmlEngine.UI.ViewModels.PropertyWindow.Descriptors;
using Microsoft.VisualStudio.Data.Entity.Design.UI.ViewModels.PropertyWindow;
using System;

namespace Microsoft.VisualStudio.Data.Entity.Design.Ide.Package
{
    // <summary>
    //     This is a simple selection container object that
    //     wraps the designers selection system.
    // </summary>
    internal class EntityDesignSelectionContainer<T> : SelectionContainer<T>
        where T : Selection
    {
        internal EntityDesignSelectionContainer(IServiceProvider shellServices, EditingContext editingContext)
            : base(shellServices, editingContext, PackageManager.Package)
        {
        }

        protected override ObjectDescriptor GetObjectDescriptor(EFElement obj, EditingContext editingContext)
        {
            return PropertyWindowViewModel.GetObjectDescriptor(obj, editingContext, true);
        }
    }
}
