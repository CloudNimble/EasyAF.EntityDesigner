// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Base.Context;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Entity.Design.Model.Mapping;
using Microsoft.VisualStudio.Data.Entity.Design.UI.ViewModels.MappingDetails.Associations;
using Microsoft.VisualStudio.Data.Entity.Design.UI.ViewModels.MappingDetails.FunctionImports;
using Microsoft.VisualStudio.Data.Entity.Design.UI.ViewModels.MappingDetails.Functions;
using Microsoft.VisualStudio.Data.Entity.Design.UI.ViewModels.MappingDetails.Tables;
using Microsoft.VisualStudio.Data.Entity.Design.UI.Views.MappingDetails;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Data.Entity.Design.UI.ViewModels.MappingDetails
{
    internal class ModelToMappingModelXRef : ContextItem
    {
        private static Dictionary<Type, Type> _modelTypeToViewModelType;
        private readonly Dictionary<EFElement, MappingEFElement> _dict = [];

        private static Dictionary<Type, Type> ModelTypeToViewModelType
        {
            get
            {
                _modelTypeToViewModelType ??= new Dictionary<Type, Type>
                    {
                        [typeof(Condition)] = typeof(MappingCondition),
                        [typeof(Association)] = typeof(MappingAssociation),
                        [typeof(AssociationSet)] = typeof(MappingAssociationSet),
                        [typeof(AssociationSetEnd)] = typeof(MappingAssociationSetEnd),
                        [typeof(ModificationFunction)] = typeof(MappingModificationFunctionMapping),
                        [typeof(FunctionScalarProperty)] = typeof(MappingFunctionScalarProperty),
                        [typeof(ResultBinding)] = typeof(MappingResultBinding),
                        [typeof(FunctionImportMapping)] = typeof(MappingFunctionImport),
                        [typeof(FunctionImportScalarProperty)] = typeof(MappingFunctionImportScalarProperty)
                    };

                return _modelTypeToViewModelType;
            }
        }

        internal static ModelToMappingModelXRef GetModelToMappingModelXRef(EditingContext context)
        {
            var xref = context.Items.GetValue<ModelToMappingModelXRef>();
            if (xref == null)
            {
                xref = new ModelToMappingModelXRef();
                context.Items.SetValue(xref);
            }
            return xref;
        }

        internal static MappingEFElement GetNewOrExisting(EditingContext context, EFElement modelElement, MappingEFElement parent)
        {
            MappingEFElement result;

            var xref = GetModelToMappingModelXRef(context);
            result = xref.GetExisting(modelElement);
            if (result != null)
            {
                result.Parent = parent;
            }
            else
            {
                ModelTypeToViewModelType.TryGetValue(modelElement.GetType(), out Type viewModelType);
                if (viewModelType == null)
                {
                    // try the base class type
                    ModelTypeToViewModelType.TryGetValue(modelElement.GetType().BaseType, out viewModelType);
                }

                if (viewModelType != null)
                {
                    result = Activator.CreateInstance(viewModelType, context, modelElement, parent) as MappingEFElement;
                    xref.Add(modelElement, result);
                }
                else
                {
                    // implement a special case for entity type
                    // create the correct C- or S-space entity type in our view model
                    if (modelElement is EntityType entityType)
                    {
                        var mappingDetailsInfo = context.Items.GetValue<MappingDetailsInfo>();
                        if (mappingDetailsInfo.EntityMappingMode == EntityMappingModes.Tables)
                        {
                            BaseEntityModel entityModel = entityType.Parent as BaseEntityModel;
                            Debug.Assert(
                                entityModel != null,
                                "entityType's parent should be an EntityModel but received type "
                                + (entityType.Parent == null ? "NULL" : entityType.Parent.GetType().FullName));

                            if (entityModel.IsCSDL)
                            {
                                result =
                                    Activator.CreateInstance(typeof(MappingConceptualEntityType), context, modelElement, parent) as
                                    MappingEFElement;
                            }
                            else
                            {
                                result =
                                    Activator.CreateInstance(typeof(MappingStorageEntityType), context, modelElement, parent) as
                                    MappingEFElement;
                            }
                        }
                        else
                        {
                            result =
                                Activator.CreateInstance(typeof(MappingFunctionEntityType), context, modelElement, parent) as
                                MappingEFElement;
                        }
                        xref.Add(modelElement, result);
                    }

                    // special case for scalar properties
                    if (modelElement is ScalarProperty scalarProperty)
                    {
                        if (scalarProperty.Parent is MappingFragment
                            || scalarProperty.Parent is ComplexProperty)
                        {
                            result =
                                Activator.CreateInstance(typeof(MappingScalarProperty), context, modelElement, parent) as MappingEFElement;
                        }
                        else
                        {
                            result =
                                Activator.CreateInstance(typeof(MappingEndScalarProperty), context, modelElement, parent) as
                                MappingEFElement;
                        }
                        xref.Add(modelElement, result);
                    }
                }
            }

            return result;
        }

        internal override Type ItemType
        {
            get { return typeof(ModelToMappingModelXRef); }
        }

        internal void Add(EFElement modelElement, MappingEFElement mapElement)
        {
            _dict.Add(modelElement, mapElement);
        }

        internal void Set(EFElement modelElement, MappingEFElement mapElement)
        {
            if (GetExisting(modelElement) == null)
            {
                _dict.Add(modelElement, mapElement);
            }
            else
            {
                _dict[modelElement] = mapElement;
            }
        }

        internal void Remove(EFElement modelElement)
        {
            _dict.Remove(modelElement);
        }

        internal MappingEFElement GetExisting(EFElement modelElement)
        {
            _dict.TryGetValue(modelElement, out MappingEFElement result);
            return result;
        }

        internal MappingEFElement GetExistingOrParent(EFElement modelElement)
        {
            MappingEFElement result = null;
            while (result == null
                   && modelElement != null)
            {
                if (!_dict.TryGetValue(modelElement, out result))
                {
                    modelElement = modelElement.Parent as EFElement;
                }
            }
            return result;
        }

        internal void Clear()
        {
            _dict.Clear();
        }
    }
}
