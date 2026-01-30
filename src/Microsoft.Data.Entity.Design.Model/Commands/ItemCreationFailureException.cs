// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Data.Entity.Design.Model.Commands
{
    [Serializable]
    internal class ItemCreationFailureException : Exception
    {
        internal ItemCreationFailureException()
        {
        }

        protected ItemCreationFailureException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
