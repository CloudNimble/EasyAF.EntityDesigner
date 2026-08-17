// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

namespace Microsoft.Data.Entity.Design.Extensibility
{

    /// <summary>
    /// Specifies objects in the Entity Data Model Designer or the Model Browser that, when selected by a user, cause the
    /// <see cref="IEntityDesignerExtendedProperty.CreateProperty(System.Xml.Linq.XElement, PropertyExtensionContext)" /> method
    /// of the annotated class to be called.
    /// </summary>
    /// <example>
    /// This attribute is MEF metadata: it only has an effect when it sits alongside an <c>Export</c> of
    /// <see cref="IEntityDesignerExtendedProperty" />. Combine flags to cover several kinds of selection with one extension.
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
    /// Used by Managed Extensibility Framework extensions to specify the scope of operations, based on the user's selection in
    /// the Entity Designer. The designer maps whatever the user selected onto a single
    /// <see cref="Extensibility.EntityDesignerSelection" /> value and invokes only those property extensions whose flags include
    /// it, so an extension is never asked to describe a kind of object it did not opt into.
    /// </remarks>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class EntityDesignerExtendedPropertyAttribute : Attribute
    {

        #region Fields

        private readonly EntityDesignerSelection _entityDesignerSelection;

        #endregion

        #region Properties

        /// <summary>
        /// The object in the Entity Data Model Designer or the Model Browser that, when selected by a user, triggers the call of
        /// the <see cref="IEntityDesignerExtendedProperty.CreateProperty(System.Xml.Linq.XElement, PropertyExtensionContext)" />
        /// method.
        /// </summary>
        /// <value>The set of selections the annotated extension contributes properties for.</value>
        public EntityDesignerSelection EntityDesignerSelection
        {
            get => _entityDesignerSelection;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Instantiates a new instance of the <see cref="EntityDesignerExtendedPropertyAttribute" /> class.
        /// </summary>
        /// <param name="entityDesignerSelection">The object in the Entity Data Model Designer or the Model Browser that, when selected by a user, triggers the call of the CreateProperty method.</param>
        public EntityDesignerExtendedPropertyAttribute(EntityDesignerSelection entityDesignerSelection)
        {
            _entityDesignerSelection = entityDesignerSelection;
        }

        #endregion

    }

}
