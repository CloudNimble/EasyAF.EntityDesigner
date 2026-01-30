// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.Data.Entity.Design.Model.Entity;

namespace Microsoft.Data.Entity.Design.Model.Commands
{
    internal class ChangePropertyTypeCommand : Command
    {
        public Property Property { get; set; }
        internal string NewTypeName { get; set; }

        public ChangePropertyTypeCommand(Func<Command, CommandProcessorContext, bool> bindingAction)
            : base(bindingAction)
        {
        }

        internal ChangePropertyTypeCommand(Property property, string newType)
        {
            CommandValidation.ValidateProperty(property);
            ValidateString(newType);

            Property = property;
            NewTypeName = newType;
        }

        protected override void InvokeInternal(CommandProcessorContext cpc)
        {
            Debug.Assert(Property != null, "InvokeInternal is called when Property is null.");

            if (Property == null)
            {
                throw new InvalidOperationException("InvokeInternal is called when Property is null.");
            }

            ConceptualProperty concProp = Property as ConceptualProperty;
            StorageProperty storeProp = Property as StorageProperty;

            if (concProp == null
                && storeProp == null)
            {
                throw new InvalidOperationException("InvokeInternal is called when the Property is neither a ConceptualProperty nor a StorageProperty");
            }

            var typeName = (concProp != null ? concProp.TypeName : storeProp.TypeName);

            if (String.Compare(typeName, NewTypeName, StringComparison.Ordinal) == 0)
            {
                // no change needed
                return;
            }

            // Remove all facets for previous type (except Nullable - we persist the setting of that across types)
            Property.RemoveAllFacetsExceptNullable();

            if (concProp != null)
            {
                ConceptualEntityModel cModel = (ConceptualEntityModel)concProp.GetParentOfType(typeof(ConceptualEntityModel));
                Debug.Assert(cModel != null, "Unable to find ConceptualModel for property:" + concProp.DisplayName);

                if (cModel != null)
                {
                    var enumType = ModelHelper.FindEnumType(cModel, NewTypeName);
                    if (enumType != null)
                    {
                        concProp.ChangePropertyType(enumType);
                    }
                    else
                    {
                        concProp.ChangePropertyType(NewTypeName);
                    }
                }
            }
            else
            {
                storeProp.Type.Value = NewTypeName;
            }
        }
    }
}
