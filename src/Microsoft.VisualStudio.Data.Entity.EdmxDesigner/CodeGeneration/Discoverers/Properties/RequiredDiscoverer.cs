// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration.Configuration;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration.Configuration.Properties;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration.Extensions;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration.Discoverers.Properties
{
    internal class RequiredDiscoverer : IPropertyConfigurationDiscoverer
    {
        public IConfiguration Discover(EdmProperty property, DbModel model)
        {
            Debug.Assert(property != null, "property is null.");
            Debug.Assert(model != null, "model is null.");

            if (property.Nullable
                || property.PrimitiveType.ClrEquivalentType.IsValueType
                || property.IsKey()
                || model.GetColumn(property).IsTimestamp())
            {
                // By convention
                return null;
            }

            return new RequiredConfiguration();
        }
    }
}

