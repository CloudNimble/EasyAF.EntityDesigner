// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Diagrams.ModelChanges;
using Microsoft.Data.Entity.Design.Diagrams.Utils;
using Microsoft.Data.Entity.Design.Diagrams.ViewModel;
using Microsoft.VisualStudio.Modeling;
using System.Diagnostics;

namespace Microsoft.Data.Entity.Design.Diagrams.Rules
{
    /// <summary>
    ///     Rule fired when an NavigationProperty changes
    /// </summary>
    [RuleOn(typeof(NavigationProperty), FireTime = TimeToFire.TopLevelCommit)]
    internal sealed class NavigationProperty_AddRule : AddRule
    {
        /// <summary>
        ///     Do the following when an Entity changes:
        ///     - Update roles in related Associations
        /// </summary>
        public override void ElementAdded(ElementAddedEventArgs e)
        {
            base.ElementAdded(e);

            NavigationProperty addedProperty = e.ModelElement as NavigationProperty;
            Debug.Assert(addedProperty != null, "addedProperty != null");
            Debug.Assert(addedProperty.EntityType != null && addedProperty.EntityType.EntityDesignerViewModel != null, "addedProperty.EntityType != null && addedProperty.EntityType.EntityDesignerViewModel != null");

            if (addedProperty != null
                && addedProperty.EntityType != null
                && addedProperty.EntityType.EntityDesignerViewModel != null)
            {
                var tx = ModelUtils.GetCurrentTx(e.ModelElement.Store);
                Debug.Assert(tx != null, "tx != null");
                if (tx != null
                    && !tx.IsSerializing)
                {
                    ViewModelChangeContext.GetNewOrExistingContext(tx).ViewModelChanges.Add(new NavigationPropertyAdd(addedProperty));
                }
            }
        }
    }
}
