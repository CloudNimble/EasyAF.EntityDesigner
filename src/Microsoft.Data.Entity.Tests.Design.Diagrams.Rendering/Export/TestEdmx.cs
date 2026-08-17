// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.Data.Entity.Tests.Design.Renderer.Export
{
    /// <summary>
    ///     Writes a minimal but complete EDMX v3 file for tests that need a real file on disk.
    /// </summary>
    /// <remarks>
    ///     Two entities, one association, and a Diagrams section that places the shapes at known coordinates so tests
    ///     can tell stored layout apart from auto layout. The SSDL names a provider that will not be registered on a
    ///     build agent, which is deliberate - rendering must not depend on one.
    /// </remarks>
    internal static class TestEdmx
    {
        private const string Content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<edmx:Edmx Version=""3.0"" xmlns:edmx=""http://schemas.microsoft.com/ado/2009/11/edmx"">
  <edmx:Runtime>
    <edmx:StorageModels>
      <Schema Namespace=""TestModel.Store"" Provider=""Microsoft.Data.SqlClient"" ProviderManifestToken=""2012"" Alias=""Self"" xmlns=""http://schemas.microsoft.com/ado/2009/11/edm/ssdl"">
        <EntityType Name=""Customers"">
          <Key><PropertyRef Name=""Id"" /></Key>
          <Property Name=""Id"" Type=""int"" StoreGeneratedPattern=""Identity"" Nullable=""false"" />
          <Property Name=""Name"" Type=""nvarchar"" MaxLength=""100"" />
        </EntityType>
        <EntityType Name=""Orders"">
          <Key><PropertyRef Name=""Id"" /></Key>
          <Property Name=""Id"" Type=""int"" StoreGeneratedPattern=""Identity"" Nullable=""false"" />
          <Property Name=""CustomerId"" Type=""int"" Nullable=""false"" />
        </EntityType>
        <Association Name=""FK_Orders_Customers"">
          <End Role=""Customers"" Type=""Self.Customers"" Multiplicity=""1"" />
          <End Role=""Orders"" Type=""Self.Orders"" Multiplicity=""*"" />
          <ReferentialConstraint>
            <Principal Role=""Customers""><PropertyRef Name=""Id"" /></Principal>
            <Dependent Role=""Orders""><PropertyRef Name=""CustomerId"" /></Dependent>
          </ReferentialConstraint>
        </Association>
        <EntityContainer Name=""TestModelStoreContainer"">
          <EntitySet Name=""Customers"" EntityType=""Self.Customers"" Schema=""dbo"" store:Type=""Tables"" xmlns:store=""http://schemas.microsoft.com/ado/2007/12/edm/EntityStoreSchemaGenerator"" />
          <EntitySet Name=""Orders"" EntityType=""Self.Orders"" Schema=""dbo"" store:Type=""Tables"" xmlns:store=""http://schemas.microsoft.com/ado/2007/12/edm/EntityStoreSchemaGenerator"" />
          <AssociationSet Name=""FK_Orders_Customers"" Association=""Self.FK_Orders_Customers"">
            <End Role=""Customers"" EntitySet=""Customers"" />
            <End Role=""Orders"" EntitySet=""Orders"" />
          </AssociationSet>
        </EntityContainer>
      </Schema>
    </edmx:StorageModels>
    <edmx:ConceptualModels>
      <Schema Namespace=""TestModel"" Alias=""Self"" annotation:UseStrongSpatialTypes=""false"" xmlns:annotation=""http://schemas.microsoft.com/ado/2009/02/edm/annotation"" xmlns=""http://schemas.microsoft.com/ado/2009/11/edm"">
        <EntityType Name=""Customer"">
          <Key><PropertyRef Name=""Id"" /></Key>
          <Property Name=""Id"" Type=""Int32"" Nullable=""false"" annotation:StoreGeneratedPattern=""Identity"" />
          <Property Name=""Name"" Type=""String"" MaxLength=""100"" FixedLength=""false"" Unicode=""true"" />
          <NavigationProperty Name=""Orders"" Relationship=""Self.FK_Orders_Customers"" FromRole=""Customers"" ToRole=""Orders"" />
        </EntityType>
        <EntityType Name=""Order"">
          <Key><PropertyRef Name=""Id"" /></Key>
          <Property Name=""Id"" Type=""Int32"" Nullable=""false"" annotation:StoreGeneratedPattern=""Identity"" />
          <Property Name=""CustomerId"" Type=""Int32"" Nullable=""false"" />
          <NavigationProperty Name=""Customer"" Relationship=""Self.FK_Orders_Customers"" FromRole=""Orders"" ToRole=""Customers"" />
        </EntityType>
        <Association Name=""FK_Orders_Customers"">
          <End Role=""Customers"" Type=""Self.Customer"" Multiplicity=""1"" />
          <End Role=""Orders"" Type=""Self.Order"" Multiplicity=""*"" />
          <ReferentialConstraint>
            <Principal Role=""Customers""><PropertyRef Name=""Id"" /></Principal>
            <Dependent Role=""Orders""><PropertyRef Name=""CustomerId"" /></Dependent>
          </ReferentialConstraint>
        </Association>
        <EntityContainer Name=""TestModelContainer"" annotation:LazyLoadingEnabled=""true"">
          <EntitySet Name=""Customers"" EntityType=""Self.Customer"" />
          <EntitySet Name=""Orders"" EntityType=""Self.Order"" />
          <AssociationSet Name=""FK_Orders_Customers"" Association=""Self.FK_Orders_Customers"">
            <End Role=""Customers"" EntitySet=""Customers"" />
            <End Role=""Orders"" EntitySet=""Orders"" />
          </AssociationSet>
        </EntityContainer>
      </Schema>
    </edmx:ConceptualModels>
    <edmx:Mappings>
      <Mapping Space=""C-S"" xmlns=""http://schemas.microsoft.com/ado/2009/11/mapping/cs"">
        <EntityContainerMapping StorageEntityContainer=""TestModelStoreContainer"" CdmEntityContainer=""TestModelContainer"">
          <EntitySetMapping Name=""Customers"">
            <EntityTypeMapping TypeName=""IsTypeOf(TestModel.Customer)"">
              <MappingFragment StoreEntitySet=""Customers"">
                <ScalarProperty Name=""Id"" ColumnName=""Id"" />
                <ScalarProperty Name=""Name"" ColumnName=""Name"" />
              </MappingFragment>
            </EntityTypeMapping>
          </EntitySetMapping>
          <EntitySetMapping Name=""Orders"">
            <EntityTypeMapping TypeName=""IsTypeOf(TestModel.Order)"">
              <MappingFragment StoreEntitySet=""Orders"">
                <ScalarProperty Name=""Id"" ColumnName=""Id"" />
                <ScalarProperty Name=""CustomerId"" ColumnName=""CustomerId"" />
              </MappingFragment>
            </EntityTypeMapping>
          </EntitySetMapping>
        </EntityContainerMapping>
      </Mapping>
    </edmx:Mappings>
  </edmx:Runtime>
  <Designer xmlns=""http://schemas.microsoft.com/ado/2009/11/edmx"">
    <Connection>
      <DesignerInfoPropertySet>
        <DesignerProperty Name=""MetadataArtifactProcessing"" Value=""EmbedInOutputAssembly"" />
      </DesignerInfoPropertySet>
    </Connection>
    <Options>
      <DesignerInfoPropertySet>
        <DesignerProperty Name=""ValidateOnBuild"" Value=""true"" />
        <DesignerProperty Name=""EnablePluralization"" Value=""true"" />
        <DesignerProperty Name=""CodeGenerationStrategy"" Value=""None"" />
      </DesignerInfoPropertySet>
    </Options>
    <Diagrams>
      <Diagram DiagramId=""aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"" Name=""Diagram1"" ZoomLevel=""100"">
        <EntityTypeShape EntityType=""TestModel.Customer"" Width=""1.5"" PointX=""0.75"" PointY=""0.5"" IsExpanded=""true"" />
        <EntityTypeShape EntityType=""TestModel.Order"" Width=""1.5"" PointX=""3.5"" PointY=""2.25"" IsExpanded=""true"" />
        <AssociationConnector Association=""TestModel.FK_Orders_Customers"" ManuallyRouted=""false"" />
      </Diagram>
    </Diagrams>
  </Designer>
</edmx:Edmx>";

        /// <summary>
        ///     Writes the test model to a temporary .edmx file.
        /// </summary>
        /// <returns>The full path of the file. The caller is responsible for deleting it.</returns>
        internal static string Write()
        {
            var path = Path.Combine(Path.GetTempPath(), $"EdmxRenderTest_{Path.GetRandomFileName()}.edmx");
            File.WriteAllText(path, Content);

            return path;
        }
    }
}
