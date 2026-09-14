// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Data.Entity.Design.DatabaseGeneration;
using Microsoft.Data.Entity.Design.DatabaseGeneration.OutputGenerators;
using Microsoft.Data.Entity.Design.DatabaseGeneration.Properties;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;

namespace Microsoft.Data.Entity.Tests.Design.DatabaseGeneration
{
    [TestClass]
    public class SsdlToDdlTests
    {
        private const string AssociationSsdl =
            "<Schema Namespace='Model.Store' Provider='System.Data.SqlClient' ProviderManifestToken='2008' xmlns='http://schemas.microsoft.com/ado/2009/11/edm/ssdl'>"
            + "  <EntityContainer Name='StoreContainer'>"
            + "    <EntitySet Name='Parents' EntityType='Model.Store.Parents' Schema='dbo' />"
            + "    <EntitySet Name='Children' EntityType='Model.Store.Children' Schema='dbo' />"
            + "    <AssociationSet Name='FK_Children_Parents' Association='Model.Store.FK_Children_Parents'>"
            + "      <End Role='Parents' EntitySet='Parents' />"
            + "      <End Role='Children' EntitySet='Children' />"
            + "    </AssociationSet>"
            + "  </EntityContainer>"
            + "  <EntityType Name='Parents'>"
            + "    <Key><PropertyRef Name='Id' /></Key>"
            + "    <Property Name='Id' Type='int' StoreGeneratedPattern='Identity' Nullable='false' />"
            + "  </EntityType>"
            + "  <EntityType Name='Children'>"
            + "    <Key><PropertyRef Name='Id' /></Key>"
            + "    <Property Name='Id' Type='int' StoreGeneratedPattern='Identity' Nullable='false' />"
            + "    <Property Name='ParentId' Type='int' Nullable='false' />"
            + "  </EntityType>"
            + "  <Association Name='FK_Children_Parents'>"
            + "    <End Role='Parents' Type='Model.Store.Parents' Multiplicity='1' />"
            + "    <End Role='Children' Type='Model.Store.Children' Multiplicity='*' />"
            + "    <ReferentialConstraint>"
            + "      <Principal Role='Parents'><PropertyRef Name='Id' /></Principal>"
            + "      <Dependent Role='Children'><PropertyRef Name='ParentId' /></Dependent>"
            + "    </ReferentialConstraint>"
            + "  </Association>"
            + "</Schema>";

        private const string IdentifyingFkSsdl =
            "<Schema Namespace='Model.Store' Provider='System.Data.SqlClient' ProviderManifestToken='2008' xmlns='http://schemas.microsoft.com/ado/2009/11/edm/ssdl'>"
            + "  <EntityContainer Name='StoreContainer'>"
            + "    <EntitySet Name='Parents' EntityType='Model.Store.Parents' Schema='dbo' />"
            + "    <EntitySet Name='Children' EntityType='Model.Store.Children' Schema='dbo' />"
            + "    <AssociationSet Name='FK_Children_Parents' Association='Model.Store.FK_Children_Parents'>"
            + "      <End Role='Parents' EntitySet='Parents' />"
            + "      <End Role='Children' EntitySet='Children' />"
            + "    </AssociationSet>"
            + "  </EntityContainer>"
            + "  <EntityType Name='Parents'>"
            + "    <Key><PropertyRef Name='Id' /></Key>"
            + "    <Property Name='Id' Type='int' StoreGeneratedPattern='Identity' Nullable='false' />"
            + "  </EntityType>"
            + "  <EntityType Name='Children'>"
            + "    <Key><PropertyRef Name='ParentId' /></Key>"
            + "    <Property Name='ParentId' Type='int' Nullable='false' />"
            + "    <Property Name='Name' Type='nvarchar' MaxLength='50' Nullable='false' />"
            + "  </EntityType>"
            + "  <Association Name='FK_Children_Parents'>"
            + "    <End Role='Parents' Type='Model.Store.Parents' Multiplicity='1' />"
            + "    <End Role='Children' Type='Model.Store.Children' Multiplicity='0..1' />"
            + "    <ReferentialConstraint>"
            + "      <Principal Role='Parents'><PropertyRef Name='Id' /></Principal>"
            + "      <Dependent Role='Children'><PropertyRef Name='ParentId' /></Dependent>"
            + "    </ReferentialConstraint>"
            + "  </Association>"
            + "</Schema>";

        private const string SimpleSsdl =
            "<Schema Namespace='AdventureWorksModel.Store' Provider='System.Data.SqlClient' ProviderManifestToken='2008' xmlns='http://schemas.microsoft.com/ado/2009/11/edm/ssdl'>"
            + "  <EntityContainer Name='AdventureWorksModelStoreContainer'>"
            + "    <EntitySet Name='Entities' EntityType='AdventureWorksModel.Store.Entities' Schema='dbo' />"
            + "  </EntityContainer>"
            + "  <EntityType Name='Entities'>"
            + "    <Key>"
            + "      <PropertyRef Name='Id' />"
            + "    </Key>"
            + "    <Property Name='Id' Type='int' StoreGeneratedPattern='Identity' Nullable='false' />"
            + "    <Property Name='Name' Type='nvarchar(max)' Nullable='false' />"
            + "  </EntityType>"
            + "</Schema>";

