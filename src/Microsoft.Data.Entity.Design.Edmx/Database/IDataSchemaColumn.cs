// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Data;

namespace Microsoft.Data.Entity.Design.Model.Database
{
    internal interface IDataSchemaColumn : IRawDataSchemaColumn
    {
        DbType DbType { get; }
    }
}
