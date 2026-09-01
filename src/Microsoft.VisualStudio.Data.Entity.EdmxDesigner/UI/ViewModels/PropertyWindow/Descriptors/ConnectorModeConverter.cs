// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Designer;
using System;
using System.ComponentModel;
using System.Linq;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.PropertyWindow.Descriptors
{

    /// <summary>
    ///     Offers the connector modes a user can actually choose, which is all of them except
    ///     <see cref="ConnectorMode.Legacy" />.
    /// </summary>
    /// <remarks>
    ///     <see cref="ConnectorMode.Legacy" /> is not a way of drawing connectors so much as the absence of one:
    ///     it says the Modeling SDK draws them, which is a consequence of the layout mode rather than a separate
    ///     decision. The property it filters is only shown at all when the diagram is in modern layout, where
    ///     picking it would mean nothing.
    /// </remarks>
    internal class ConnectorModeConverter : EnumConverter
    {

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConnectorModeConverter" /> class.
        /// </summary>
        public ConnectorModeConverter()
            : base(typeof(ConnectorMode))
        {
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(
                Enum.GetValues(typeof(ConnectorMode))
                    .Cast<ConnectorMode>()
                    .Where(mode => mode != ConnectorMode.Legacy)
                    .ToArray());
        }

        /// <inheritdoc />
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        /// <inheritdoc />
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        #endregion

    }

}
