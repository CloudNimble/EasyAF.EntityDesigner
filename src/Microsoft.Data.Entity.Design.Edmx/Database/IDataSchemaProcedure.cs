// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Data.Entity.Design.Model.Database
{
    internal interface IDataSchemaProcedure : IRawDataSchemaProcedure
    {
        IList<IDataSchemaParameter> Parameters { get; }
        IList<IDataSchemaColumn> Columns { get; }
        IDataSchemaParameter ReturnValue { get; }
    }
}
