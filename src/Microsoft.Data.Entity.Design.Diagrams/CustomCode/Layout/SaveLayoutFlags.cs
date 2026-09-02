// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Modeling.Diagrams;
using Microsoft.VisualStudio.Modeling.Diagrams.GraphObject;
using System;
using System.Collections;

namespace Microsoft.Data.Entity.Design.Diagrams.Layout
{

    /// <summary>
    ///     Saves the <see cref="NodeShape.LayoutObjectFixedFlags" /> of a set of shapes and restores them on
    ///     dispose, optionally setting them all to one value for the duration.
    /// </summary>
    /// <example>
    ///     <code>
    ///     using (new SaveLayoutFlags(inheritanceShapes, VGNodeFixedStates.FixedPlace))
    ///     {
    ///         surface.AutoLayoutShapeElements(shapes, routingStyle, placementStyle, false);
    ///     }
    ///     </code>
    /// </example>
    /// <remarks>
    ///     This is how a layout pass is told to leave some shapes alone. The Modeling SDK's layout engine reads
    ///     these flags off each node, so freezing a subset means setting the flags, laying out, and putting them
    ///     back — and putting them back matters, because the flags outlive the pass that set them.
    /// </remarks>
    internal sealed class SaveLayoutFlags : IDisposable
    {

        #region Fields

        private readonly IList _elements;
        private VGNodeFixedStates[] _savedFlags;

        #endregion

        #region Constructors

        /// <summary>
        ///     Saves the current flags of <paramref name="elements" /> without changing them.
        /// </summary>
        /// <param name="elements">The shapes whose flags should be preserved.</param>
        internal SaveLayoutFlags(IList elements)
        {
            _elements = elements;
            Save();
        }

        /// <summary>
        ///     Saves the current flags of <paramref name="elements" /> and sets them all to <paramref name="flags" />.
        /// </summary>
        /// <param name="elements">The shapes whose flags should be preserved.</param>
        /// <param name="flags">The value to apply until this instance is disposed.</param>
        internal SaveLayoutFlags(IList elements, VGNodeFixedStates flags)
            : this(elements)
        {
            SetFlags(flags);
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Restores the flags saved when this instance was created.
        /// </summary>
        public void Dispose()
        {
            Restore();
        }

        /// <summary>
        ///     Sets the flags on every shape to the same value.
        /// </summary>
        /// <param name="flags">The value to apply.</param>
        public void SetFlags(VGNodeFixedStates flags)
        {
            for (var i = 0; i < _elements.Count; i++)
            {
                NodeShape node = _elements[i] as NodeShape;
                node?.LayoutObjectFixedFlags = flags;
            }
        }

        #endregion

        #region Private Methods

        private void Restore()
        {
            for (var i = 0; i < _elements.Count; i++)
            {
                NodeShape node = _elements[i] as NodeShape;
                node?.LayoutObjectFixedFlags = _savedFlags[i];
            }
        }

        private void Save()
        {
            _savedFlags = new VGNodeFixedStates[_elements.Count];
            for (var i = 0; i < _elements.Count; i++)
            {
                if (_elements[i] is NodeShape node)
                {
                    _savedFlags[i] = node.LayoutObjectFixedFlags;
                }
            }
        }

        #endregion

    }

}
