// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;

namespace Microsoft.Data.Entity.Design.XmlEngine.Model.StandAlone
{

    /// <summary>
    /// A host independent <see cref="XmlModelProvider" /> that loads XML documents straight from a <see cref="Uri" />
    /// and tracks edits with its own in-process transaction and lock managers.
    /// </summary>
    /// <remarks>
    /// This is the "vanilla" counterpart to the Visual Studio XML editor backed provider. It exists so tooling can
    /// read and write EDMX files without a running shell, which means it cannot delegate transactions, locking or
    /// undo to editor services and owns all three itself. Derived providers override <see cref="Build" /> to change
    /// where the XML comes from.
    /// </remarks>
    public class VanillaXmlModelProvider : XmlModelProvider
    {

        #region Fields

        /// <summary>
        /// Number of characters from the reported line position back to the start of a CDATA section.
        /// </summary>
        /// <remarks>
        /// The parser reports the position just past "&lt;![CDATA[", so nine delimiter characters plus one for the
        /// one-based column numbering have to be subtracted to reach the opening bracket.
        /// </remarks>
        public const int CDataOffset = 10;

        /// <summary>
        /// Number of characters from the reported line position back to the start of a comment ("&lt;!--" plus one
        /// for the one-based column numbering).
        /// </summary>
        public const int CommentOffset = 5;

        /// <summary>
        /// Number of characters from the reported line position back to the start of an element start tag ("&lt;"
        /// plus one for the one-based column numbering).
        /// </summary>
        public const int ElementStartTagOffset = 2;

        /// <summary>
        /// Number of characters occupied by the closing delimiter of an empty element ("/&gt;" plus one for the
        /// one-based column numbering).
        /// </summary>
        public const int EmptyElementOffset = 3;

        /// <summary>
        /// Number of characters from the reported line position back to the start of a processing instruction
        /// ("&lt;?" plus one for the one-based column numbering).
        /// </summary>
        public const int ProcessingInstructionOffset = 3;

        /// <summary>
        /// Number of characters from the reported line position back to the start of a text node, which is one for
        /// the one-based column numbering since text has no delimiter.
        /// </summary>
        public const int TextOffset = 1;

        /// <summary>
        /// The factory used to create the undo/redo commands recorded by transactions on this provider.
        /// </summary>
        private readonly CommandFactory _factory;

        /// <summary>
        /// The models this provider has open, keyed by the URI they were loaded from.
        /// </summary>
        private readonly Dictionary<Uri, SimpleXmlModel> _models;

        /// <summary>
        /// The transaction manager that owns the transaction stack and document locks for this provider.
        /// </summary>
        private readonly SimpleTransactionManager _txmanager;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the innermost transaction that is currently open on this provider.
        /// </summary>
        /// <remarks>
        /// Returns <see langword="null" /> when no transaction is open.
        /// </remarks>
        public override XmlTransaction CurrentTransaction => _txmanager.GetCurrentTransaction(this);

        /// <summary>
        /// Gets the models this provider currently has open.
        /// </summary>
        public override IEnumerable<XmlModel> OpenXmlModels
        {
            get
            {
                foreach (var model in _models.Values)
                {
                    yield return model;
                }
            }
        }

        /// <summary>
        /// Gets the factory used to create the undo/redo commands recorded by transactions on this provider.
        /// </summary>
        internal CommandFactory CommandFactory => _factory;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="VanillaXmlModelProvider" /> class with no models open.
        /// </summary>
        public VanillaXmlModelProvider()
        {
            _models = [];
            _txmanager = new SimpleTransactionManager();
            _factory = new CommandFactory();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Opens a transaction over every model this provider has open.
        /// </summary>
        /// <param name="name">A descriptive name for the transaction.</param>
        /// <param name="userState">Arbitrary state the caller wants to associate with the transaction.</param>
        /// <returns>The newly opened, active transaction.</returns>
        /// <remarks>
        /// When a transaction is already open the new one is nested inside it, which suspends change tracking on
        /// the outer transaction until the inner one completes.
        /// </remarks>
        public override XmlTransaction BeginTransaction(string name, object userState)
        {
            return _txmanager.BeginTransaction(this, name, CurrentTransaction as SimpleTransaction, userState);
        }

        /// <summary>
        /// Closes and disposes the model loaded from the given URI.
        /// </summary>
        /// <param name="source">The URI of the model to close.</param>
        public override void CloseXmlModel(Uri source)
        {
            if (_models.TryGetValue(source, out SimpleXmlModel model))
            {
                _models.Remove(source);
                model.Dispose();
            }
            base.CloseXmlModel(source);
        }

        /// <summary>
        /// Gets the model for a URI, loading and parsing the document on first request.
        /// </summary>
        /// <param name="source">The URI of the document to load.</param>
        /// <returns>The model for <paramref name="source" />.</returns>
        public override XmlModel GetXmlModel(Uri source)
        {
            if (!_models.TryGetValue(source, out SimpleXmlModel model))
            {
                var doc = Build(source);
                model = _models[source] = new SimpleXmlModel(source, doc);
            }

            return model;
        }

        /// <summary>
        /// Re-keys an open model after its underlying file has been renamed.
        /// </summary>
        /// <param name="oldName">The URI the model is currently registered under.</param>
        /// <param name="newName">The URI the model should be registered under.</param>
        /// <returns><see langword="true" /> when a model was found and re-keyed; otherwise <see langword="false" />.</returns>
        /// <remarks>
        /// The document itself is untouched; only the provider's lookup table and the model's reported name change,
        /// so existing references to the model and to its XLinq tree stay valid across a rename.
        /// </remarks>
        public override bool RenameXmlModel(Uri oldName, Uri newName)
        {
            if (_models.TryGetValue(oldName, out SimpleXmlModel model))
            {
                model.SetName(newName);
                _models.Remove(oldName);
                _models.Add(newName, model);
                Debug.Assert(new Uri(model.Name) == newName, "new Uri(model.Name) == newName");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Loads and parses the document at a URI.
        /// </summary>
        /// <param name="uri">The URI of the document to load.</param>
        /// <returns>The parsed document, annotated with the text ranges of its nodes.</returns>
        /// <remarks>
        /// Derived providers override this to source the XML from somewhere other than the URI itself, for example
        /// from an in-memory string.
        /// </remarks>
        protected virtual XDocument Build(Uri uri)
        {
            return new AnnotatedTreeBuilder().Build(uri);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Raises the transaction completed event for a transaction that has just finished.
        /// </summary>
        /// <param name="tx">The transaction that completed.</param>
        internal void FireTransactionCompleted(SimpleTransaction tx)
        {
            OnTransactionCompleted(new XmlTransactionEventArgs(tx, tx.DesignerTransaction));
        }

        #endregion

    }

}
