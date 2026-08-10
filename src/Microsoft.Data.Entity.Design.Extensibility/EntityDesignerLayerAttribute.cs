// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

namespace Microsoft.Data.Entity.Design.Extensibility
{

    /// <summary>
    /// Attribute used to specify that an Extension belongs to a particular layer
    /// </summary>
    /// <example>
    /// This attribute is MEF metadata: apply it next to the <c>Export</c> of any extension that should only be active while a
    /// given layer is enabled. The name must match the <see cref="IEntityDesignerLayer.Name" /> of the layer.
    /// <code>
    /// [Export(typeof(IModelTransformExtension))]
    /// [EntityDesignerLayer("My Layer")]
    /// public class MyLayerTransform : IModelTransformExtension
    /// {
    ///     // ...
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// Layer membership acts as a filter at discovery time. When the layer is switched off - and whenever no layer manager is
    /// present at all, such as outside the designer - extensions carrying a layer name are dropped and never invoked. Extensions
    /// without this attribute are always active.
    /// </remarks>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class EntityDesignerLayerAttribute : Attribute
    {

        #region Fields

        private readonly string _layerName;

        #endregion

        #region Properties

        /// <summary>
        /// Unique name specifying the layer (a logical collection of extensions)
        /// </summary>
        /// <value>The name of the layer this extension belongs to.</value>
        public string LayerName
        {
            get => _layerName;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an EntityDesignerLayerAttribute given a particular layer name
        /// </summary>
        /// <param name="layerName">Unique name specifying the layer (a logical collection of extensions)</param>
        public EntityDesignerLayerAttribute(string layerName)
        {
            _layerName = layerName;
        }

        #endregion

    }

}
