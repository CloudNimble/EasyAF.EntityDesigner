// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Data.Entity.Design.Model.Commands
{
    [Serializable]
    internal class CommandValidationFailedException : Exception
    {
        internal CommandValidationFailedException(string message)
            : base(message)
        {
        }
    }
}
