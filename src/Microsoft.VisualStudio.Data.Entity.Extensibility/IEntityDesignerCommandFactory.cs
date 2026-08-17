// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Data.Entity.Design.Extensibility
{

    /// <summary>
    /// Supplies the commands an extension contributes to the context menu of the Entity Data Model Designer.
    /// </summary>
    /// <remarks>
    /// Implementations are discovered through MEF by exporting <see cref="IEntityDesignerCommandFactory" />, and can be scoped to
    /// a layer with <see cref="EntityDesignerLayerAttribute" /> so their commands appear only while that layer is enabled.
    /// <para>
    /// Both this interface and <see cref="EntityDesignerCommand" /> are internal to the designer, so only assemblies granted
    /// <c>InternalsVisibleTo</c> can implement it; it is not part of the third-party extension surface.
    /// </para>
    /// </remarks>
    internal interface IEntityDesignerCommandFactory
    {

        #region Properties

        /// <summary>
        /// Commands that will be surfaced in the Entity Designer
        /// </summary>
        /// <value>The commands to add to the designer's context menu.</value>
        IList<EntityDesignerCommand> Commands { get; }

        #endregion

    }

}
