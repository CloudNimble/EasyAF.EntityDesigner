// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb
{
    public partial class StoreModelBuilderTests
    {
        [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
        public void CreateEntityType_creates_entity_for_valid_properties()
        {
            //var columns =
            //    new List<TableDetailsRow>
            //        {
            //            CreateRow(
            //                table: "table", columnName: "Id", dataType: "int", isPrimaryKey: true,
            //                isNullable: false),
            //            CreateRow(table: "table", columnName: "Name", dataType: "nvarchar(max)")
            //        };

            //bool needsDefiningQuery;
            //var entity = CreateStoreModelBuilder()
            //    .CreateEntityType(columns, out needsDefiningQuery);

            //entity.FullName.Should().Be("myModel.table");
            //new[] { "Id" }.SequenceEqual(entity.KeyMembers.Select(m => m.Name)).Should().BeTrue();
            //new[] { "Id", "Name" }.SequenceEqual(entity.Members.Select(m => m.Name)).Should().BeTrue();
            //needsDefiningQuery.Should().BeFalse();
            //entity.MetadataProperties.Any(p => p.Name == "EdmSchemaErrors").Should().BeFalse();
            //MetadataItemHelper.IsInvalid(entity).Should().BeFalse();
        }

        [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
        public void CreateEntityType_defining_query_not_needed_for_tables_where_all_columns_are_key_columns()
        {
            //var columns =
            //    new List<TableDetailsRow>
            //        {
            //            CreateRow(table: "table", columnName: "Id", dataType: "int", isPrimaryKey: true),
            //            CreateRow(table: "table", columnName: "Name", dataType: "nvarchar(max)", isPrimaryKey: true),
            //        };

            //bool needsDefiningQuery;
            //var entity = CreateStoreModelBuilder()
            //    .CreateEntityType(columns, out needsDefiningQuery);

            //entity.FullName.Should().Be("myModel.table");
            //new[] { "Id", "Name" }.SequenceEqual(entity.KeyMembers.Select(k => k.Name)).Should().BeTrue();
            //new[] { "Id", "Name" }.SequenceEqual(entity.Members.Select(m => m.Name)).Should().BeTrue();
            //needsDefiningQuery.Should().BeFalse();
            //MetadataItemHelper.IsInvalid(entity).Should().BeFalse();

            //var edmSchemaErrors =
            //    (IList<EdmSchemaError>)entity.MetadataProperties.Single(p => p.Name == "EdmSchemaErrors").Value;
            //edmSchemaErrors.Count().Should().Be(2);

            //edmSchemaErrors[0].Message.Should().Be(
            //    string.Format(
            //        CultureInfo.InvariantCulture,
            //        Resources_VersioningFacade.CoercingNullablePrimaryKeyPropertyToNonNullable,
            //        "Id",
            //        "table"));
            //edmSchemaErrors[0].Severity.Should().Be(EdmSchemaErrorSeverity.Warning);

            //edmSchemaErrors[1].Message.Should().Be(
            //    string.Format(
            //        CultureInfo.InvariantCulture,
            //        Resources_VersioningFacade.CoercingNullablePrimaryKeyPropertyToNonNullable,
            //        "Name",
            //        "table"));
            //edmSchemaErrors[1].Severity.Should().Be(EdmSchemaErrorSeverity.Warning);
        }

        [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
        public void CreateEntityType_creates_readonly_entity_if_some_column_keys_are_excluded()
        {
            //var columns =
            //    new List<TableDetailsRow>
            //        {
            //            CreateRow(
            //                schema: "dbo", table: "table", columnName: "Id", dataType: "int",
            //                isPrimaryKey: true),
            //            CreateRow(
            //                schema: "dbo", table: "table", columnName: "Id1", dataType: "invalid-type",
            //                isPrimaryKey: true),
            //            CreateRow(
            //                schema: "dbo", table: "table", columnName: "Id2", dataType: "invalid-type",
            //                isPrimaryKey: true),
            //            CreateRow(
            //                schema: "dbo", table: "table", columnName: "Name", dataType: "nvarchar(max)",
            //                isPrimaryKey: false)
            //        };

            //bool needsDefiningQuery;
            //var entity = CreateStoreModelBuilder()
            //    .CreateEntityType(columns, out needsDefiningQuery);

            //entity.FullName.Should().Be("myModel.table");
            //new[] { "Id" }.SequenceEqual(entity.KeyMembers.Select(m => m.Name)).Should().BeTrue();
            //new[] { "Id", "Name" }.SequenceEqual(entity.Members.Select(m => m.Name)).Should().BeTrue();
            //needsDefiningQuery.Should().BeTrue();
            //MetadataItemHelper.IsInvalid(entity).Should().BeFalse();

            //var edmSchemaErrors =
            //    ((IList<EdmSchemaError>)(entity.MetadataProperties.Single(p => p.Name == "EdmSchemaErrors").Value))
            //        .Where(e => e.ErrorCode == 6031 && e.Severity == EdmSchemaErrorSeverity.Warning).ToArray();

            //edmSchemaErrors.Length.Should().Be(2);

            //edmSchemaErrors[0].Message.Should().Be(
            //    string.Format(
            //        CultureInfo.InvariantCulture,
            //        Resources_VersioningFacade.ExcludedColumnWasAKeyColumnEntityIsReadOnly,
            //        "Id1",
            //        "dbo.table"));

            //edmSchemaErrors[1].Message.Should().Be(
            //    string.Format(
            //        CultureInfo.InvariantCulture,
            //        Resources_VersioningFacade.ExcludedColumnWasAKeyColumnEntityIsReadOnly,
            //        "Id2",
            //        "dbo.table"));
        }

        [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
        public void CreateEntityType_creates_invalid_entity_if_all_column_keys_are_excluded()
        {
            //var columns =
            //    new List<TableDetailsRow>
            //        {
            //            CreateRow(
            //                schema: "dbo", table: "table", columnName: "Id1", dataType: "invalid-type",
            //                isPrimaryKey: true),
            //            CreateRow(
            //                schema: "dbo", table: "table", columnName: "Id2", dataType: "invalid-type",
            //                isPrimaryKey: true),
            //            CreateRow(
            //                schema: "dbo", table: "table", columnName: "Name", dataType: "nvarchar(max)",
            //                isPrimaryKey: false)
            //        };

            //bool needsDefiningQuery;
            //var entity = CreateStoreModelBuilder()
            //    .CreateEntityType(columns, out needsDefiningQuery);

            //entity.FullName.Should().Be("myModel.table");
            //entity.KeyMembers.Should().BeEmpty();
            //new[] { "Name" }.SequenceEqual(entity.Members.Select(m => m.Name)).Should().BeTrue();
            //needsDefiningQuery.Should().BeFalse();
            //MetadataItemHelper.IsInvalid(entity).Should().BeTrue();

            //var edmSchemaErrors =
            //    ((IList<EdmSchemaError>)(entity.MetadataProperties.Single(p => p.Name == "EdmSchemaErrors").Value))
            //        .Where(e => e.ErrorCode == 6031 && e.Severity == EdmSchemaErrorSeverity.Warning).ToArray();

            //edmSchemaErrors.Length.Should().Be(2);

            //edmSchemaErrors[0].Message.Should().Be(
            //    string.Format(
            //        CultureInfo.InvariantCulture,
            //        Resources_VersioningFacade.ExcludedColumnWasAKeyColumnEntityIsInvalid,
            //        "Id1",
            //        "dbo.table"));

            //edmSchemaErrors[1].Message.Should().Be(
            //    string.Format(
            //        CultureInfo.InvariantCulture,
            //        Resources_VersioningFacade.ExcludedColumnWasAKeyColumnEntityIsInvalid,
            //        "Id2",
            //        "dbo.table"));
        }

        [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
        public void CreateEntityType_creates_readonly_entity_if_after_excluding_invalid_key_columns_only_valid_key_columns_of_invalid_types_exist()
        {
            //var columns =
            //    new List<TableDetailsRow>
            //        {
            //            CreateRow(
            //                schema: "dbo", table: "table", columnName: "Id1", dataType: "invalid-type",
            //                isPrimaryKey: true),
            //            CreateRow(
            //                schema: "dbo", table: "table", columnName: "Id2", dataType: "geography",
            //                isPrimaryKey: true, isNullable: false),
            //            CreateRow(
            //                schema: "dbo", table: "table", columnName: "Id3", dataType: "int",
            //                isPrimaryKey: true, isNullable: false),
            //            CreateRow(
            //                schema: "dbo", table: "table", columnName: "Name", dataType: "nvarchar(max)",
            //                isPrimaryKey: false)
            //        };

            //bool needsDefiningQuery;
            //var entity = CreateStoreModelBuilder()
            //    .CreateEntityType(columns, out needsDefiningQuery);

            //entity.FullName.Should().Be("myModel.table");
            //new[] { "Id3" }.SequenceEqual(entity.KeyMembers.Select(m => m.Name)).Should().BeTrue();
            //new[] { "Id2", "Id3", "Name" }.SequenceEqual(entity.Members.Select(m => m.Name)).Should().BeTrue();
            //needsDefiningQuery.Should().BeTrue();
            //MetadataItemHelper.IsInvalid(entity).Should().BeFalse();

            //var edmSchemaErrors =
            //    ((IList<EdmSchemaError>)(entity.MetadataProperties.Single(p => p.Name == "EdmSchemaErrors").Value))
            //        .Skip(1).ToArray();

            //edmSchemaErrors.Length.Should().Be(2);

            //edmSchemaErrors[0].Message.Should().Be(
            //    string.Format(
            //        CultureInfo.InvariantCulture,
            //        Resources_VersioningFacade.ExcludedColumnWasAKeyColumnEntityIsReadOnly,
            //        "Id1",
            //        "dbo.table"));
            //edmSchemaErrors[0].Severity.Should().Be(EdmSchemaErrorSeverity.Warning);

            //edmSchemaErrors[1].Message.Should().Be(
            //    string.Format(
            //        CultureInfo.InvariantCulture,
            //        Resources_VersioningFacade.InvalidTypeForPrimaryKey,
            //        "dbo.table",
            //        "Id2",
            //        "geography"));
            //edmSchemaErrors[1].Severity.Should().Be(EdmSchemaErrorSeverity.Warning);
        }

        [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
        public void CreateEntityType_creates_invalid_entity_if_after_excluding_invalid_key_columns_only_key_columns_of_invalid_types_exist()
        {
            //var columns =
            //    new List<TableDetailsRow>
            //        {
            //            CreateRow(
            //                schema: "dbo", table: "table", columnName: "Id1", dataType: "invalid-type",
            //                isPrimaryKey: true),
            //            CreateRow(
            //                schema: "dbo", table: "table", columnName: "Id2", dataType: "geography",
            //                isPrimaryKey: true),
            //            CreateRow(
            //                schema: "dbo", table: "table", columnName: "Name", dataType: "nvarchar(max)",
            //                isPrimaryKey: false)
            //        };

            //bool needsDefiningQuery;
            //var entity = CreateStoreModelBuilder()
            //    .CreateEntityType(columns, out needsDefiningQuery);

            //entity.FullName.Should().Be("myModel.table");
            //entity.KeyMembers.Should().BeEmpty();
            //new[] { "Id2", "Name" }.SequenceEqual(entity.Members.Select(m => m.Name)).Should().BeTrue();
            //needsDefiningQuery.Should().BeFalse();
            //MetadataItemHelper.IsInvalid(entity).Should().BeTrue();

            //var edmSchemaErrors =
            //    ((IList<EdmSchemaError>)(entity.MetadataProperties.Single(p => p.Name == "EdmSchemaErrors").Value))
            //        .Skip(1).ToArray();

            //edmSchemaErrors.Length.Should().Be(3);

            //edmSchemaErrors[0].Message.Should().Be(
            //    string.Format(
            //        CultureInfo.InvariantCulture,
            //        Resources_VersioningFacade.CoercingNullablePrimaryKeyPropertyToNonNullable,
            //        "Id2",
            //        "table"));
            //edmSchemaErrors[0].Severity.Should().Be(EdmSchemaErrorSeverity.Warning);

            //edmSchemaErrors[1].Message.Should().Be(
            //    string.Format(
            //        CultureInfo.InvariantCulture,
            //        Resources_VersioningFacade.ExcludedColumnWasAKeyColumnEntityIsInvalid,
            //        "Id1",
            //        "dbo.table"));
            //edmSchemaErrors[1].Severity.Should().Be(EdmSchemaErrorSeverity.Warning);

            //edmSchemaErrors[2].Message.Should().Be(
            //    string.Format(
            //        CultureInfo.InvariantCulture,
            //        Resources_VersioningFacade.InvalidTypeForPrimaryKey,
            //        "dbo.table",
            //        "Id2",
            //        "geography"));
            //edmSchemaErrors[2].Severity.Should().Be(EdmSchemaErrorSeverity.Warning);
        }

        [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
        public void CreateEntityType_creates_readonly_entity_if_no_keys_defined_but_keys_can_be_inferred()
        {
            //var columns =
            //    new List<TableDetailsRow>
            //        {
            //            CreateRow(
            //                schema: "dbo", table: "table", columnName: "Id", dataType: "int",
            //                isNullable: false),
            //            CreateRow(
            //                schema: "dbo", table: "table", columnName: "Name", dataType: "nvarchar(max)",
            //                isNullable: false)
            //        };

            //bool needsDefiningQuery;
            //var entity = CreateStoreModelBuilder()
            //    .CreateEntityType(columns, out needsDefiningQuery);

            //entity.FullName.Should().Be("myModel.table");
            //new[] { "Id", "Name" }.SequenceEqual(entity.KeyMembers.Select(m => m.Name)).Should().BeTrue();
            //new[] { "Id", "Name" }.SequenceEqual(entity.Members.Select(m => m.Name)).Should().BeTrue();
            //needsDefiningQuery.Should().BeTrue();
            //MetadataItemHelper.IsInvalid(entity).Should().BeFalse();

            //var edmSchemaErrors =
            //    (IList<EdmSchemaError>)entity.MetadataProperties.Single(p => p.Name == "EdmSchemaErrors").Value;

            //edmSchemaErrors.Count.Should().Be(1);
            //edmSchemaErrors[0].ErrorCode.Should().Be(6002);
            //edmSchemaErrors[0].Severity.Should().Be(EdmSchemaErrorSeverity.Warning);
            //edmSchemaErrors[0].Message.Should().Be(
            //    string.Format(
            //        CultureInfo.InvariantCulture,
            //        Resources_VersioningFacade.NoPrimaryKeyDefined,
            //        "dbo.table"));
        }

        [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
        public void CreateEntityType_creates_invalid_entity_if_no_keys_defined_and_keys_cannot_be_inferred()
        {
            //var columns =
            //    new List<TableDetailsRow>
            //        {
            //            CreateRow(schema: "dbo", table: "table", columnName: "Id", dataType: "int", isNullable: true),
            //            CreateRow(
            //                schema: "dbo", table: "table", columnName: "Name", dataType: "geography",
            //                isNullable: false)
            //        };

            //bool needsDefiningQuery;
            //var entity = CreateStoreModelBuilder()
            //    .CreateEntityType(columns, out needsDefiningQuery);

            //entity.FullName.Should().Be("myModel.table");
            //entity.KeyMembers.Should().BeEmpty();
            //new[] { "Id", "Name" }.SequenceEqual(entity.Members.Select(m => m.Name)).Should().BeTrue();
            //needsDefiningQuery.Should().BeFalse();
            //MetadataItemHelper.IsInvalid(entity).Should().BeTrue();

            //var edmSchemaErrors =
            //    (IList<EdmSchemaError>)entity.MetadataProperties.Single(p => p.Name == "EdmSchemaErrors").Value;

            //edmSchemaErrors.Count.Should().Be(1);
            //edmSchemaErrors[0].ErrorCode.Should().Be(6013);
            //edmSchemaErrors[0].Severity.Should().Be(EdmSchemaErrorSeverity.Warning);
            //edmSchemaErrors[0].Message.Should().Be(
            //    string.Format(
            //        CultureInfo.InvariantCulture,
            //        Resources_VersioningFacade.CannotCreateEntityWithNoPrimaryKeyDefined,
            //        "dbo.table"));
        }

        [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
        public void CreateEntityType_creates_readonly_entity_if_defined_keys_have_invalid_key_type_but_keys_can_be_inferred()
        {
            //var columns =
            //    new List<TableDetailsRow>
            //        {
            //            CreateRow(
            //                schema: "dbo", table: "table", columnName: "Id", dataType: "varbinary",
            //                isPrimaryKey: true, isNullable: false),
            //            CreateRow(
            //                schema: "dbo", table: "table", columnName: "Name", dataType: "int",
            //                isNullable: false)
            //        };

            //bool needsDefiningQuery;
            //var entity = CreateStoreModelBuilder(targetEntityFrameworkVersion: EntityFrameworkVersion.Version1)
            //    .CreateEntityType(columns, out needsDefiningQuery);

            //entity.FullName.Should().Be("myModel.table");
            //new[] { "Name" }.SequenceEqual(entity.KeyMembers.Select(m => m.Name)).Should().BeTrue();
            //new[] { "Id", "Name" }.SequenceEqual(entity.Members.Select(m => m.Name)).Should().BeTrue();
            //needsDefiningQuery.Should().BeTrue();
            //MetadataItemHelper.IsInvalid(entity).Should().BeFalse();

            //var edmSchemaErrors =
            //    (IList<EdmSchemaError>)entity.MetadataProperties.Single(p => p.Name == "EdmSchemaErrors").Value;

            //edmSchemaErrors.Count.Should().Be(2);

            //edmSchemaErrors[0].ErrorCode.Should().Be(6032);
            //edmSchemaErrors[0].Severity.Should().Be(EdmSchemaErrorSeverity.Warning);
            //edmSchemaErrors[0].Message.Should().Be(
            //    string.Format(
            //        CultureInfo.InvariantCulture,
            //        Resources_VersioningFacade.InvalidTypeForPrimaryKey,
            //        "dbo.table",
            //        "Id",
            //        "varbinary"));

            //edmSchemaErrors[1].ErrorCode.Should().Be(6002);
            //edmSchemaErrors[1].Severity.Should().Be(EdmSchemaErrorSeverity.Warning);
            //edmSchemaErrors[1].Message.Should().Be(
            //    string.Format(
            //        CultureInfo.InvariantCulture,
            //        Resources_VersioningFacade.NoPrimaryKeyDefined,
            //        "dbo.table"));
        }

        [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
        public void CreateEntityType_creates_invalid_entity_if_defined_keys_have_invalid_key_type_and_keys_cannot_be_inferred()
        {
            //var columns =
            //    new List<TableDetailsRow>
            //        {
            //            CreateRow(
            //                schema: "dbo", table: "table", columnName: "Id", dataType: "geometry",
            //                isPrimaryKey: true, isNullable: false),
            //            CreateRow(
            //                schema: "dbo", table: "table", columnName: "Name", dataType: "geography",
            //                isNullable: false)
            //        };

            //bool needsDefiningQuery;
            //var entity = CreateStoreModelBuilder()
            //    .CreateEntityType(columns, out needsDefiningQuery);

            //entity.FullName.Should().Be("myModel.table");
            //entity.KeyMembers.Should().BeEmpty();
            //new[] { "Id", "Name" }.SequenceEqual(entity.Members.Select(m => m.Name)).Should().BeTrue();
            //needsDefiningQuery.Should().BeFalse();
            //MetadataItemHelper.IsInvalid(entity).Should().BeTrue();

            //var edmSchemaErrors =
            //    (IList<EdmSchemaError>)entity.MetadataProperties.Single(p => p.Name == "EdmSchemaErrors").Value;

            //edmSchemaErrors.Count.Should().Be(2);

            //edmSchemaErrors[0].ErrorCode.Should().Be(6032);
            //edmSchemaErrors[0].Severity.Should().Be(EdmSchemaErrorSeverity.Warning);
            //edmSchemaErrors[0].Message.Should().Be(
            //    string.Format(
            //        CultureInfo.InvariantCulture,
            //        Resources_VersioningFacade.InvalidTypeForPrimaryKey,
            //        "dbo.table",
            //        "Id",
            //        "geometry"));

            //edmSchemaErrors[1].ErrorCode.Should().Be(6013);
            //edmSchemaErrors[1].Severity.Should().Be(EdmSchemaErrorSeverity.Warning);
            //edmSchemaErrors[1].Message.Should().Be(
            //    string.Format(
            //        CultureInfo.InvariantCulture,
            //        Resources_VersioningFacade.CannotCreateEntityWithNoPrimaryKeyDefined,
            //        "dbo.table"));
        }
    }
}
