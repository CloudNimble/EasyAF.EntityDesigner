// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Data.Entity.Design.Extensibility
{

    /// <summary>
    /// An enumeration used to specify which object types that, when selected in the Entity Data Model Designer or the Model
    /// Browser, cause the
    /// <see cref="IEntityDesignerExtendedProperty.CreateProperty(System.Xml.Linq.XElement, PropertyExtensionContext)" /> method
    /// of the annotated class to be called.
    /// </summary>
    /// <example>
    /// <code>
    /// [Export(typeof(IEntityDesignerExtendedProperty))]
    /// [EntityDesignerExtendedProperty(EntityDesignerSelection.ConceptualModelEntityType | EntityDesignerSelection.ConceptualModelProperty)]
    /// public class MyProperties : IEntityDesignerExtendedProperty
    /// {
    ///     // ...
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// Passed to <see cref="EntityDesignerExtendedPropertyAttribute" />. The values are flags, so a single property extension can
    /// opt into several kinds of selection at once. The designer classifies whatever the user selected into exactly one of these
    /// values and then invokes the property extensions whose flags include it.
    /// <para>
    /// The <c>ConceptualModel*</c> values describe objects in the conceptual (C-space) model and the <c>StorageModel*</c> values
    /// describe objects in the storage (S-space) model; some object kinds appear in both and are distinguished only by which
    /// model they belong to.
    /// </para>
    /// </remarks>
    [Flags]
    public enum EntityDesignerSelection
    {

        /// <summary>
        /// Specifies that CreateProperty should be called when the Entity Data Model Designer surface is selected in the Entity
        /// Data Model Designer.
        /// </summary>
        DesignerSurface = 0x00001,

        /// <summary>
        /// Specifies that CreateProperty should be called when a conceptual model entity set is selected in the Model Browser.
        /// </summary>
        ConceptualModelEntitySet = 0x00002,

        /// <summary>
        /// Specifies that CreateProperty should be called when a conceptual model association set is selected in the Model
        /// Browser.
        /// </summary>
        ConceptualModelAssociationSet = 0x00004,

        /// <summary>
        /// Specifies that CreateProperty should be called when a conceptual model entity container is selected in the Model
        /// Browser.
        /// </summary>
        ConceptualModelEntityContainer = 0x00008,

        /// <summary>
        /// Specifies that CreateProperty should be called when a conceptual model entity type is selected in the Entity Data
        /// Model Designer or the Model Browser.
        /// </summary>
        ConceptualModelEntityType = 0x00010,

        /// <summary>
        /// Specifies that CreateProperty should be called when a conceptual model property is selected in the Entity Data Model
        /// Designer or the Model Browser.
        /// </summary>
        ConceptualModelProperty = 0x00020,

        /// <summary>
        /// Specifies that CreateProperty should be called when a conceptual model navigation property is selected in the Entity
        /// Data Model Designer or the Model Browser.
        /// </summary>
        ConceptualModelNavigationProperty = 0x00040,

        /// <summary>
        /// Specifies that CreateProperty should be called when a conceptual model association is selected in the Entity Data
        /// Model Designer or the Model Browser.
        /// </summary>
        ConceptualModelAssociation = 0x00080,

        /// <summary>
        /// Specifies that CreateProperty should be called when a conceptual model complex type is selected in the Model Browser.
        /// </summary>
        ConceptualModelComplexType = 0x00100,

        /// <summary>
        /// Specifies that CreateProperty should be called when a conceptual model complex property is selected in the Entity
        /// Data Model Designer or the Model Browser.
        /// </summary>
        ConceptualModelComplexProperty = 0x00200,

        /// <summary>
        /// Specifies that CreateProperty should be called when a conceptual model function import is selected in the Model
        /// Browser.
        /// </summary>
        ConceptualModelFunctionImport = 0x00400,

        /// <summary>
        /// Specifies that CreateProperty should be called when a conceptual model function import parameter is selected in the
        /// Model Browser.
        /// </summary>
        ConceptualModelFunctionImportParameter = 0x00800,

        /// <summary>
        /// Specifies that CreateProperty should be called when a storage model entity container is selected in the Model
        /// Browser.
        /// </summary>
        StorageModelEntityContainer = 0x01000,

        /// <summary>
        /// Specifies that CreateProperty should be called when a storage model entity type is selected in the Model Browser.
        /// </summary>
        StorageModelEntityType = 0x02000,

        /// <summary>
        /// Specifies that CreateProperty should be called when a storage model property is selected in the Model Browser.
        /// </summary>
        StorageModelProperty = 0x04000,

        /// <summary>
        /// Specifies that CreateProperty should be called when a storage model association is selected in the Model Browser.
        /// </summary>
        StorageModelAssociation = 0x08000,

        /// <summary>
        /// Specifies that CreateProperty should be called when a storage model function is selected in the Model Browser.
        /// </summary>
        StorageModelFunction = 0x10000,

        /// <summary>
        /// Specifies that CreateProperty should be called when a storage model function parameter is selected in the Model
        /// Browser.
        /// </summary>
        StorageModelFunctionParameter = 0x20000,

    }

}
