// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Data.Entity.Design.EntityDesigner.ModelChanges;
using Microsoft.Data.Entity.Design.EntityDesigner.Utils;
using Microsoft.Data.Entity.Design.EntityDesigner.View;
using Microsoft.Data.Entity.Design.EntityDesigner.ViewModel;
using Microsoft.Data.Tools.VSXmlDesignerBase.VisualStudio.Modeling;
using Microsoft.VisualStudio.Modeling;
using Microsoft.VisualStudio.Modeling.Diagrams;

namespace Microsoft.Data.Entity.Design.EntityDesigner.Rules
{
    /// <summary>
    ///     Rule fired when an Inheritance is created
    /// </summary>
    [RuleOn(typeof(Inheritance), FireTime = TimeToFire.TopLevelCommit)]
    internal sealed class Inheritance_DeleteRule : DeleteRule
    {
        public override void ElementDeleted(ElementDeletedEventArgs e)
        {
            base.ElementDeleted(e);

            if (e.ModelElement is Inheritance inheritance)
            {
                if (inheritance.TargetEntityType != null)
                {
                    // We need to invalidate the target entitytypeshape element; so base type name will be updated correctly.
                    foreach (var pe in PresentationViewsSubject.GetPresentation(inheritance.TargetEntityType))
                    {
                        EntityTypeShape entityShape = pe as EntityTypeShape;
                        entityShape?.Invalidate();
                    }
                }

                var tx = ModelUtils.GetCurrentTx(inheritance.Store);
                Debug.Assert(tx != null);
                if (tx != null
                    && !tx.IsSerializing)
                {
                    ViewModelChangeContext.GetNewOrExistingContext(tx).ViewModelChanges.Add(new InheritanceDelete(inheritance));
                }
            }
        }
    }
}
