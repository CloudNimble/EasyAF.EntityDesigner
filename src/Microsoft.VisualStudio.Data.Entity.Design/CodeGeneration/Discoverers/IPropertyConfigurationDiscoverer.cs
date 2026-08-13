// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration.Configuration;

namespace Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration.Discoverers
{
    internal interface IPropertyConfigurationDiscoverer
    {
        IConfiguration Discover(EdmProperty property, DbModel model);
    }
}