        private static readonly DateTime GeneratedAt = new DateTime(2026, 9, 14, 12, 0, 0);

        [TestMethod]
        public void Generate_creates_sql_server_script_for_simple_ssdl()
        {
            var ddl = new SsdlToDdl(GeneratedAt).Generate(SimpleSsdl, string.Empty, CreateBag());

            ddl.Should().Contain("-- Entity Designer DDL Script for SQL Server 2005, 2008, 2012 and Azure");
            ddl.Should().Contain("-- Date Created: " + GeneratedAt.ToString());
            ddl.Should().Contain("SET QUOTED_IDENTIFIER OFF;");
            ddl.Should().Contain("USE [Northwind];");
            ddl.Should().Contain("IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');");
            ddl.Should().Contain("CREATE TABLE [dbo].[Entities] (");
            ddl.Should().Contain("[Id] int IDENTITY(1,1) NOT NULL");
            ddl.Should().Contain("ADD CONSTRAINT [PK_Entities]");
            ddl.Should().Contain("PRIMARY KEY CLUSTERED ([Id] ASC);");
            ddl.Should().Contain("-- Script has ended");
            ddl.Should().NotContain("-- Warning: There were errors validating the existing SSDL");
        }

        [TestMethod]
        public void Generate_creates_sqlce_script_without_schema_or_use_database()
        {
            var bag = CreateBag(providerInvariantName: "System.Data.SqlServerCe.4.0", databaseName: "Northwind");

            var ddl = new SsdlToDdl(GeneratedAt).Generate(SimpleSsdl, string.Empty, bag);

            ddl.Should().Contain("-- Entity Designer DDL Script for SQL Server Compact Edition");
            ddl.Should().NotContain("SET QUOTED_IDENTIFIER");
            ddl.Should().NotContain("USE [");
            ddl.Should().NotContain("CREATE SCHEMA");
            ddl.Should().Contain("CREATE TABLE [Entities] (");
            ddl.Should().Contain("PRIMARY KEY ([Id] );");
            ddl.Should().NotContain("CLUSTERED");
        }

        [TestMethod]
        public void Generate_creates_foreign_key_and_index_when_fk_is_not_part_of_pk()
        {
            var ddl = new SsdlToDdl(GeneratedAt).Generate(AssociationSsdl, string.Empty, CreateBag());

            ddl.Should().Contain("ADD CONSTRAINT [FK_Children_Parents]");
            ddl.Should().Contain("FOREIGN KEY ([ParentId])");
            ddl.Should().Contain("REFERENCES [dbo].[Parents]");
            ddl.Should().Contain("([Id])");
            ddl.Should().Contain("ON DELETE NO ACTION ON UPDATE NO ACTION;");
            ddl.Should().Contain("CREATE INDEX [IX_FK_Children_Parents]");
            ddl.Should().Contain("ON [dbo].[Children]");
        }

        [TestMethod]
        public void Generate_does_not_create_index_when_fk_is_the_primary_key()
        {
            var ddl = new SsdlToDdl(GeneratedAt).Generate(IdentifyingFkSsdl, string.Empty, CreateBag());

            ddl.Should().Contain("ADD CONSTRAINT [FK_Children_Parents]");
            ddl.Should().NotContain("CREATE INDEX [IX_FK_Children_Parents]");
        }

        [TestMethod]
        public void Generate_drops_existing_tables_and_foreign_keys()
        {
            var ddl = new SsdlToDdl(GeneratedAt).Generate(AssociationSsdl, AssociationSsdl, CreateBag());

            ddl.Should().Contain("IF OBJECT_ID(N'[dbo].[FK_Children_Parents]', 'F') IS NOT NULL");
            ddl.Should().Contain("ALTER TABLE [dbo].[Children] DROP CONSTRAINT [FK_Children_Parents];");
            ddl.Should().Contain("IF OBJECT_ID(N'[dbo].[Parents]', 'U') IS NOT NULL");
            ddl.Should().Contain("DROP TABLE [dbo].[Parents];");
            ddl.Should().Contain("IF OBJECT_ID(N'[dbo].[Children]', 'U') IS NOT NULL");
            ddl.Should().Contain("DROP TABLE [dbo].[Children];");
        }

        [TestMethod]
        public void Generate_includes_edmx_path_in_header_when_supplied()
        {
            var bag = CreateBag(edmxPath: @"C:\Project\Model.edmx");

            var ddl = new SsdlToDdl(GeneratedAt).Generate(SimpleSsdl, string.Empty, bag);

            ddl.Should().Contain("-- Generated from EDMX file: C:\\Project\\Model.edmx");
        }

