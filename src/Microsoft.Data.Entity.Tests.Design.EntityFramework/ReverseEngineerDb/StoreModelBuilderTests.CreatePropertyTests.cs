// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Globalization;
using System.Linq;
using Microsoft.Data.Entity.Design.VersioningFacade;
using Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb.SchemaDiscovery;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb
{
    public partial class StoreModelBuilderTests
    {
        [TestMethod]
        public void CreateProperty_creates_default_property_for_Int32_store_type()
        {
            List<EdmSchemaError> errors = new List<EdmSchemaError>();
            var property =
                CreateStoreModelBuilder()
                    .CreateProperty(
                        CreateRow(columnName: "IntColumn", dataType: "int"),
                        errors);

            property.Should().NotBeNull();
            property.Name.Should().Be("IntColumn");
            property.PrimitiveType.PrimitiveTypeKind.Should().Be(PrimitiveTypeKind.Int32);
            property.StoreGeneratedPattern.Should().Be(StoreGeneratedPattern.None);
            errors.Should().BeEmpty();
        }

        [TestMethod]
        public void CreateProperty_respects_IsNullable_column()
        {
            foreach (var isNullable in new[] { true, false })
            {
                List<EdmSchemaError> errors = new List<EdmSchemaError>();
                var property =
                    CreateStoreModelBuilder()
                        .CreateProperty(
                            CreateRow(columnName: "IntColumn", dataType: "int", isNullable: isNullable),
                            errors);

                property.Should().NotBeNull();
                property.Name.Should().Be("IntColumn");
                property.PrimitiveType.PrimitiveTypeKind.Should().Be(PrimitiveTypeKind.Int32);
                property.Nullable.Should().Be(isNullable);
            }
        }

        [TestMethod]
        public void CreateProperty_returns_error_for_null_property_type()
        {
            List<EdmSchemaError> errors = new List<EdmSchemaError>();
            var property =
                CreateStoreModelBuilder()
                    .CreateProperty(
                        CreateRow("catalog", "schema", "table", "IntColumn", dataType: null),
                        errors);

            property.Should().BeNull();
            errors.Count.Should().Be(1);
            errors.Single().Message.Should().Be(
                string.Format(
                    CultureInfo.InvariantCulture,
                    Resources_VersioningFacade.UnsupportedDataTypeUnknownType,
                    "IntColumn",
                    "catalog.schema.table"));
            errors.Single().ErrorCode.Should().Be(6005);
        }

        [TestMethod]
        public void CreateProperty_returns_error_for_unknown_property_type()
        {
            List<EdmSchemaError> errors = new List<EdmSchemaError>();
            var property =
                CreateStoreModelBuilder()
                    .CreateProperty(
                        CreateRow("catalog", "schema", "table", "IntColumn", dataType: "invalid-type"),
                        errors);

            property.Should().BeNull();
            errors.Count.Should().Be(1);
            errors.Single().Message.Should().Be(
                string.Format(
                    CultureInfo.InvariantCulture,
                    Resources_VersioningFacade.UnsupportedDataType,
                    "invalid-type",
                    "catalog.schema.table",
                    "IntColumn"));
            errors.Single().ErrorCode.Should().Be(6005);
        }

        [TestMethod]
        public void CreateProperty_supports_geography_and_geometry_types_in_Version3()
        {
            // In Version3, geography and geometry types are fully supported
            var schemaGenerator = CreateStoreModelBuilder(targetEntityFrameworkVersion: EntityFrameworkVersion.Version3);

            foreach (var typeName in new[] { "geography", "geometry" })
            {
                List<EdmSchemaError> errors = new List<EdmSchemaError>();
                var property =
                    schemaGenerator.CreateProperty(
                        CreateRow("catalog", "schema", "table", "SpatialColumn", dataType: typeName),
                        errors);

                property.Should().NotBeNull();
                errors.Should().BeEmpty();
            }
        }

        // datetime sql server type is const (as opposed to datetime2)
        [TestMethod]
        public void CreateProperty_uses_const_value_for_facets_that_are_const()
        {
            List<EdmSchemaError> errors = new List<EdmSchemaError>();
            var property =
                CreateStoreModelBuilder()
                    .CreateProperty(
                        CreateRow(
                            table: "table", columnName: "datetimeColumn", dataType: "datetime",
                            dateTimePrecision: 2),
                        errors);

            property.Should().NotBeNull();
            property.PrimitiveType.PrimitiveTypeKind.Should().Be(PrimitiveTypeKind.DateTime);
            ((byte)property.Precision).Should().Be(3);
            errors.Should().BeEmpty();
        }

        [TestMethod]
        public void CreateProperty_does_not_validate_value_for_facets_that_are_const()
        {
            List<EdmSchemaError> errors = new List<EdmSchemaError>();
            var property =
                CreateStoreModelBuilder()
                    .CreateProperty(
                        CreateRow(
                            table: "table", columnName: "datetimeColumn", dataType: "datetime",
                            dateTimePrecision: byte.MaxValue),
                        errors);

            property.Should().NotBeNull();
            property.PrimitiveType.PrimitiveTypeKind.Should().Be(PrimitiveTypeKind.DateTime);
            ((byte)property.Precision).Should().Be(3);
            errors.Should().BeEmpty();
        }

        [TestMethod]
        public void CreateProperty_returns_decimal_property_with_specified_scale_and_precision()
        {
            List<EdmSchemaError> errors = new List<EdmSchemaError>();
            var property =
                CreateStoreModelBuilder()
                    .CreateProperty(
                        CreateRow(
                            table: "table", columnName: "DecimalColumn", dataType: "decimal", scale: 4,
                            precision: 12),
                        errors);

            property.Should().NotBeNull();
            property.PrimitiveType.PrimitiveTypeKind.Should().Be(PrimitiveTypeKind.Decimal);
            ((byte)property.Scale).Should().Be(4);
            ((byte)property.Precision).Should().Be(12);
            errors.Should().BeEmpty();
        }

        [TestMethod]
        public void CreateProperty_creates_decimal_property_with_default_scale_and_precision_if_they_are_not_specified()
        {
            List<EdmSchemaError> errors = new List<EdmSchemaError>();
            var property =
                CreateStoreModelBuilder()
                    .CreateProperty(
                        CreateRow(table: "table", columnName: "DecimalColumn", dataType: "decimal"),
                        errors);

            property.Should().NotBeNull();
            property.PrimitiveType.PrimitiveTypeKind.Should().Be(PrimitiveTypeKind.Decimal);
            ((byte)property.Scale).Should().Be(0);
            ((byte)property.Precision).Should().Be(18);
            errors.Should().BeEmpty();
        }

        [TestMethod]
        public void CreateProperty_returns_error_if_precision_is_out_of_range_for_decimal_property()
        {
            foreach (var precision in new byte[] { 0, 255 })
            {
                List<EdmSchemaError> errors = new List<EdmSchemaError>();
                var property =
                    CreateStoreModelBuilder()
                        .CreateProperty(
                            CreateRow(
                                "catalog", "schema", "table", "DecimalColumn", dataType: "decimal",
                                precision: precision),
                            errors);

                property.Should().BeNull();
                errors.Count.Should().Be(1);
                errors.Single().Message.Should().Be(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources_VersioningFacade.ColumnFacetValueOutOfRange,
                        "Precision",
                        precision,
                        1,
                        38,
                        "DecimalColumn",
                        "catalog.schema.table"));
                errors.Single().ErrorCode.Should().Be(6006);
            }
        }

        [TestMethod]
        public void CreateProperty_returns_error_if_scale_is_out_of_range_for_decimal_property()
        {
            foreach (var scale in new[] { -1, 255 })
            {
                List<EdmSchemaError> errors = new List<EdmSchemaError>();
                var property =
                    CreateStoreModelBuilder()
                        .CreateProperty(
                            CreateRow(
                                "catalog", "schema", "table", "DecimalColumn", dataType: "decimal",
                                scale: scale),
                            errors);

                property.Should().BeNull();
                errors.Count.Should().Be(1);
                errors.Single().Message.Should().Be(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources_VersioningFacade.ColumnFacetValueOutOfRange,
                        "Scale",
                        scale,
                        0,
                        38,
                        "DecimalColumn",
                        "catalog.schema.table"));
                errors.Single().ErrorCode.Should().Be(6006);
            }
        }

        [TestMethod]
        public void CreateProperty_returns_datetime_property_with_specified_datetimeprecision()
        {
            List<EdmSchemaError> errors = new List<EdmSchemaError>();
            var property =
                CreateStoreModelBuilder()
                    .CreateProperty(
                        CreateRow(
                            table: "table", columnName: "DateTime2Column", dataType: "datetime2",
                            dateTimePrecision: 4),
                        errors);

            property.Should().NotBeNull();
            errors.Should().BeEmpty();
            property.PrimitiveType.PrimitiveTypeKind.Should().Be(PrimitiveTypeKind.DateTime);
            ((byte)property.Precision).Should().Be(4);
        }

        [TestMethod]
        public void CreateProperty_creates_datetime_property_with_default_precision_if_it_is_not_specified()
        {
            List<EdmSchemaError> errors = new List<EdmSchemaError>();
            var property =
                CreateStoreModelBuilder()
                    .CreateProperty(
                        CreateRow(table: "table", columnName: "DateTime2Column", dataType: "datetime2"),
                        errors);

            property.Should().NotBeNull();
            property.PrimitiveType.PrimitiveTypeKind.Should().Be(PrimitiveTypeKind.DateTime);
            ((byte)property.Precision).Should().Be(7);
            errors.Should().BeEmpty();
        }

        [TestMethod]
        public void CreateProperty_returns_error_if_datetimeprecision_is_out_of_range_for_datetime_property()
        {
            foreach (var precision in new[] { -1, 255 })
            {
                List<EdmSchemaError> errors = new List<EdmSchemaError>();
                var property =
                    CreateStoreModelBuilder()
                        .CreateProperty(
                            CreateRow(
                                "catalog", "schema", "table", "DateTime2Column", dataType: "datetime2",
                                dateTimePrecision: precision),
                            errors);

                property.Should().BeNull();
                errors.Count.Should().Be(1);
                errors.Single().Message.Should().Be(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources_VersioningFacade.ColumnFacetValueOutOfRange,
                        "Precision",
                        precision,
                        0,
                        7,
                        "DateTime2Column",
                        "catalog.schema.table"));
                errors.Single().ErrorCode.Should().Be(6006);
            }
        }

        [TestMethod]
        public void CreateProperty_returns_datetimeoffset_property_with_specified_datetimeprecision()
        {
            List<EdmSchemaError> errors = new List<EdmSchemaError>();
            var property =
                CreateStoreModelBuilder()
                    .CreateProperty(
                        CreateRow(
                            table: "table", columnName: "DateTimeOffsetColumn", dataType: "datetimeoffset",
                            dateTimePrecision: 4),
                        errors);

            property.Should().NotBeNull();
            errors.Should().BeEmpty();
            property.PrimitiveType.PrimitiveTypeKind.Should().Be(PrimitiveTypeKind.DateTimeOffset);
            ((byte)property.Precision).Should().Be(4);
        }

        [TestMethod]
        public void CreateProperty_creates_datetimeoffset_property_with_default_precision_if_it_is_not_specified()
        {
            List<EdmSchemaError> errors = new List<EdmSchemaError>();
            var property =
                CreateStoreModelBuilder()
                    .CreateProperty(
                        CreateRow(table: "table", columnName: "DateTimeOffsetColumn", dataType: "datetimeoffset"),
                        errors);

            property.Should().NotBeNull();
            property.PrimitiveType.PrimitiveTypeKind.Should().Be(PrimitiveTypeKind.DateTimeOffset);
            ((byte)property.Precision).Should().Be(7);
            errors.Should().BeEmpty();
        }

        [TestMethod]
        public void CreateProperty_returns_error_if_datetimeprecision_is_out_of_range_for_datetimeoffset_property()
        {
            foreach (var precision in new[] { -1, 255 })
            {
                List<EdmSchemaError> errors = new List<EdmSchemaError>();
                var property =
                    CreateStoreModelBuilder()
                        .CreateProperty(
                            CreateRow(
                                "catalog", "schema", "table", "DateTimeOffsetColumn",
                                dataType: "datetimeoffset", dateTimePrecision: precision),
                            errors);

                property.Should().BeNull();
                errors.Count.Should().Be(1);
                errors.Single().Message.Should().Be(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources_VersioningFacade.ColumnFacetValueOutOfRange,
                        "Precision",
                        precision,
                        0,
                        7,
                        "DateTimeOffsetColumn",
                        "catalog.schema.table"));
                errors.Single().ErrorCode.Should().Be(6006);
            }
        }

        [TestMethod]
        public void CreateProperty_returns_time_property_with_specified_datetimeprecision()
        {
            List<EdmSchemaError> errors = new List<EdmSchemaError>();
            var property =
                CreateStoreModelBuilder()
                    .CreateProperty(
                        CreateRow(table: "table", columnName: "TimeColumn", dataType: "time", dateTimePrecision: 4),
                        errors);

            property.Should().NotBeNull();
            errors.Should().BeEmpty();
            property.PrimitiveType.PrimitiveTypeKind.Should().Be(PrimitiveTypeKind.Time);
            ((byte)property.Precision).Should().Be(4);
        }

        [TestMethod]
        public void CreateProperty_creates_time_property_with_default_precision_if_it_is_not_specified()
        {
            List<EdmSchemaError> errors = new List<EdmSchemaError>();
            var property =
                CreateStoreModelBuilder()
                    .CreateProperty(
                        CreateRow(table: "table", columnName: "TimeColumn", dataType: "time"),
                        errors);

            property.Should().NotBeNull();
            property.PrimitiveType.PrimitiveTypeKind.Should().Be(PrimitiveTypeKind.Time);
            ((byte)property.Precision).Should().Be(7);
            errors.Should().BeEmpty();
        }

        [TestMethod]
        public void CreateProperty_returns_error_if_datetimeprecision_is_out_of_range_for_time_property()
        {
            foreach (var precision in new[] { -1, 255 })
            {
                List<EdmSchemaError> errors = new List<EdmSchemaError>();
                var property =
                    CreateStoreModelBuilder()
                        .CreateProperty(
                            CreateRow(
                                "catalog", "schema", "table", "TimeColumn", dataType: "time",
                                dateTimePrecision: precision),
                            errors);

                property.Should().BeNull();
                errors.Count.Should().Be(1);
                errors.Single().Message.Should().Be(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources_VersioningFacade.ColumnFacetValueOutOfRange,
                        "Precision",
                        precision,
                        0,
                        7,
                        "TimeColumn",
                        "catalog.schema.table"));
                errors.Single().ErrorCode.Should().Be(6006);
            }
        }

        [TestMethod]
        public void CreateProperty_creates_nvarcharmax_property_with_default_maxlength_if_it_is_not_specified()
        {
            List<EdmSchemaError> errors = new List<EdmSchemaError>();
            var property =
                CreateStoreModelBuilder()
                    .CreateProperty(
                        CreateRow(table: "table", columnName: "NVarCharMaxColumn", dataType: "nvarchar(max)"),
                        errors);

            property.Should().NotBeNull();
            property.PrimitiveType.PrimitiveTypeKind.Should().Be(PrimitiveTypeKind.String);
            ((long)property.MaxLength).Should().Be(1073741823);
            errors.Should().BeEmpty();
        }

        [TestMethod]
        public void CreateProperty_ignores_requested_maxlength_for_nvarcharmax_and_uses_constant_value()
        {
            List<EdmSchemaError> errors = new List<EdmSchemaError>();
            var property =
                CreateStoreModelBuilder()
                    .CreateProperty(
                        CreateRow(
                            table: "table", columnName: "NVarCharMaxColumn", dataType: "nvarchar(max)",
                            maximumLength: 4),
                        errors);

            property.Should().NotBeNull();
            errors.Should().BeEmpty();
            property.PrimitiveType.PrimitiveTypeKind.Should().Be(PrimitiveTypeKind.String);
            property.MaxLength.Should().Be(1073741823);
        }

        [TestMethod]
        public void CreateProperty_returns_varbinary_property_with_specified_maxlength()
        {
            List<EdmSchemaError> errors = new List<EdmSchemaError>();
            var property =
                CreateStoreModelBuilder()
                    .CreateProperty(
                        CreateRow(
                            table: "table", columnName: "VarbinaryColumn", dataType: "varbinary",
                            maximumLength: 4),
                        errors);

            property.Should().NotBeNull();
            errors.Should().BeEmpty();
            property.PrimitiveType.PrimitiveTypeKind.Should().Be(PrimitiveTypeKind.Binary);
            property.MaxLength.Should().Be(4);
        }

        [TestMethod]
        public void CreateProperty_creates_varbinary_property_with_default_maxlength_if_it_is_not_specified()
        {
            List<EdmSchemaError> errors = new List<EdmSchemaError>();
            var property =
                CreateStoreModelBuilder()
                    .CreateProperty(
                        CreateRow(table: "table", columnName: "VarbinaryColumn", dataType: "varbinary"),
                        errors);

            property.Should().NotBeNull();
            property.PrimitiveType.PrimitiveTypeKind.Should().Be(PrimitiveTypeKind.Binary);
            property.MaxLength.Should().Be(8000);
            errors.Should().BeEmpty();
        }

        [TestMethod]
        public void CreateProperty_returns_error_if_maxlength_is_out_of_range_for_varbinary_property()
        {
            foreach (var maxLength in new[] { -1, 10000 })
            {
                List<EdmSchemaError> errors = new List<EdmSchemaError>();
                var property =
                    CreateStoreModelBuilder()
                        .CreateProperty(
                            CreateRow(
                                "catalog", "schema", "table", "VarbinaryColumn", dataType: "varbinary",
                                maximumLength: maxLength),
                            errors);

                property.Should().BeNull();
                errors.Count.Should().Be(1);
                errors.Single().Message.Should().Be(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Resources_VersioningFacade.ColumnFacetValueOutOfRange,
                        "MaxLength",
                        maxLength,
                        1,
                        8000,
                        "VarbinaryColumn",
                        "catalog.schema.table"));
                errors.Single().ErrorCode.Should().Be(6006);
            }
        }

        [TestMethod]
        public void CreateProperty_sets_identity()
        {
            List<EdmSchemaError> errors = new List<EdmSchemaError>();
            var property =
                CreateStoreModelBuilder()
                    .CreateProperty(
                        CreateRow(columnName: "IntColumn", dataType: "int", isIdentity: true),
                        errors);

            property.Should().NotBeNull();
            property.Name.Should().Be("IntColumn");
            property.PrimitiveType.PrimitiveTypeKind.Should().Be(PrimitiveTypeKind.Int32);
            property.StoreGeneratedPattern.Should().Be(StoreGeneratedPattern.Identity);
            errors.Should().BeEmpty();
        }

        [TestMethod]
        public void CreateProperty_sets_computed()
        {
            List<EdmSchemaError> errors = new List<EdmSchemaError>();
            var property =
                CreateStoreModelBuilder()
                    .CreateProperty(
                        CreateRow(columnName: "IntColumn", dataType: "int", isServerGenerated: true),
                        errors);

            property.Should().NotBeNull();
            property.Name.Should().Be("IntColumn");
            property.PrimitiveType.PrimitiveTypeKind.Should().Be(PrimitiveTypeKind.Int32);
            property.StoreGeneratedPattern.Should().Be(StoreGeneratedPattern.Computed);
            errors.Should().BeEmpty();
        }

        [TestMethod]
        public void CreateProperties_creates_properties_for_valid_rows_and_exclude_properties_for_invalid_rows()
        {
            List<TableDetailsRow> rows =
                new List<TableDetailsRow>
                    {
                        CreateRow(table: "TestTable", columnName: "IntColumn", dataType: "int", isPrimaryKey: true),
                        CreateRow(table: "TestTable", columnName: "GeographyKey", dataType: "geography", isPrimaryKey: true),
                        CreateRow(table: "TestTable", columnName: "DecimalColumn", dataType: "decimal", isPrimaryKey: false),
                        CreateRow(table: "TestTable", columnName: "InvalidColumn", isPrimaryKey: false)
                    };

            List<EdmSchemaError> errors = new List<EdmSchemaError>();
            var properties =
                CreateStoreModelBuilder()
                    .CreateProperties(rows, errors, out List<string> keyColumns, out List<string> excludedColumns, out List<string> invalidKeyTypeColumns);

            properties.Count.Should().Be(3);
            properties[0].Name.Should().Be("IntColumn");
            properties[1].Name.Should().Be("GeographyKey");
            properties[2].Name.Should().Be("DecimalColumn");

            errors.Count.Should().Be(3);
            errors[0].Message.Should().Be(
                string.Format(
                    CultureInfo.InvariantCulture,
                    Resources_VersioningFacade.CoercingNullablePrimaryKeyPropertyToNonNullable,
                    "IntColumn",
                    "TestTable"));
            errors[0].Severity.Should().Be(EdmSchemaErrorSeverity.Warning);
            errors[1].Message.Should().Be(
                string.Format(
                    CultureInfo.InvariantCulture,
                    Resources_VersioningFacade.CoercingNullablePrimaryKeyPropertyToNonNullable,
                    "GeographyKey",
                    "TestTable"));
            errors[1].Severity.Should().Be(EdmSchemaErrorSeverity.Warning);
            errors[2].Message.Should().Be(
                string.Format(
                    CultureInfo.InvariantCulture,
                    Resources_VersioningFacade.UnsupportedDataTypeUnknownType,
                    "InvalidColumn",
                    "TestTable"));
            errors[2].Severity.Should().Be(EdmSchemaErrorSeverity.Warning);

            excludedColumns.Single().Should().Be("InvalidColumn");
            keyColumns.Count.Should().Be(2);
            keyColumns[0].Should().Be("IntColumn");
            keyColumns[1].Should().Be("GeographyKey");
            invalidKeyTypeColumns.Count.Should().Be(1);
            invalidKeyTypeColumns[0].Should().Be("GeographyKey");
        }

        [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
        public void Build_creates_EdmModel_containing_converted_objects()
        {
            // Test commented out due to API visibility issues
        }
    }
}
