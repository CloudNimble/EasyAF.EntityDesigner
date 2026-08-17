// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.Edmx.Integrity;
using Microsoft.Data.Entity.Design.Edmx.Mapping;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Eventing;

namespace Microsoft.Data.Entity.Design.Edmx.Commands
{
    internal class DeleteScalarPropertyCommand : DeleteEFElementCommand
    {
        internal string MFConceptualEntityTypeName { get; private set; }
        internal string MFConceptualEntityTypeOwnerName { get; private set; }
        internal string MFConceptualPropertyName { get; private set; }
        internal string MFStorageEntitySetName { get; private set; }
        internal string MFStorageColumnName { get; private set; }
        internal IEnumerable<string> MFComplexParentList { get; private set; }

        internal DeleteScalarPropertyCommand(Func<Command, CommandProcessorContext, bool> bindingAction)
            : base(bindingAction)
        {
        }

        /// <summary>
        ///     Deletes the passed in ScalarProperty
        /// </summary>
        /// <param name="sp"></param>
        internal DeleteScalarPropertyCommand(ScalarProperty sp)
            : base(sp)
        {
            CommandValidation.ValidateScalarProperty(sp);
        }

        protected internal ScalarProperty ScalarProperty
        {
            get
            {
                ScalarProperty elem = EFElement as ScalarProperty;
                Debug.Assert(elem != null, "underlying element does not exist or is not a ScalarProperty");
                if (elem == null)
                {
                    throw new InvalidModelItemException();
                }
                return elem;
            }
            set { EFElement = value; }
        }

        protected override void PreInvoke(CommandProcessorContext cpc)
        {
            if (ScalarProperty.MappingFragment != null)
            {
                EnforceEntitySetMappingRules.AddRule(cpc, ScalarProperty);

                MFConceptualEntityTypeName = ScalarProperty.FirstBoundConceptualEntityType?.Name.Value;
                MFConceptualPropertyName = ScalarProperty.Name.Target?.Name.Value;
                MFStorageEntitySetName = ScalarProperty.BoundStorageEntityType?.Name.Value;
                MFStorageColumnName = ScalarProperty.ColumnName.Target?.Name.Value;
                MFConceptualEntityTypeOwnerName = ScalarProperty.FirstBoundConceptualEntityType?.Name.Value;
                MFComplexParentList =
                    ScalarProperty.GetParentComplexProperties(true)
                        .Where(cp => cp.Name.Target != null)
                        .Select(cp => cp.Name.Target.Name.Value);
            }
            else if (ScalarProperty.EndProperty != null)
            {
                AssociationSetMapping asm = ScalarProperty.EndProperty.Parent as AssociationSetMapping;
                Debug.Assert(asm != null, "this.ScalarProperty.EndProperty.Parent should be an AssociationSetMapping");
                if (asm != null)
                {
                    EnforceAssociationSetMappingRules.AddRule(cpc, asm);

                    var assoc = asm.TypeName.Target;
                    Debug.Assert(assoc != null, "the association set mapping does not have an Association");
                    if (assoc != null)
                    {
                        InferReferentialConstraints.AddRule(cpc, assoc);
                    }
                }
            }

            // Also add the integrity check to propagate the StoreGeneratedPattern value to the
            // S-side (may be altered by property mapping being deleted) unless we are part
            // of an Update Model txn in which case there is no need as the whole artifact has
            // this integrity check applied by UpdateModelFromDatabaseCommand
            if (EfiTransactionOriginator.UpdateModelFromDatabaseId != cpc.OriginatorId
                && ScalarProperty.Name != null
                && ScalarProperty.Name.Target != null)
            {
                ConceptualProperty cProp = ScalarProperty.Name.Target as ConceptualProperty;
                Debug.Assert(
                    cProp != null, "ScalarProperty should have Name target with type " + typeof(ConceptualProperty).Name +
                                   ", instead got type " + ScalarProperty.Name.Target.GetType().FullName);
                if (cProp != null)
                {
                    PropagateStoreGeneratedPatternToStorageModel.AddRule(cpc, cProp, true);
                }
            }

            base.PreInvoke(cpc);
        }

        protected override void InvokeInternal(CommandProcessorContext cpc)
        {
            // don't remove the parent mapping fragment because the user can explicitly remove it and
            // they might want to keep it around
            var end = ScalarProperty.EndProperty;
            var complexProperty = ScalarProperty.ComplexProperty;
            if (end != null
                && end.ScalarProperties().Count == 1)
            {
                // if we are about to remove the last item from this end, just remove it
                Debug.Assert(
                    end.ScalarProperties()[0] == ScalarProperty, "end.ScalarProperties()[0] should be the same as this.ScalarProperty");
                DeleteInTransaction(cpc, end);
            }
            else if (complexProperty != null
                     && complexProperty.ScalarProperties().Count == 1
                     && complexProperty.ComplexProperties().Count == 0)
            {
                // if we are about to remove the last item from this ComplexProperty, just remove it
                Debug.Assert(
                    complexProperty.ScalarProperties()[0] == ScalarProperty,
                    "complexProperty.ScalarProperties()[0] should be the same as this.ScalarProperty");
                DeleteInTransaction(cpc, complexProperty);
            }
            else
            {
                base.InvokeInternal(cpc);
            }
        }
    }
}
