// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Data.Entity.Design.Model.Eventing
{
    [Serializable]
    internal class FileNotEditableException : Exception
    {
        internal FileNotEditableException(string message)
            : base(message)
        {
        }
    }
}
