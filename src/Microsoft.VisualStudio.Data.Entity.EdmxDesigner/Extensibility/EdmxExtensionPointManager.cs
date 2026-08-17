// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx;
using Microsoft.Data.Entity.Design.Edmx.Designer;
using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.Model;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.Package;
using Microsoft.VisualStudio.Data.Entity.Extensibility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Extensibility
{

    /// <summary>
    ///     Central lookup for the designer's extension points.
    /// </summary>
    /// <remarks>
    ///     Every extension point is a MEF export contributed by a third-party VSIX and pulled from Visual Studio's
    ///     shared composition container, so the set of available extensions is only known at run time and can differ
    ///     between installations. Exports are returned as <see cref="Lazy{T}" /> instances, which keeps a misbehaving
    ///     extension from being constructed until the designer actually needs it. Extensions that declare a layer via
    ///     <see cref="IEntityDesignerLayerData" /> are additionally filtered against the layers enabled on the current
    ///     artifact, so an installed-but-disabled layer contributes nothing.
    /// </remarks>
    internal class EdmxExtensionPointManager
    {

        #region Fields

        /// <summary>
        ///     The MEF container the designer pulls all of its extension exports from.
        /// </summary>
        /// <remarks>
        ///     This is Visual Studio's shared default export provider rather than a private container, so extensions
        ///     compose against the same catalog as the rest of the shell.
        /// </remarks>
        private readonly ExportProvider _exportProvider;

        /// <summary>
        ///     Backing store for <see cref="Instance" />.
        /// </summary>
        private static EdmxExtensionPointManager _instance;

        #endregion

        #region Properties

        /// <summary>
        ///     The MEF container the designer pulls all of its extension exports from.
        /// </summary>
        internal ExportProvider ExportProvider
        {
            get { return _exportProvider; }
        }

        /// <summary>
        ///     The layer manager for the artifact currently open in the designer, if any.
        /// </summary>
        /// <remarks>
        ///     Resolved per call rather than cached because the "current" artifact changes as the user switches
        ///     document windows. Returns <see langword="null" /> when no artifact is open or when the open artifact is
        ///     not a Visual Studio artifact, in which case layer-specific extensions are suppressed entirely.
        /// </remarks>
        internal static LayerManager LayerManager
        {
            get
            {
                VSArtifact vsArtifact = PackageManager.Package.DocumentFrameMgr.CurrentArtifact as VSArtifact;
                return vsArtifact?.LayerManager;
            }
        }

        /// <summary>
        ///     The lazily created singleton used by the static extension-loading members.
        /// </summary>
        /// <remarks>
        ///     Construction is deferred rather than done in a static initializer because the constructor reaches into
        ///     the VS package for the MEF component model, which is only available once the package has sited itself.
        /// </remarks>
        private static EdmxExtensionPointManager Instance
        {
            get
            {
                _instance ??= new EdmxExtensionPointManager();
                return _instance;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        ///     Captures Visual Studio's default MEF export provider.
        /// </summary>
        /// <remarks>
        ///     Private because callers go through <see cref="Instance" />; see that member for why the lookup cannot
        ///     happen any earlier than first use.
        /// </remarks>
        private EdmxExtensionPointManager()
        {
            IComponentModel componentModelService = (IComponentModel)PackageManager.Package.GetService(typeof(SComponentModel));
            _exportProvider = componentModelService.DefaultExportProvider;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        ///     Maps a model element onto the extensibility selection kind that describes it.
        /// </summary>
        /// <param name="el">The model element the designer currently has selected.</param>
        /// <returns>
        ///     The matching <see cref="EntityDesignerSelection" />, or <see langword="null" /> when the element is not
        ///     one that extensions can be notified about.
        /// </returns>
        /// <remarks>
        ///     Extensions describe what they apply to in terms of a coarse selection kind, so the concrete model type
        ///     has to be collapsed onto that vocabulary. Most element names are shared between the conceptual and the
        ///     storage model, so the element's owning model root decides which of the two variants applies.
        /// </remarks>
        internal static EntityDesignerSelection? DetermineEntityDesignerSelection(EFElement el)
        {
            var isConceptual = el.RuntimeModelRoot() is ConceptualEntityModel;

            if (el.EFTypeName == BaseEntityModel.ElementName)
            {
                if (el is ConceptualEntityModel)
                {
                    return EntityDesignerSelection.DesignerSurface;
                }
                else
                {
                    return EntityDesignerSelection.StorageModelEntityContainer;
                }
            }
            else if (el.EFTypeName == EntitySet.ElementName && isConceptual)
            {
                return EntityDesignerSelection.ConceptualModelEntitySet;
            }
            else if (el.EFTypeName == AssociationSet.ElementName && isConceptual)
            {
                return EntityDesignerSelection.ConceptualModelAssociationSet;
            }
            else if (el.EFTypeName == BaseEntityContainer.ElementName)
            {
                if (isConceptual)
                {
                    return EntityDesignerSelection.ConceptualModelEntityContainer;
                }
                else
                {
                    return EntityDesignerSelection.StorageModelEntityContainer;
                }
            }
            else if (el.EFTypeName == EntityType.ElementName)
            {
                if (isConceptual)
                {
                    return EntityDesignerSelection.ConceptualModelEntityType;
                }
                else
                {
                    return EntityDesignerSelection.StorageModelEntityType;
                }
            }
            else if (el.EFTypeName == Property.ElementName)
            {
                if (isConceptual)
                {
                    if (el is ComplexConceptualProperty)
                    {
                        return EntityDesignerSelection.ConceptualModelComplexProperty;
                    }
                    else
                    {
                        return EntityDesignerSelection.ConceptualModelProperty;
                    }
                }
                else
                {
                    return EntityDesignerSelection.StorageModelProperty;
                }
            }
            else if (el.EFTypeName == NavigationProperty.ElementName)
            {
                return EntityDesignerSelection.ConceptualModelNavigationProperty;
            }
            else if (el.EFTypeName == Association.ElementName)
            {
                if (isConceptual)
                {
                    return EntityDesignerSelection.ConceptualModelAssociation;
                }
                else
                {
                    return EntityDesignerSelection.StorageModelAssociation;
                }
            }
            else if (el.EFTypeName == ComplexType.ElementName && isConceptual)
            {
                return EntityDesignerSelection.ConceptualModelComplexType;
            }
            else if (el.EFTypeName == FunctionImport.ElementName)
            {
                return EntityDesignerSelection.ConceptualModelFunctionImport;
            }
            else if (el.EFTypeName == Parameter.ElementName)
            {
                if (isConceptual)
                {
                    return EntityDesignerSelection.ConceptualModelFunctionImportParameter;
                }
                else
                {
                    return EntityDesignerSelection.StorageModelFunctionParameter;
                }
            }
            else if (el.EFTypeName == Function.ElementName)
            {
                return EntityDesignerSelection.StorageModelFunction;
            }
            else if (el.EFTypeName == EntityTypeShape.ElementName)
            {
                return EntityDesignerSelection.ConceptualModelEntityType;
            }

            return null;
        }

        /// <summary>
        ///     Loads the command factories that contribute commands to the designer's context menus.
        /// </summary>
        /// <param name="excludeLayers">Drop factories that belong to a layer.</param>
        /// <param name="excludeNonLayers">Drop factories that do not belong to any layer.</param>
        /// <returns>The command factory exports that survive layer filtering.</returns>
        /// <remarks>
        ///     The two exclusion flags let the caller build the layer-owned and the non-layer portions of a menu
        ///     separately, which is how layer commands are kept grouped apart from the designer's own commands.
        /// </remarks>
        internal static Lazy<IEntityDesignerCommandFactory>[] LoadCommandExtensions(bool excludeLayers, bool excludeNonLayers)
        {
            return LoadLayerFilteredExtensions<IEntityDesignerCommandFactory>(excludeLayers, excludeNonLayers).ToArray();
        }

        /// <summary>
        ///     Loads every installed designer layer.
        /// </summary>
        /// <returns>All layer exports found in the MEF container.</returns>
        /// <remarks>
        ///     Deliberately unfiltered: this is the list the designer offers the user for enabling and disabling, so it
        ///     has to include layers that are currently switched off.
        /// </remarks>
        internal static IEnumerable<Lazy<IEntityDesignerLayer>> LoadLayerExtensions()
        {
            return Instance.ExportProvider.GetExports<IEntityDesignerLayer>();
        }

        /// <summary>
        ///     Loads the extensions that convert foreign file formats to and from the designer's model.
        /// </summary>
        /// <returns>The conversion exports that survive layer filtering, paired with their conversion metadata.</returns>
        /// <remarks>
        ///     The metadata carries the file extension each extension claims, which is how the correct converter is
        ///     chosen when a document is opened or saved.
        /// </remarks>
        internal static Lazy<IModelConversionExtension, IEntityDesignerConversionData>[] LoadModelConversionExtensions()
        {
            return LoadLayerFilteredExtensions<IModelConversionExtension, IEntityDesignerConversionData>().ToArray();
        }

        /// <summary>
        ///     Loads the extensions that observe or amend the model produced by the designer's generation wizards.
        /// </summary>
        /// <returns>The generation exports that survive layer filtering.</returns>
        internal static Lazy<IModelGenerationExtension>[] LoadModelGenerationExtensions()
        {
            return LoadLayerFilteredExtensions<IModelGenerationExtension>().ToArray();
        }

        /// <summary>
        ///     Loads the extensions that transform the model as it is loaded from and saved to disk.
        /// </summary>
        /// <returns>The transform exports that survive layer filtering.</returns>
        internal static Lazy<IModelTransformExtension>[] LoadModelTransformExtensions()
        {
            return LoadLayerFilteredExtensions<IModelTransformExtension>().ToArray();
        }

        /// <summary>
        ///     Loads the extensions that add properties to the designer's Properties window.
        /// </summary>
        /// <returns>The extended property exports that survive layer filtering, paired with their property metadata.</returns>
        /// <remarks>
        ///     The metadata identifies which selection kind each property applies to, so it must be available without
        ///     constructing the extension itself.
        /// </remarks>
        internal static Lazy<IEntityDesignerExtendedProperty, IEntityDesignerPropertyData>[] LoadPropertyDescriptorExtensions()
        {
            return LoadLayerFilteredExtensions<IEntityDesignerExtendedProperty, IEntityDesignerPropertyData>().ToArray();
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Pulls every export of the given contract out of the MEF container and applies layer filtering.
        /// </summary>
        /// <typeparam name="T">The extension contract to import.</typeparam>
        /// <param name="excludeLayers">Drop extensions that belong to a layer.</param>
        /// <param name="excludeNonLayers">Drop extensions that do not belong to any layer.</param>
        /// <returns>The exports that survive layer filtering.</returns>
        /// <remarks>
        ///     Exports are requested with <see cref="IEntityDesignerLayerData" /> metadata so an extension's layer can
        ///     be inspected without instantiating it. When no artifact is open there is no layer manager and therefore
        ///     no way to know which layers are enabled, so every layer-specific extension is dropped rather than
        ///     optimistically included.
        /// </remarks>
        private static IEnumerable<Lazy<T>> LoadLayerFilteredExtensions<T>(bool excludeLayers = false, bool excludeNonLayers = false)
        {
            // if a layer manager exists, then use it to filter the extension based on what layers are enabled
            // or not. Otherwise, remove all layer-specific extensions from the list we're going to return.
            List<Lazy<T, IEntityDesignerLayerData>> extensions = new List<Lazy<T, IEntityDesignerLayerData>>();
            extensions.AddRange(Instance.ExportProvider.GetExports<T, IEntityDesignerLayerData>());
            var layerManager = LayerManager;
            if (layerManager is not null)
            {
                return layerManager.Filter(extensions, excludeLayers, excludeNonLayers);
            }

            return extensions.Where(l => String.IsNullOrEmpty(l.Metadata.LayerName));
        }

        /// <summary>
        ///     Pulls every export of the given contract out of the MEF container, preserving the caller's metadata view,
        ///     and applies layer filtering.
        /// </summary>
        /// <typeparam name="T">The extension contract to import.</typeparam>
        /// <typeparam name="M">The metadata view the caller needs alongside each export.</typeparam>
        /// <returns>The exports that survive layer filtering, paired with their metadata.</returns>
        /// <remarks>
        ///     Used where the caller needs richer metadata than the layer name alone. Because that metadata view is not
        ///     guaranteed to carry layer information, an export whose metadata does not implement
        ///     <see cref="IEntityDesignerLayerData" /> is treated as belonging to no layer and is kept.
        /// </remarks>
        private static IEnumerable<Lazy<T, M>> LoadLayerFilteredExtensions<T, M>()
        {
            // if a layer manager exists, then use it to filter the extension based on what layers are enabled
            // or not. Otherwise, remove all layer-specific extensions from the list we're going to return.
            var extensions = Instance.ExportProvider.GetExports<T, M>();
            var layerManager = LayerManager;
            if (layerManager is not null)
            {
                return layerManager.Filter(extensions);
            }

            return extensions.Where(
                l =>
                    {
                        return l.Metadata is not IEntityDesignerLayerData layerData || String.IsNullOrEmpty(layerData.LayerName);
                    });
        }

        #endregion

    }

}
