// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using FluentAssertions;
using Microsoft.VisualStudio.Data.Entity.Design.VisualStudio.ModelWizard.Gui.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resources = Microsoft.Data.Entity.Design.Resources;

namespace Microsoft.Data.Entity.Tests.Design.VisualStudio.ModelWizard.Gui.ViewModels
{
    [TestClass]
    public class EntityFrameworkVersionOptionTests
    {
        [TestMethod]
        public void Ctor_sets_name_and_version()
        {
            Version version = new Version(4, 3, 0, 0);
            EntityFrameworkVersionOption option = new EntityFrameworkVersionOption(version);

            option.Name.Should().Be(
                string.Format(Resources.EntityFrameworkVersionName, new Version(version.Major, version.Minor)));
            option.Version.Should().BeSameAs(version);
        }

        [TestMethod]
        public void Ctor_sets_6_x_name_for_EF6()
        {
            Version version = new Version(6, 0, 0, 0);
            EntityFrameworkVersionOption option = new EntityFrameworkVersionOption(version);

            option.Name.Should().Be(
                string.Format(Resources.EntityFrameworkVersionName, "6.x"));
            option.Version.Should().BeSameAs(version);
        }

    }
}
