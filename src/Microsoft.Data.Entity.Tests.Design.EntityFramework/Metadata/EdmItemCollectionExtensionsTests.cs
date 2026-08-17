// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Data.Entity.Core.Metadata.Edm;
using System.Xml.Linq;
using Microsoft.Data.Entity.Design.VersioningFacade.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.Metadata
{
    [TestClass]
    public class EdmItemCollectionExtensionsTests
    {
        [TestMethod]
        public void EdmItemCollectionExtensions_CsdlVersion_returns_correct_result_for_known_Csdl_versions()
        {
            const string csdlTemplate = @"<Schema xmlns=""{0}"" Namespace=""ExampleModel"" />";

            var csdlVersions =
                new[]
                    {
                        new Tuple<Version, string>(new Version(1, 0, 0, 0), "http://schemas.microsoft.com/ado/2006/04/edm"),
                        new Tuple<Version, string>(new Version(1, 1, 0, 0), "http://schemas.microsoft.com/ado/2007/05/edm"),
                        new Tuple<Version, string>(new Version(2, 0, 0, 0), "http://schemas.microsoft.com/ado/2008/09/edm"),
                        new Tuple<Version, string>(new Version(3, 0, 0, 0), "http://schemas.microsoft.com/ado/2009/11/edm"),
                    };

            foreach (var csdlVersion in csdlVersions)
            {
                EdmItemCollection edmItemCollection = new EdmItemCollection(
                    new[]
                        {
                            XDocument.Parse(string.Format(csdlTemplate, csdlVersion.Item2)).CreateReader()
                        });

                edmItemCollection.CsdlVersion().Should().Be(csdlVersion.Item1);
            }
        }
    }
}