        [TestMethod]
        public void Generate_skips_drops_and_warns_when_existing_ssdl_is_invalid()
        {
            const string invalidExisting = "<Schema xmlns='http://schemas.microsoft.com/ado/2009/11/edm/ssdl'></Schema>";

            var ddl = new SsdlToDdl(GeneratedAt).Generate(SimpleSsdl, invalidExisting, CreateBag());

            ddl.Should().Contain("-- Warning: There were errors validating the existing SSDL. Drop statements");
            ddl.Should().Contain("-- will not be generated.");
            ddl.Should().NotContain("DROP TABLE");
            ddl.Should().Contain("CREATE TABLE [dbo].[Entities] (");
        }

        [TestMethod]
        public void Generate_throws_when_parameter_bag_is_null()
        {
            Action act = () => new SsdlToDdl().Generate(SimpleSsdl, string.Empty, null);

            act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("edmParameterBag");
        }

        [TestMethod]
        public void Generate_throws_when_provider_is_missing()
        {
            var bag = CreateBag(providerInvariantName: " ");

            Action act = () => new SsdlToDdl().Generate(SimpleSsdl, string.Empty, bag);

            act.Should().Throw<InvalidOperationException>().Which.Message.Should().Be(
                string.Format(
                    CultureInfo.CurrentCulture,
                    DatabaseGenerationResources.ErrorNoParameterDefined,
                    EdmParameterBag.ParameterName.ProviderInvariantName));
        }

        [TestMethod]
        public void Generate_throws_when_schema_name_is_missing()
        {
            var bag = CreateBag(databaseSchemaName: string.Empty);

            Action act = () => new SsdlToDdl().Generate(SimpleSsdl, string.Empty, bag);

            act.Should().Throw<InvalidOperationException>().Which.Message.Should().Be(
                string.Format(
                    CultureInfo.CurrentCulture,
                    DatabaseGenerationResources.ErrorNoParameterDefined,
                    EdmParameterBag.ParameterName.DatabaseSchemaName));
        }

        [TestMethod]
        public void Generate_throws_when_target_version_is_invalid()
        {
            var bag = CreateBag(targetVersion: new Version(1, 0, 0, 0));

            Action act = () => new SsdlToDdl().Generate(SimpleSsdl, string.Empty, bag);

            act.Should().Throw<InvalidOperationException>().Which.Message.Should().Be(
                string.Format(
                    CultureInfo.CurrentCulture,
                    DatabaseGenerationResources.ErrorNonValidTargetVersion,
                    new Version(1, 0, 0, 0)));
        }

        [TestMethod]
        public void Generate_throws_when_target_version_is_missing()
        {
            var bag = new EdmParameterBag(
                null,
                null,
                null,
                "System.Data.SqlClient",
                "2008",
                null,
                "dbo",
                "Northwind",
                null,
                null);

            Action act = () => new SsdlToDdl().Generate(SimpleSsdl, string.Empty, bag);

            act.Should().Throw<InvalidOperationException>().Which.Message.Should().Be(
                string.Format(
                    CultureInfo.CurrentCulture,
                    DatabaseGenerationResources.ErrorNoParameterDefined,
                    EdmParameterBag.ParameterName.TargetVersion));
        }

        [TestMethod]
        public void DatabaseScriptGenerator_parameterless_constructor_uses_ssdl_to_ddl()
        {
            const string csdl =
                "<Schema Namespace='AdventureWorksModel' Alias='Self' xmlns:annotation='http://schemas.microsoft.com/ado/2009/02/edm/annotation' xmlns='http://schemas.microsoft.com/ado/2009/11/edm'>"
                + "   <EntityContainer Name='AdventureWorksEntities'>"
                + "       <EntitySet Name='Entities' EntityType='AdventureWorksModel.Entity' />"
                + "   </EntityContainer>"
                + "   <EntityType Name='Entity'>"
                + "       <Key>"
                + "           <PropertyRef Name='Id' />"
                + "       </Key>"
                + "       <Property Type='Int32' Name='Id' Nullable='false' annotation:StoreGeneratedPattern='Identity' />"
                + "       <Property Type='String' Name='Name' Nullable='false' />"
                + "   </EntityType>"
                + "</Schema>";

            var edm = EdmExtension.CreateAndValidateEdmItemCollection(csdl, new Version(3, 0, 0, 0));

            var script = new DatabaseScriptGenerator().Generate(edm, string.Empty, CreateBag(providerManifestToken: "2008"));

            script.Ddl.Should().NotBeNullOrWhiteSpace();
            script.Ddl.Should().Contain("CREATE TABLE");
            script.Ssdl.Should().NotBeNullOrWhiteSpace();
            script.Msl.Should().NotBeNullOrWhiteSpace();
        }

        private static EdmParameterBag CreateBag(
            string providerInvariantName = "System.Data.SqlClient",
            string databaseSchemaName = "dbo",
            string databaseName = "Northwind",
            string edmxPath = null,
            string providerManifestToken = "2008",
            Version targetVersion = null)
        {
            return new EdmParameterBag(
                null,
                null,
                targetVersion ?? new Version(3, 0, 0, 0),
                providerInvariantName,
                providerManifestToken,
                null,
                databaseSchemaName,
                databaseName,
                null,
                edmxPath);
        }
    }
}
