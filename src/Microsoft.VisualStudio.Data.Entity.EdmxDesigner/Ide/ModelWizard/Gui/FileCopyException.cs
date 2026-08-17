// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide.ModelWizard.Gui
{

    [Serializable]
    internal class FileCopyException : Exception
    {
        internal FileCopyException(string msg)
            : base(msg)
        {
        }

        protected FileCopyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

}