// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Data.Entity.Extensibility
{

    /// <summary>
    /// Creates a unit of work that can be undone or redone with the Undo and Redo buttons in Visual Studio.
    /// </summary>
    /// <example>
    /// <code>
    /// using (var scope = context.CreateChangeScope("Set My Annotation"))
    /// {
    ///     element.SetAttributeValue(MyNamespace + "flag", "true");
    ///     scope.Complete();
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// A change scope is obtained from <see cref="PropertyExtensionContext.CreateChangeScope(string)" /> and is the only
    /// supported way for an extension to edit EDMX content that is open in the designer. Every edit made while the scope is
    /// alive becomes a single entry in the Visual Studio undo stack. Call <see cref="Complete" /> to commit; disposing a scope
    /// that was never completed rolls the edits back, so the usual pattern is a <c>using</c> block with
    /// <see cref="Complete" /> as its last statement.
    /// </remarks>
    public abstract class EntityDesignerChangeScope : IDisposable
    {

        #region Constructors

        /// <summary>
        /// Finalizer for the <see cref="EntityDesignerChangeScope" /> class.
        /// </summary>
        /// <remarks>
        /// Present so that a scope which is abandoned without being disposed still gets a chance to release what it holds. It
        /// is not a substitute for disposing the scope: a scope collected by the finalizer is rolled back, not committed.
        /// </remarks>
        ~EntityDesignerChangeScope()
        {
            Dispose(false);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Commits all operations within a change scope.
        /// </summary>
        /// <remarks>
        /// Indicates that all operations within the scope completed successfully and that they should be applied to the model
        /// as one undoable unit. Once a scope has been completed it is closed for further editing, and any subsequent attempt
        /// to modify the model through it throws an <see cref="InvalidOperationException" />.
        /// </remarks>
        public abstract void Complete();

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="EntityDesignerChangeScope" /> class.
        /// </summary>
        /// <remarks>
        /// Disposing a scope that has not been completed discards every edit made inside it.
        /// </remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Does nothing in this base class. Should be overridden in classes which inherit from this class and which have
        /// resources to release.
        /// </summary>
        /// <param name="disposing">True if this is called from Dispose(), false if called from the finalizer</param>
        /// <remarks>
        /// When <paramref name="disposing" /> is <see langword="false" /> the call came from the finalizer, so only unmanaged
        /// resources may be touched - other managed objects may already have been collected.
        /// </remarks>
        protected virtual void Dispose(bool disposing)
        {
        }

        #endregion

    }

}
