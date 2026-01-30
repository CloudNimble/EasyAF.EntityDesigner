// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Entity.Design.UI.ViewModels.PropertyWindow.Descriptors;
using XmlDesignerBaseResources = Microsoft.Data.Tools.XmlDesignerBase.Resources;

namespace Microsoft.Data.Entity.Design.UI.ViewModels.PropertyWindow.Converters
{
    internal class BaseTypeListConverter : DynamicListConverter<EntityType, ObjectDescriptor>
    {
        protected override void PopulateMappingForSelectedObject(ObjectDescriptor selectedObject)
        {
            Debug.Assert(selectedObject != null, "selectedObject should not be null");

            if (selectedObject != null)
            {
                ConceptualEntityType currentType = selectedObject.WrappedItem as ConceptualEntityType;
                Debug.Assert(
                    !(selectedObject.WrappedItem is EntityType) || currentType != null,
                    "EntityType is not ConceptualEntityType");
                if (currentType == null)
                {
                    EntityTypeBaseType entityTypeBaseType = selectedObject.WrappedItem as EntityTypeBaseType;
                    Debug.Assert(entityTypeBaseType != null, "Type Converter used on unhandled Object Descriptor.");
                    if (entityTypeBaseType != null)
                    {
                        currentType = entityTypeBaseType.OwnerEntityType;
                    }
                }
                else
                {
                    // Add an entry for (None) with null value
                    AddMapping(null, XmlDesignerBaseResources.NoneDisplayValueUsedForUX);
                }

                if (currentType != null)
                {
                    var parent = currentType.EntityModel;
                    Debug.Assert(parent != null, "Parent for EntityType needs to be BaseEntityModel.");

                    if (parent != null)
                    {
                        // Now get all valid  entity types to be displayed
                        foreach (var type in parent.EntityTypes())
                        {
                            ConceptualEntityType cet = type as ConceptualEntityType;
                            Debug.Assert(cet != null, "EntityType is not ConceptualEntityType");

                            // Add to the list if
                            // 1. the entity type is not as same as the selected entity type.
                            // 2. the entity type is not the derived type form the selected entity type.
                            if (cet != currentType
                                && !cet.IsDerivedFrom(currentType))
                            {
                                AddMapping(type, type.LocalName.Value);
                            }

                            // Now add unresolved EntityType if any
                            if (cet.BaseType.Target == null
                                && !String.IsNullOrEmpty(cet.BaseType.RefName))
                            {
                                AddMapping(currentType, cet.BaseType.RefName);
                            }
                        }
                    }
                }
            }
        }
    }
}