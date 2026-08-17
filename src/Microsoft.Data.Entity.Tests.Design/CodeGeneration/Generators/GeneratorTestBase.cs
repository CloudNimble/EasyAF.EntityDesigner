// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Data.Entity;
using System.Data.Entity.Infrastructure;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration.Generators
{
    public class GeneratorTestBase
    {
        private static DbModel _model;

        protected static DbModel Model
        {
            get
            {
                if (_model == null)
                {
                    DbModelBuilder modelBuilder = new DbModelBuilder();
                    modelBuilder.Entity<Entity>();
                    _model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
                }

                return _model;
            }
        }

        private class Entity
        {
            public int Id { get; set; }
        }
    }
}
