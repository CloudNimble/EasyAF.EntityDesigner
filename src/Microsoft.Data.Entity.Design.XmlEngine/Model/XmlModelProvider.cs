// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;

namespace Microsoft.Data.Entity.Design.XmlEngine.Model
{

    /// <summary>
    ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public abstract class XmlModelProvider : IDisposable
    {
        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        protected XmlModelProvider()
        {
            IsDisposed = false;
        }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
            IsDisposed = true;
        }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="disposing">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <remarks>
        ///     Return the VisualStudio TextSpan object for the given xobject.  If the given xobject is null, this
        ///     will return a new TextSpan with values of 0.
        ///     Virtual to allow mocking.
        /// </remarks>
        public virtual TextSpan GetTextSpanForXObject(XObject xobject, Uri uri)
        {
            Debug.Assert(uri != null, "uri != null");

            return xobject != null
                       ? GetXmlModel(uri).GetTextSpan(xobject)
                       : new TextSpan();
        }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public abstract XmlModel GetXmlModel(Uri source);

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="source">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        public virtual void CloseXmlModel(Uri source)
        {
            // if the last xml-model is closed, dispose the model-provider as well.
            var isThereOpenXmlModel = false;
            foreach (var model in OpenXmlModels)
            {
                if (model.IsDisposed == false)
                {
                    isThereOpenXmlModel = true;
                    break;
                }
            }

            if (isThereOpenXmlModel == false)
            {
                Dispose();
            }
        }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public abstract IEnumerable<XmlModel> OpenXmlModels { get; }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public event EventHandler<XmlTransactionEventArgs> TransactionCompleted;

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="args">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        protected virtual void OnTransactionCompleted(XmlTransactionEventArgs args)
        {
            if (TransactionCompleted != null)
            {
                TransactionCompleted(this, args);
            }
        }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public event EventHandler<XmlTransactionEventArgs> UndoRedoCompleted;

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="args">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        protected virtual void OnUndoRedoCompleted(XmlTransactionEventArgs args)
        {
            if (UndoRedoCompleted != null)
            {
                UndoRedoCompleted(this, args);
            }
        }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="name">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        /// <param name="userState">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        /// <returns>This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</returns>
        public abstract XmlTransaction BeginTransaction(string name, object userState);

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public abstract XmlTransaction CurrentTransaction { get; }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="oldName">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        /// <param name="newName">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        /// <returns>This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</returns>
        public abstract bool RenameXmlModel(Uri oldName, Uri newName);

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="name">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        public virtual void BeginUndoScope(string name)
        {
        }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public virtual void EndUndoScope()
        {
        }
    }

}
