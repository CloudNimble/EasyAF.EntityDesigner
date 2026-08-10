// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;

namespace Microsoft.Data.Entity.Design.CodeGeneration
{
    internal abstract class LengthDiscovererBase : IPropertyConfigurationDiscoverer
    {
        protected static readonly PrimitiveTypeKind[] _lengthTypes = new[]
            {
                PrimitiveTypeKind.String,
                PrimitiveTypeKind.Binary
            };

        public abstract IConfiguration Discover(EdmProperty property, DbModel model);
    }
}
