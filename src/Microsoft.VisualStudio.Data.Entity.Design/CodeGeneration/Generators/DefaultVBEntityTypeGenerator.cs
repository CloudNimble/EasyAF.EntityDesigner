// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;

namespace Microsoft.Data.Entity.Design.CodeGeneration
{
    internal partial class DefaultVBEntityTypeGenerator : IEntityTypeGenerator
    {
        public string Generate(EntitySet entitySet, DbModel model, string codeNamespace)
        {
            Debug.Assert(entitySet != null, "entitySet is null.");
            Debug.Assert(model != null, "model is null.");

            Session = new Dictionary<string, object>
                    {
                        { "EntitySet", entitySet },
                        { "Model", model },
                        { "Namespace", codeNamespace }
                    };
            Initialize();

            return TransformText();
        }
    }
}
