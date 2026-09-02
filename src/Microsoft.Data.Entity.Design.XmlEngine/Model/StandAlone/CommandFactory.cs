// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.XmlEngine.Model.StandAlone
{

    /// <summary>
    /// Creates the undo/redo commands that a <see cref="SimpleTransactionLogger" /> records for each observed change.
    /// </summary>
    /// <remarks>
    /// The factory exists so a derived <see cref="VanillaXmlModelProvider" /> can substitute commands that also push
    /// the edit into a host specific text buffer, without the logger needing to know which host it is running under.
    /// </remarks>
    internal class CommandFactory
    {

        #region Internal Methods

        /// <summary>
        /// Creates the command that replays or reverts a node insertion.
        /// </summary>
        /// <param name="provider">The provider the change was observed on.</param>
        /// <param name="change">The recorded insertion.</param>
        /// <returns>A command able to undo and redo the insertion.</returns>
        internal virtual AddNodeCommand CreateAddNodeCommand(
            VanillaXmlModelProvider provider, AddNodeChange change)
        {
            return new AddNodeCommand(change);
        }

        /// <summary>
        /// Creates the command that replays or reverts a node removal.
        /// </summary>
        /// <param name="provider">The provider the change was observed on.</param>
        /// <param name="change">The recorded removal.</param>
        /// <returns>A command able to undo and redo the removal.</returns>
        internal virtual RemoveNodeCommand CreateRemoveNodeCommand(
            VanillaXmlModelProvider provider, RemoveNodeChange change)
        {
            return new RemoveNodeCommand(change);
        }

        /// <summary>
        /// Creates the command that replays or reverts a rename.
        /// </summary>
        /// <param name="provider">The provider the change was observed on.</param>
        /// <param name="change">The recorded rename.</param>
        /// <returns>A command able to undo and redo the rename.</returns>
        internal virtual NodeNameCommand CreateSetNameCommand(
            VanillaXmlModelProvider provider, NodeNameChange change)
        {
            return new NodeNameCommand(change);
        }

        /// <summary>
        /// Creates the command that replays or reverts a value change.
        /// </summary>
        /// <param name="provider">The provider the change was observed on.</param>
        /// <param name="change">The recorded value change.</param>
        /// <returns>A command able to undo and redo the value change.</returns>
        internal virtual NodeValueCommmand CreateSetValueCommand(
            VanillaXmlModelProvider provider, NodeValueChange change)
        {
            return new NodeValueCommmand(change);
        }

        #endregion

    }

}
