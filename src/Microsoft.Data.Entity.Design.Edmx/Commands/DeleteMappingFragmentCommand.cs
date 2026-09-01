// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Integrity;
using Microsoft.Data.Entity.Design.Edmx.Mapping;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using System.Diagnostics;

namespace Microsoft.Data.Entity.Design.Edmx.Commands
{
    internal class DeleteMappingFragmentCommand : DeleteEFElementCommand
    {
        internal string ConceptualEntityTypeName { get; private set; }
        internal string StorageEntitySetName { get; private set; }

        /// <summary>
        ///     Deletes the passed in MappingFragment
        /// </summary>
        /// <param name="fragment"></param>
        internal DeleteMappingFragmentCommand(MappingFragment fragment)
            : base(fragment)
        {
            CommandValidation.ValidateMappingFragment(fragment);
        }

        protected MappingFragment MappingFragment
        {
            get
            {
                MappingFragment elem = EFElement as MappingFragment;
                Debug.Assert(elem != null, "underlying element does not exist or is not a MappingFragment");
                if (elem == null)
                {
                    throw new InvalidModelItemException();
                }
                return elem;
            }
        }

        protected override void PreInvoke(CommandProcessorContext cpc)
        {
            if (MappingFragment.EntityTypeMapping != null
                && MappingFragment.EntityTypeMapping.EntitySetMapping != null)
            {
                ConceptualEntityTypeName = MappingFragment.EntityTypeMapping.FirstBoundConceptualEntityType?.Name.Value;
                StorageEntitySetName = MappingFragment.StoreEntitySet.Target?.Name.Value;

                EnforceEntitySetMappingRules.AddRule(cpc, MappingFragment.EntityTypeMapping.EntitySetMapping);
            }

            base.PreInvoke(cpc);
        }

        protected override void InvokeInternal(CommandProcessorContext cpc)
        {
            var etm = MappingFragment.EntityTypeMapping;
            if (etm.MappingFragments().Count == 1)
            {
                // if we are about to remove the last fragment from this ETM, just remove it
                Debug.Assert(
                    etm.MappingFragments()[0] == MappingFragment, "end.MappingFragments()[0] should be the same as this.MappingFragment");
                DeleteInTransaction(cpc, etm);
            }
            else
            {
                base.InvokeInternal(cpc);
            }
        }
    }
}
