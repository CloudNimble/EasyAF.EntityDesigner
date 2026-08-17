// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Data.Entity.Design.Model.Mapping;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;

namespace Microsoft.Data.Entity.Design.Model.Commands
{
    internal class DeleteUnboundMappingsCommand : Command
    {
        protected override void InvokeInternal(CommandProcessorContext cpc)
        {
            var service = cpc.EditingContext.GetEFArtifactService();
            var artifact = service.Artifact;
            Debug.Assert(null != artifact, "Null Artifact");
            if (null == artifact)
            {
                return;
            }

            var mappingModel = artifact.MappingModel();
            if (null == mappingModel)
            {
                return;
            }

            var ecm = mappingModel.FirstEntityContainerMapping;
            if (null == ecm)
            {
                return;
            }

            // loop over the EntitySetMappings looking for deleted EntityTypes
            // Note: have to convert to array first to prevent exceptions due to
            // editing the collection while iterating over it
            var entitySetMappings = ecm.EntitySetMappings().ToArray();
            foreach (var esm in entitySetMappings)
            {
                RecursiveDeleteUnboundElements(cpc, esm);
            }

            // loop over the AssociationSetMappings looking for deleted Associations
            // Note: have to convert to array first to prevent exceptions due to
            // editing the collection while iterating over it
            var associationSetMappings = ecm.AssociationSetMappings().ToArray();
            foreach (var asm in associationSetMappings)
            {
                RecursiveDeleteUnboundElements(cpc, asm);
            }

            // loop over the FunctionImportMappings looking for deleted Associations
            // Note: have to convert to array first to prevent exceptions due to
            // editing the collection while iterating over it
            var functionImportMappings = ecm.FunctionImportMappings().ToArray();
            foreach (var fim in functionImportMappings)
            {
                RecursiveDeleteUnboundElements(cpc, fim);
            }
        }

        /// <summary>
        ///     If this element has any children that have unresolved references
        ///     or has itself an unresolved reference then Delete the element
        ///     NB: Be warned this method is destructive. If you have unbound
        ///     elements for whatever reason then those elements will be removed.
        /// </summary>
        private static void RecursiveDeleteUnboundElements(CommandProcessorContext cpc, EFElement efElement)
        {
            if (efElement is Condition mappingCondition)
            {
                // A Mapping Condition is special because it will 
                // only ever have 1 bound child out of 2
                var children = efElement.Children.ToArray();
                var anyOneOfChildrenIsBound =
                    children.OfType<ItemBinding>().Any(itemBinding => itemBinding.Resolved);

                if (!anyOneOfChildrenIsBound)
                {
                    DeleteEFElementCommand.DeleteInTransaction(cpc, efElement);
                }

                return;
            }
            else if (efElement is QueryView queryView)
            {
                // QueryView elements have a TypeName attribute, but it is optional and
                // so the QueryView should not be deleted even if the TypeName attribute
                // is not in a Resolved state
                return;
            }
            else
            {
                // cannot use IEnumerable directly as we are potentially
                // deleting from the returned collection
                List<EFObject> children = new List<EFObject>(efElement.Children);

                // remove any children which are optional (and hence not being resolved does not require a delete)
                if (efElement is ComplexProperty cp)
                {
                    // TypeName binding in ComplexProperty is optional and can be unresolved, so remove it from the check list
                    children.Remove(cp.TypeName);
                }
                if (efElement is ModificationFunction mf)
                {
                    children.Remove(mf.RowsAffectedParameter);
                }

                // loop over children and recursively delete
                foreach (var child in children)
                {
                    if (child is EFElement efElementChild)
                    {
                        RecursiveDeleteUnboundElements(cpc, efElementChild);
                    }
                    else if (child is ItemBinding itemBindingChild)
                    {
                        if (!itemBindingChild.Resolved)
                        {
                            DeleteEFElementCommand.DeleteInTransaction(cpc, efElement);
                            return;
                        }
                    }
                }
            }
        }
    }
}
