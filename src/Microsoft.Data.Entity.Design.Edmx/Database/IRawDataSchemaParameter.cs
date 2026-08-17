// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Data;

namespace Microsoft.Data.Entity.Design.Model.Database
{
    internal interface IRawDataSchemaParameter : IDataSchemaObject
    {
        Type UrtType { get; }
        ParameterDirection Direction { get; }
        int Size { get; }
        int Precision { get; }
        int Scale { get; }
        int ProviderDataType { get; }
        string NativeDataType { get; }
    }
}
