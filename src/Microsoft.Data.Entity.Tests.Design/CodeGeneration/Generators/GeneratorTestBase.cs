// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Threading;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration.Generators
{
    public class GeneratorTestBase
    {
        /// <summary>
        ///     The model every generator test generates from, built exactly once.
        /// </summary>
        /// <remarks>
        ///     <b>Exactly once matters, and a null check does not achieve it.</b> Four test classes derive from
        ///     this base and MSTest runs at <c>MethodLevel</c> parallelism, so two threads could both find the
        ///     field null, both build a model, and the second assignment would replace the first.
        ///     <para>
        ///     That is not a wasted allocation, it is a wrong answer. Every test here reads the property twice -
        ///     once for an entity set and once for the model passed alongside it - and a replacement between
        ///     those two reads hands the generator an entity set belonging to a different <see cref="DbModel" />.
        ///     <c>TableDiscoverer</c> then looks the set up in the model's mappings, finds nothing, and
        ///     <c>First</c> throws "Sequence contains no matching element". That was the intermittent failure
        ///     recorded as issue 1.4 in specs/fixed-bugs.md.
        ///     </para>
        /// </remarks>
        private static readonly Lazy<DbModel> LazyModel =
            new Lazy<DbModel>(BuildModel, LazyThreadSafetyMode.ExecutionAndPublication);

        protected static DbModel Model
        {
            get { return LazyModel.Value; }
        }

        private static DbModel BuildModel()
        {
            DbModelBuilder modelBuilder = new DbModelBuilder();
            modelBuilder.Entity<Entity>();

            return modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
        }

        private class Entity
        {
            public int Id { get; set; }
        }
    }
}
