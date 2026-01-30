// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Designer;
using Microsoft.Data.Entity.Design.Model.Entity;

namespace Microsoft.Data.Entity.Design.UI.ViewModels.Explorer
{
    internal class EntityDesignModelToExplorerModelXRef : ModelToExplorerModelXRef
    {
        private static readonly Dictionary<Type, Type> _modelType2ExplorerViewModelType;

        static EntityDesignModelToExplorerModelXRef()
        {
            _modelType2ExplorerViewModelType = new Dictionary<Type, Type>
            {
                { typeof(AssociationSet), typeof(ExplorerAssociationSet) },
                { typeof(ConceptualEntityContainer), typeof(ExplorerConceptualEntityContainer) },
                { typeof(ConceptualEntityModel), typeof(ExplorerConceptualEntityModel) },
                { typeof(ConceptualEntitySet), typeof(ExplorerEntitySet) },
                { typeof(ConceptualEntityType), typeof(ExplorerConceptualEntityType) },
                { typeof(ComplexType), typeof(ExplorerComplexType) },
                { typeof(ConceptualProperty), typeof(ExplorerConceptualProperty) },
                { typeof(ComplexConceptualProperty), typeof(ExplorerConceptualProperty) },
                { typeof(EntityTypeShape), typeof(ExplorerEntityTypeShape) },
                { typeof(EnumType), typeof(ExplorerEnumType) },
                { typeof(Diagram), typeof(ExplorerDiagram) },
                { typeof(Diagrams), typeof(ExplorerDiagrams) },
                { typeof(Function), typeof(ExplorerFunction) },
                { typeof(FunctionImport), typeof(ExplorerFunctionImport) },
                { typeof(NavigationProperty), typeof(ExplorerNavigationProperty) },
                { typeof(Parameter), typeof(ExplorerParameter) },
                { typeof(StorageEntityModel), typeof(ExplorerStorageEntityModel) },
                { typeof(StorageEntityType), typeof(ExplorerStorageEntityType) },
                { typeof(StorageProperty), typeof(ExplorerStorageProperty) }
            };
        }

        // <summary>
        //     Helper method that determine whether an EFElement is represented in model browser.
        // </summary>
        internal static bool IsDisplayedInExplorer(EFElement efElement)
        {
            // If efElement type is StorageEntityContainer or EFDesignerInfoRoot, don't display it in Model Browser.
            // Note: GetParentOfType() will also return true if self is of passed-in type.
            if (null != efElement.GetParentOfType(typeof(StorageEntityContainer)))
            {
                return false;
            }
                // We do not display Enum type members
            else if (efElement is EnumTypeMember)
            {
                return false;
            }
                // For any Designer objects, check whether the map between the EFElement and ExplorerEFElement exists.
            else if (null != efElement.GetParentOfType(typeof(EFDesignerInfoRoot)))
            {
                return _modelType2ExplorerViewModelType.ContainsKey(efElement.GetType());
            }

            return true;
        }

        // TODO - make this private, and remove the need to pass in the type to GetNew()/GetNewOrExisting().
        protected override Type GetViewModelTypeForEFlement(EFElement efElement)
        {
            if (!IsDisplayedInExplorer(efElement))
            {
                return null;
            }

            var efElementType = efElement.GetType();

            _modelType2ExplorerViewModelType.TryGetValue(efElementType, out Type explorerType);

            // Get correct view-model type for a c-side or s-side entity type.  
            if (explorerType == null)
            {
                if (efElement is Association assoc)
                {
                    if (assoc.EntityModel.IsCSDL)
                    {
                        explorerType = typeof(ExplorerConceptualAssociation);
                    }
                    else
                    {
                        explorerType = typeof(ExplorerStorageAssociation);
                    }
                }
            }

            Debug.Assert(explorerType != null, "Unable to find explorer type for efobject type " + efElementType);

            return explorerType;
        }

        protected override bool IsDisplayedInExplorerProtected(EFElement efElement)
        {
            return IsDisplayedInExplorer(efElement);
        }

        internal override Type ItemType
        {
            get { return typeof(EntityDesignModelToExplorerModelXRef); }
        }
    }
}
