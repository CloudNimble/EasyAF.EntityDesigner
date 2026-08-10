// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Data.Entity.Infrastructure;

namespace Microsoft.Data.Entity.Design.CodeGeneration
{
    internal interface IContextGenerator
    {
        string Generate(DbModel model, string codeNamespace, string contextClassName, string connectionStringName);
    }
}