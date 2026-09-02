// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Data.Entity.Design.XmlEngine.Model
{

    /// <summary>
    ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public abstract class XmlTransaction : IDisposable
    {
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
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public abstract XmlModelProvider Provider { get; }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public abstract XmlTransaction Parent { get; }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public abstract XmlTransactionStatus Status { get; }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <returns>This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</returns>
        public abstract IEnumerable<IXmlChange> Changes();

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="model">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        /// <returns>This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</returns>
        public abstract IEnumerable<IXmlChange> Changes(XmlModel model);

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public abstract void Commit();

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public abstract void Rollback();

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public abstract object UserState { get; }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public abstract object UndoUserState { get; }
    }

}
