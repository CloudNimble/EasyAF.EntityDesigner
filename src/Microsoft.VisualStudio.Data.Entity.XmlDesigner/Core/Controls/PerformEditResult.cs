// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.Core.Controls
{

    /// <summary>
    ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public enum PerformEditResult
    {
        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        NotAttempted,

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        Success, //Successful edit

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        FailRetry, //Failed Edit, however we will let the consumer to retry

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        FailAbort //Failed Edit and we won't let the consumer to retry
    };

}
