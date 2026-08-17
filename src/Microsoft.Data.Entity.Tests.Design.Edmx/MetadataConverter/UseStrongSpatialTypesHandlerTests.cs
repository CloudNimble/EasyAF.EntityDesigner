// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Globalization;
using System.Xml;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.VersioningFacade;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Tests.Design.Model
{
    /// <summary>
    ///     Tests for UseStrongSpatialTypesHandler.
    ///     Note: Only Version3 (EF6) is now supported as a target. V1/V2 downgrade tests have been removed.
    /// </summary>
    [TestClass]
    public class UseStrongSpatialTypesHandlerTests
    {
        private const string EdmxTemplate = @"
<edmx:Edmx Version=""{0}"" xmlns:edmx=""{1}"">
  <edmx:Runtime>
    <edmx:StorageModels>
      <Schema Namespace=""Model1.Store"" Alias=""Self"" Provider=""System.Data.SqlClient"" ProviderManifestToken=""2005"" xmlns=""{2}"">
        <EntityContainer Name=""Model1TargetContainer"">
        </EntityContainer>
      </Schema>
    </edmx:StorageModels>
    <!-- CSDL content -->
    <edmx:ConceptualModels>
      <Schema Namespace=""Model1"" Alias=""Self"" xmlns=""{3}"" {4}>
        <EntityContainer Name=""Model1Container"">
        </EntityContainer>
      </Schema>
    </edmx:ConceptualModels>
    <!-- C-S mapping content -->
    <edmx:Mappings>
      <Mapping Space=""C-S"" xmlns=""{5}"">
        <Alias Key=""Model"" Value=""Model1"" />
        <Alias Key=""Target"" Value=""Model1.Store"" />
        <EntityContainerMapping CdmEntityContainer=""Model1Container"" StorageEntityContainer=""Model1TargetContainer"">
        </EntityContainerMapping>
      </Mapping>
    </edmx:Mappings>
  </edmx:Runtime>
</edmx:Edmx>";

        private readonly string V3EdmxWithoutUseStrongSpatialTypes =
            string.Format(
                CultureInfo.InvariantCulture,
                EdmxTemplate,
                new[]
                    {
                        "3.0",
                        SchemaManager.GetEDMXNamespaceName(EntityFrameworkVersion.Version3),
                        SchemaManager.GetSSDLNamespaceName(EntityFrameworkVersion.Version3),
                        SchemaManager.GetCSDLNamespaceName(EntityFrameworkVersion.Version3),
                        string.Empty,
                        SchemaManager.GetMSLNamespaceName(EntityFrameworkVersion.Version3)
                    });

        [TestMethod]
        public void HandleConversion_Targeting_EntityFramework_V3_Inserts_UseStrongSpatialTypes_Attribute()
        {
            var inputDoc = LoadEdmx(V3EdmxWithoutUseStrongSpatialTypes);
            UseStrongSpatialTypesHandler handler = new UseStrongSpatialTypesHandler(EntityFrameworkVersion.Version3);
            var resultDoc = handler.HandleConversion(inputDoc);
            var nsmgr = SchemaManager.GetEdmxNamespaceManager(resultDoc.NameTable, EntityFrameworkVersion.Version3);
            nsmgr.AddNamespace("annotation", SchemaManager.GetAnnotationNamespaceName());

            XmlElement schemaElement = (XmlElement)resultDoc.SelectSingleNode("/edmx:Edmx/edmx:Runtime/edmx:ConceptualModels/csdl:Schema", nsmgr);
            schemaElement.Attributes["annotation", "http://www.w3.org/2000/xmlns/"].Should().NotBeNull();
            XmlAttribute useStrongSpatialTypeAttr =
                (XmlAttribute)
                resultDoc.SelectSingleNode(
                    "/edmx:Edmx/edmx:Runtime/edmx:ConceptualModels/csdl:Schema/@annotation:UseStrongSpatialTypes", nsmgr);
            useStrongSpatialTypeAttr.Value.Should().Be("false");
        }

        private static XmlDocument LoadEdmx(string edmx)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(edmx);
            return doc;
        }
    }
}
