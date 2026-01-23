// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio.ModelWizard.Gui.ViewModels
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class EntityFrameworkVersionOptionTests
    {
        [TestMethod]
        public void Ctor_sets_name_and_version()
        {
            var version = new Version(4, 3, 0, 0);
            var option = new EntityFrameworkVersionOption(version);

            Assert.Equal(
                string.Format(Resources.EntityFrameworkVersionName, new Version(version.Major, version.Minor)),
                option.Name);
            option.Version.Should().BeSameAs(version);
        }

        [TestMethod]
        public void Ctor_sets_6_x_name_for_EF6()
        {
            var version = new Version(6, 0, 0, 0);
            var option = new EntityFrameworkVersionOption(version);

            Assert.Equal(
                string.Format(Resources.EntityFrameworkVersionName, "6.x"),
                option.Name);
            option.Version.Should().BeSameAs(version);
        }

    }
}
