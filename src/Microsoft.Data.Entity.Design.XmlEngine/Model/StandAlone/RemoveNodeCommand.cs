// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.XmlEngine.Model.StandAlone
{

    /// <summary>
    /// Replays or reverts the removal of a node from an XLinq tree.
    /// </summary>
    /// <remarks>
    /// A removal is exactly the inverse of an insertion, so this command delegates to a private
    /// <see cref="AddNodeCommand" /> with undo and redo swapped rather than duplicating the reinsertion logic.
    /// </remarks>
    internal class RemoveNodeCommand : XmlModelCommand
    {

        #region Fields

        // Remove is simply the reverse of Add.
        /// <summary>
        /// The equivalent insertion whose undo and redo are used in reverse.
        /// </summary>
        private readonly AddNodeCommand _add;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveNodeCommand" /> class.
        /// </summary>
        /// <param name="change">The recorded removal this command replays.</param>
        public RemoveNodeCommand(RemoveNodeChange change)
            : base(change)
        {
            AddNodeChange ac = new AddNodeChange(change.Node, change.Action) { NextNode = change.NextNode, Parent = change.Parent };
            _add = new AddNodeCommand(ac);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Always returns <see langword="false" />; removals are never merged with other commands.
        /// </summary>
        /// <param name="other">The earlier command that is a candidate for merging.</param>
        /// <returns><see langword="false" />.</returns>
        public override bool Merge(ModelCommand other)
        {
            return false;
        }

        /// <summary>
        /// Detaches the node again.
        /// </summary>
        public override void Redo()
        {
            _add.Undo();
        }

        /// <summary>
        /// Re-inserts the node at the position it occupied before it was removed.
        /// </summary>
        public override void Undo()
        {
            _add.Redo();
        }

        #endregion

    }

}
