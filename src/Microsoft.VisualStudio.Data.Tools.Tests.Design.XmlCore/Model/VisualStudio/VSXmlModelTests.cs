// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.Data.Tools.VSXmlDesignerBase.Model.VisualStudio;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.XmlEditor;

namespace Microsoft.VisualStudio.Data.Tools.Tests.Design.XmlCore.Model.VisualStudio
{
    [TestClass]
    public class VSXmlModelTests
    {
        private class XmlModelMock : XmlModel
        {
            private readonly string _name;

            public XmlModelMock(string name)
            {
                _name = name;
            }

            public override event EventHandler BufferReloaded
            {
                add { }
                remove { }
            }

            public override void Dispose()
            {
            }

            public override XDocument Document
            {
                get { throw new NotImplementedException(); }
            }

            public override TextSpan GetTextSpan(XObject node)
            {
                throw new NotImplementedException();
            }

            public override string Name
            {
                get { return _name; }
            }

            public override XmlModelSaveAction SaveActionOnDispose
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            public override XmlStore Store
            {
                get { throw new NotImplementedException(); }
            }
        }

        [TestMethod]
        public void VSXmlModel_returns_correct_local_path_for_Uri_with_hashes()
        {
            var localPath = @"C:\C# Projects\#pie#";

            new VSXmlModel(null, new XmlModelMock("file://" + localPath)).Uri.LocalPath
                .Should().Be(localPath);

            new VSXmlModel(null, new XmlModelMock(localPath)).Uri.LocalPath
                .Should().Be(localPath);
        }
    }
}
