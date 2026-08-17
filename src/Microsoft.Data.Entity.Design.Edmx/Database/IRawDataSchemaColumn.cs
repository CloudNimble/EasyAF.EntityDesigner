// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Data.Entity.Design.Model.Database
{
    internal interface IRawDataSchemaColumn : IDataSchemaObject
    {
        Type UrtType { get; }
        uint? Size { get; }
        bool IsNullable { get; }
        uint? Precision { get; }
        uint? Scale { get; }
        int ProviderDataType { get; }
        string NativeDataType { get; }
    }
}
