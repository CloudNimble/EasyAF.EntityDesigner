// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Data.Entity.Design.Base.Context;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Designer;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Entity.Design.UI.ViewModels.PropertyWindow.Descriptors;

namespace Microsoft.Data.Entity.Design.UI.ViewModels.PropertyWindow
{
    // <summary>
    //     provides the information required for displaying
    //     and editing properties of EFElement items
    // </summary>
    internal static class PropertyWindowViewModel
    {
        private static Dictionary<Type, Type> _objectDescriptorTypes;

        public static Dictionary<Type, Type> ObjectDescriptorTypes
        {
            get
            {
                // build a dictionary associating all relevant EFElement derived types 
                // to the corresponding derived type of ItemDescriptor<TEFElement>
                _objectDescriptorTypes ??= new Dictionary<Type, Type>
                {
                    [typeof(Association)] = typeof(EFAssociationDescriptor),
                    [typeof(AssociationSet)] = typeof(EFAssociationSetDescriptor),
                    [typeof(ConceptualEntityContainer)] = typeof(EFEntityContainerDescriptor),
                    [typeof(ConceptualEntityModel)] = typeof(EFEntityModelDescriptor),
                    [typeof(ConceptualEntitySet)] = typeof(EFEntitySetDescriptor),
                    [typeof(ConceptualEntityType)] = typeof(EFEntityTypeDescriptor),
                    [typeof(ComplexType)] = typeof(EFComplexTypeDescriptor),
                    [typeof(EnumType)] = typeof(EFEnumTypeDescriptor),
                    [typeof(Function)] = typeof(EFSFunctionDescriptor),
                    [typeof(FunctionImport)] = typeof(EFFunctionImportDescriptor),
                    [typeof(NavigationProperty)] = typeof(EFNavigationPropertyDescriptor),
                    [typeof(StorageEntityContainer)] = typeof(EFSEntityContainerDescriptor),
                    [typeof(StorageEntityModel)] = typeof(EFSEntityModelDescriptor),
                    [typeof(StorageEntityType)] = typeof(EFEntityTypeDescriptor),
                    [typeof(EntityTypeBaseType)] = typeof(EFEntityTypeBaseTypeDescriptor),
                    [typeof(Diagram)] = typeof(EFDiagramDescriptor),
                    [typeof(EntityTypeShape)] = typeof(EFEntityTypeShapeDescriptor)
                };

                return _objectDescriptorTypes;
            }
        }

        // <summary>
        //     Returns a wrapper for the specified EFObject. The wrapper is the type descriptor
        //     that describes the properties that should be displayed for the EFObject.
        //     The returned wrapper should be handed to a property window control
        // </summary>
        public static ObjectDescriptor GetObjectDescriptor(EFObject obj, EditingContext editingContext, bool runningInVS)
        {
            if (obj != null)
            {

                if (ObjectDescriptorTypes.TryGetValue(obj.GetType(), out Type objectDescriptorType))
                {
                    // create the descriptor wrapper for the EFObject object
                    ObjectDescriptor descriptor = (ObjectDescriptor)TypeDescriptor.CreateInstance(null, objectDescriptorType, null, null);
                    descriptor.Initialize(obj, editingContext, runningInVS);
                    return descriptor;
                }
                else
                {
                    // special case for Property
                    if (obj is Property property)
                    {
                        ObjectDescriptor descriptor = null;
                        if (property is ComplexConceptualProperty)
                        {
                            descriptor =
                                (ObjectDescriptor)TypeDescriptor.CreateInstance(null, typeof(EFComplexPropertyDescriptor), null, null);
                        }
                        else if (property.EntityModel.IsCSDL)
                        {
                            descriptor = (ObjectDescriptor)TypeDescriptor.CreateInstance(null, typeof(EFPropertyDescriptor), null, null);
                        }
                        else
                        {
                            descriptor = (ObjectDescriptor)TypeDescriptor.CreateInstance(null, typeof(EFSPropertyDescriptor), null, null);
                        }
                        descriptor.Initialize(obj, editingContext, runningInVS);
                        return descriptor;
                    }

                    // special case for Parameter
                    if (obj is Parameter parameter)
                    {
                        ObjectDescriptor descriptor = null;
                        if (parameter.Parent is FunctionImport)
                        {
                            descriptor = (ObjectDescriptor)TypeDescriptor.CreateInstance(null, typeof(EFParameterDescriptor), null, null);
                            descriptor.Initialize(obj, editingContext, runningInVS);
                            return descriptor;
                        }
                        else if (parameter.Parent is Function)
                        {
                            //Stored procedure parameter descriptor (EFSParameterDescriptor)
                            descriptor = (ObjectDescriptor)TypeDescriptor.CreateInstance(null, typeof(EFSParameterDescriptor), null, null);
                            descriptor.Initialize(obj, editingContext, runningInVS);
                            return descriptor;
                        }
                    }
                }
            }

            return null;
        }
    }
}
