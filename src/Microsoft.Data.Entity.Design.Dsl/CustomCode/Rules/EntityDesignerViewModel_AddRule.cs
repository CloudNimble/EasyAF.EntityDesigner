// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.Data.Entity.Design.EntityDesigner.Utils;
using Microsoft.Data.Entity.Design.EntityDesigner.ViewModel;
using Microsoft.VisualStudio.Modeling;

namespace Microsoft.Data.Entity.Design.EntityDesigner.Rules
{
    /// <summary>
    ///     Rule fired when a ConceptualModel is created
    /// </summary>
    [RuleOn(typeof(EntityDesignerViewModel), FireTime = TimeToFire.TopLevelCommit)]
    internal sealed class EntityDesignerViewModel_AddRule : AddRule
    {
        /// <summary>
        ///     Do the following when a new ConceptualModel is created:
        ///     - Initialize Namespace and Alias
        /// </summary>
        /// <param name="e"></param>
        public override void ElementAdded(ElementAddedEventArgs e)
        {
            base.ElementAdded(e);

            EntityDesignerViewModel model = e.ModelElement as EntityDesignerViewModel;
            Debug.Assert(model != null);

            if (model != null)
            {
                if (ModelUtils.IsSerializing(e.ModelElement.Store) == false)
                {
                    if (String.IsNullOrEmpty(model.Namespace))
                    {
                        model.Namespace = "Model";
                    }
                }
            }
        }
    }
}
