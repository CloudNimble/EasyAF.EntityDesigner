// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.VisualStudio.Data.Entity.Design.Ide.ModelWizard.Engine
{
    // <summary>
    //     Aggregate the tables/views/sprocs for display in the wizard by connecting to
    //     a database server
    // </summary>
    internal class DatabaseConnectionSsdlAggregator
    {
        private readonly ISchemaListingSettings _settings;

        internal DatabaseConnectionSsdlAggregator(ISchemaListingSettings settings)
        {
            _settings = settings;
        }

        public ICollection<EntityStoreSchemaFilterEntry> GetTableFilterEntries(DoWorkEventArgs args)
        {
            return DatabaseMetadataQueryTool.GetTablesFilterEntries(_settings, args);
        }

        public ICollection<EntityStoreSchemaFilterEntry> GetViewFilterEntries(DoWorkEventArgs args)
        {
            return DatabaseMetadataQueryTool.GetViewFilterEntries(_settings, args);
        }

        public ICollection<EntityStoreSchemaFilterEntry> GetFunctionFilterEntries(DoWorkEventArgs args)
        {
            return DatabaseMetadataQueryTool.GetFunctionsFilterEntries(_settings, args);
        }
    }
}
