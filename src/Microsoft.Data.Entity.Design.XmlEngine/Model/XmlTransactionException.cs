// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Microsoft.Data.Entity.Design.XmlEngine.Model
{

    /// <summary>
    ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
    /// </summary>
    [Serializable]
    public class XmlTransactionException : Exception
    {
        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public XmlTransactionException()
        {
        }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="message">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        public XmlTransactionException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="message">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        /// <param name="inner">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        public XmlTransactionException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="info">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        /// <param name="context">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        protected XmlTransactionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        // Satisfies rule ImplementISerializableCorrectly.
        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="info">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        /// <param name="context">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }

}
