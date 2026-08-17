// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Xml.Linq;

namespace Microsoft.VisualStudio.Data.Entity.Extensibility
{

    /// <summary>
    /// Exposes methods for adding properties to objects that are visible to a user in the Entity Data Model Designer or the
    /// Model Browser.
    /// </summary>
    /// <example>
    /// A property extension is discovered through MEF. It must be exported as <see cref="IEntityDesignerExtendedProperty" /> and
    /// annotated with <see cref="EntityDesignerExtendedPropertyAttribute" /> to say which selections it applies to.
    /// <code>
    /// [Export(typeof(IEntityDesignerExtendedProperty))]
    /// [EntityDesignerExtendedProperty(EntityDesignerSelection.ConceptualModelEntityType)]
    /// public class MyEntityProperties : IEntityDesignerExtendedProperty
    /// {
    ///     public object CreateProperty(XElement xElement, PropertyExtensionContext context)
    ///     {
    ///         // Anything returned here has its public properties shown in the Properties window.
    ///         return new MyEntityDescriptor(xElement, context);
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// Implement this interface to add rows to the Visual Studio Properties window for objects the user selects in the designer
    /// or the Model Browser. The object returned from <see cref="CreateProperty(XElement, PropertyExtensionContext)" /> is
    /// handed to the property grid as-is, so the usual approach is to return a small wrapper whose properties read and write
    /// annotations on the supplied element. Writing back requires a change scope from
    /// <see cref="PropertyExtensionContext.CreateChangeScope(string)" />.
    /// </remarks>
    public interface IEntityDesignerExtendedProperty
    {

        #region Public Methods

        /// <summary>
        /// Creates a new property for an object that is selected in the Entity Data Model Designer or the Model Browser.
        /// </summary>
        /// <param name="xElement">The element in the .edmx file that defines the object that is selected in the Entity Data Model Designer or the Model Browser</param>
        /// <param name="context">Provides file and project information.</param>
        /// <returns>
        /// An object whose public properties are displayed in the Visual Studio Properties window. For more information, see
        /// <see cref="T:System.Windows.Forms.PropertyGrid" />.
        /// </returns>
        /// <remarks>
        /// Called when the selected object changes in the ADO.NET Entity Designer. An implementation should return a new
        /// instance of a class whose public properties should be shown in the VS property window. An implementation may return
        /// "null" to not show the property. Any exceptions thrown by an implementation of CreateProperty() are shown to the user
        /// in a standard dialog box. Extensions are responsible for localizing exception messages.
        /// </remarks>
        object CreateProperty(XElement xElement, PropertyExtensionContext context);

        #endregion

    }

}
