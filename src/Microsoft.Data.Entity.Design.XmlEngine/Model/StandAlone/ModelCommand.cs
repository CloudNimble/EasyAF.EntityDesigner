// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.XmlEngine.Model.StandAlone
{

    /// <summary>
    /// Base class for a reversible edit recorded by a transaction.
    /// </summary>
    /// <remarks>
    /// Commands are recorded in the order the edits were observed and replayed in reverse order to roll a
    /// transaction back, so every command must be able to fully restore the state that preceded it.
    /// </remarks>
    internal abstract class ModelCommand
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelCommand" /> class.
        /// </summary>
        public ModelCommand()
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Determines whether this command can be reapplied in the context of the given transaction.
        /// </summary>
        /// <param name="tx">The transaction the redo would be performed under.</param>
        /// <returns><see langword="true" /> when the command can be redone; otherwise <see langword="false" />.</returns>
        public virtual bool CanRedo(SimpleTransaction tx)
        {
            return true;
        }

        /// <summary>
        /// Determines whether this command can be reverted in the context of the given transaction.
        /// </summary>
        /// <param name="tx">The transaction the undo would be performed under.</param>
        /// <returns><see langword="true" /> when the command can be undone; otherwise <see langword="false" />.</returns>
        public virtual bool CanUndo(SimpleTransaction tx)
        {
            return true;
        }

        /// <summary>
        /// Attempts to absorb an earlier command that targets the same node into this one.
        /// </summary>
        /// <param name="other">The earlier command that is a candidate for merging.</param>
        /// <returns><see langword="true" /> when <paramref name="other" /> was absorbed and can be discarded; otherwise <see langword="false" />.</returns>
        /// <remarks>
        /// Merging is required because the source modifier is not designed to update text it inserted during the
        /// same transaction; collapsing repeated edits of one node into a single command avoids that situation.
        /// </remarks>
        public abstract bool Merge(ModelCommand other);

        /// <summary>
        /// Reapplies the edit represented by this command.
        /// </summary>
        public abstract void Redo();

        /// <summary>
        /// Reverts the edit represented by this command.
        /// </summary>
        public abstract void Undo();

        #endregion

    }

}
