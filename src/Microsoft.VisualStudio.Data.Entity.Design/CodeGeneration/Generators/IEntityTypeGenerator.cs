// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;

namespace Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration.Generators
{
    internal interface IEntityTypeGenerator
    {
        string Generate(EntitySet entitySet, DbModel model, string codeNamespace);
    }
}
