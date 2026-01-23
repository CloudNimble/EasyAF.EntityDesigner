// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio.Package
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class ModelGenErrorCacheTests
    {
        [TestMethod]
        public void Can_add_get_remove_errors()
        {
            var errorCache = new ModelGenErrorCache();
            var errors = new List<EdmSchemaError>(new[] { new EdmSchemaError("test", 42, EdmSchemaErrorSeverity.Error) });

            errorCache.AddErrors("abc", errors);
            Assert.Same(errors, errorCache.GetErrors("abc"));

            errorCache.RemoveErrors("abc");
            errorCache.GetErrors("abc".Should().BeNull());
        }

        [TestMethod]
        public void GetErrors_returns_null_if_no_errors_for_file_name()
        {
            new ModelGenErrorCache(.Should().BeNull().GetErrors("abc"));
        }

        [TestMethod]
        public void Removing_non_existing_errors_does_not_fail()
        {
            new ModelGenErrorCache().RemoveErrors("abc");
        }
    }
}
