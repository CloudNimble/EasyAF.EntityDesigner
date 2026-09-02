// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.VisualStudio.UI
{

    [Serializable]
    internal class ProgressDialogException : Exception
    {
        internal ProgressDialogException(string message)
            : base(message)
        {
        }

        internal ProgressDialogException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected ProgressDialogException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

}
