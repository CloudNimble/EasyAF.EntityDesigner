// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Data.Tools.XmlDesignerBase.Base.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Data.Tools.Tests.Design.XmlCore.Base.Util
{
    [TestClass]
    public class UtilsTests
    {
        [TestMethod]
        public void Filename2Uri_can_handle_file_uris_with_hashes()
        {
            var localPath = @"C:\C# Projects\#pie#";

            Utils.FileName2Uri("file://" + localPath).LocalPath
                .Should().Be(localPath);

            Utils.FileName2Uri(localPath).LocalPath
                .Should().Be(localPath);
        }
    }
}
