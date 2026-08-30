// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using Microsoft.Data.Entity.Design.XmlEngine.Model;

namespace Microsoft.Data.Entity.Design.Edmx.Designer
{

    /// <summary>
    ///     Knows which EDMX attributes a diagram's arrangement is computed from.
    /// </summary>
    /// <example>
    ///     <code>
    ///     if (DiagramLayoutInput.Includes(change.Changed))
    ///     {
    ///         diagram.AutoLayoutDiagram();
    ///     }
    ///     </code>
    /// </example>
    /// <remarks>
    ///     One list, in one place, because every one of these is offered in the property window and a setting the
    ///     user can change but cannot see the effect of is worse than no setting at all. Anything added to that
    ///     window which feeds the layout belongs here too. See specs/diagram-layout-engines.md.
    /// </remarks>
    internal static class DiagramLayoutInput
    {

        #region Public Methods

        /// <summary>
        ///     Whether changing <paramref name="changed" /> means the diagram has to be arranged again.
        /// </summary>
        /// <param name="changed">The object a model change reported, which may be <see langword="null" />.</param>
        /// <returns>
        ///     <see langword="true" /> when <paramref name="changed" /> is one of the attributes the layout reads.
        /// </returns>
        internal static bool Includes(EFObject changed)
        {
            if (changed is not DefaultableValue attribute)
            {
                return false;
            }

            var name = attribute.PropertyName;

            return attribute.Parent switch
            {
                Diagram => string.Equals(name, Diagram.AttributeLayoutMode, StringComparison.Ordinal)
                    || string.Equals(name, Diagram.AttributeConnectorMode, StringComparison.Ordinal)
                    || string.Equals(name, Diagram.AttributeEnableGrouping, StringComparison.Ordinal)
                    || string.Equals(name, Diagram.AttributeGenerateGroupNames, StringComparison.Ordinal),

                EntityTypeShape => string.Equals(name, EntityTypeShape.AttributeGroupName, StringComparison.Ordinal),

                _ => false
            };
        }

        #endregion

    }

}
