// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Data.Entity.Design.XmlEngine.Model
{

    /// <summary>
    ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public class XmlTransactionEventArgs : EventArgs
    {
        private readonly XmlTransaction _tx;
        private readonly bool _designerTransaction;

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="transaction">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        /// <param name="designerTransaction">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        public XmlTransactionEventArgs(XmlTransaction transaction, bool designerTransaction)
        {
            _tx = transaction;
            _designerTransaction = designerTransaction;
        }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public XmlTransaction Transaction => _tx;

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public bool DesignerTransaction => _designerTransaction;
    }

}
