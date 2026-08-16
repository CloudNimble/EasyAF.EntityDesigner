// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration.Configuration;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration.Configuration.Properties;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration.Extensions;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration.Discoverers.Properties
{
    internal class TimestampDiscoverer : IPropertyConfigurationDiscoverer
    {
        public IConfiguration Discover(EdmProperty property, DbModel model)
        {
            Debug.Assert(property != null, "property is null.");
            Debug.Assert(model != null, "model is null.");

            if (!model.GetColumn(property).IsTimestamp())
            {
                // Doesn't apply
                return null;
            }

            return new TimestampConfiguration();
        }
    }
}

