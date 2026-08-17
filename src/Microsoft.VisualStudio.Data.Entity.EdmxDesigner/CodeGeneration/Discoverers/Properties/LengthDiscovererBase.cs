// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration.Configuration;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration.Discoverers.Properties
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
