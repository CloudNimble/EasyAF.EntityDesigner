// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Globalization;
using System.Linq;
using Microsoft.Data.Entity.Design.VersioningFacade;
using Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb;
using Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb.SchemaDiscovery;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb
{
    public partial class StoreModelBuilderTests
    {
        [TestMethod]
        public void GetFunctionParameterType_returns_PrimitiveType_for_valid_parameter_type()
        {
            List<EdmSchemaError> errors = new List<EdmSchemaError>();

            var type = CreateStoreModelBuilder()
                .GetFunctionParameterType(
                    CreateFunctionDetailsRow(paramTypeName: "smallint"), 1, errors);

            type.Should().NotBeNull();
            type.PrimitiveTypeKind.Should().Be(PrimitiveTypeKind.Int16);
        }

        [TestMethod]
        public void GetFunctionParameterType_returns_error_for_null_parameter_type_name()
        {
            List<EdmSchemaError> errors = new List<EdmSchemaError>();

            var type = CreateStoreModelBuilder()
                .GetFunctionParameterType(
                    CreateFunctionDetailsRow(functionName: "function", paramName: "param", paramTypeName: null),
                    1, errors);

            type.Should().BeNull();
            errors.Count.Should().Be(1);
            var error = errors.Single();

            error.Severity.Should().Be(EdmSchemaErrorSeverity.Warning);
            error.ErrorCode.Should().Be((int)ModelBuilderErrorCode.UnsupportedType);
            error.Message.Should().Be(
                string.Format(
                    CultureInfo.InvariantCulture,
                    Resources_VersioningFacade.UnsupportedFunctionParameterDataType,
                    "function", "param", 1, "null"));
        }

        [TestMethod]
        public void GetFunctionParameterType_returns_error_for_invalid_parameter_type()
        {
            List<EdmSchemaError> errors = new List<EdmSchemaError>();

            var type = CreateStoreModelBuilder()
                .GetFunctionParameterType(
                    CreateFunctionDetailsRow(functionName: "function", paramName: "param", paramTypeName: "foo-type"),
                    1, errors);

            type.Should().BeNull();
            errors.Count.Should().Be(1);
            var error = errors.Single();

            error.Severity.Should().Be(EdmSchemaErrorSeverity.Warning);
            error.ErrorCode.Should().Be((int)ModelBuilderErrorCode.UnsupportedType);
            error.Message.Should().Be(
                string.Format(
                    CultureInfo.InvariantCulture,
                    Resources_VersioningFacade.UnsupportedFunctionParameterDataType,
                    "function", "param", 1, "foo-type"));
        }

        [TestMethod]
        public void GetFunctionParameterType_supports_geography_type_in_Version3()
        {
            // In Version3, geography type is fully supported for function parameters
            List<EdmSchemaError> errors = new List<EdmSchemaError>();

            var type = CreateStoreModelBuilder(targetEntityFrameworkVersion: EntityFrameworkVersion.Version3)
                .GetFunctionParameterType(
                    CreateFunctionDetailsRow(functionName: "function", paramName: "param", paramTypeName: "geography"),
                    1, errors);

            type.Should().NotBeNull();
            errors.Should().BeEmpty();
            type.PrimitiveTypeKind.Should().Be(PrimitiveTypeKind.Geography);
        }

        [TestMethod]
        public void CreateFunctionParameter_returns_null_for_invalid_type_name()
        {
            List<EdmSchemaError> errors = new List<EdmSchemaError>();

            var parameter = CreateStoreModelBuilder()
                .CreateFunctionParameter(
                    CreateFunctionDetailsRow(functionName: "function", paramName: "param", paramTypeName: null),
                    new UniqueIdentifierService(),
                    1, errors);

            parameter.Should().BeNull();
            errors.Count.Should().Be(1);
            var error = errors.Single();

            error.Severity.Should().Be(EdmSchemaErrorSeverity.Warning);
            error.ErrorCode.Should().Be((int)ModelBuilderErrorCode.UnsupportedType);
            error.Message.Should().Be(
                string.Format(
                    CultureInfo.InvariantCulture,
                    Resources_VersioningFacade.UnsupportedFunctionParameterDataType,
                    "function", "param", 1, "null"));
        }

        [TestMethod]
        public void CreateFunctionParameter_returns_null_for_invalid_parameter_direction()
        {
            List<EdmSchemaError> errors = new List<EdmSchemaError>();

            var parameter = CreateStoreModelBuilder()
                .CreateFunctionParameter(
                    CreateFunctionDetailsRow(
                        functionName: "function", paramName: "param", paramTypeName: "smallint", parameterDirection: "foo"),
                    new UniqueIdentifierService(),
                    1, errors);

            parameter.Should().BeNull();
            errors.Count.Should().Be(1);
            var error = errors.Single();

            error.Severity.Should().Be(EdmSchemaErrorSeverity.Warning);
            error.ErrorCode.Should().Be((int)ModelBuilderErrorCode.ParameterDirectionNotValid);
            error.Message.Should().Be(
                string.Format(
                    CultureInfo.InvariantCulture,
                    Resources_VersioningFacade.ParameterDirectionNotValid,
                    "function", "param", "foo"));
        }

        [TestMethod]
        public void CreateFunctionParameter_returns_parameter_for_valid_function_details_row()
        {
            List<EdmSchemaError> errors = new List<EdmSchemaError>();

            var parameter = CreateStoreModelBuilder()
                .CreateFunctionParameter(
                    CreateFunctionDetailsRow(
                        functionName: "function", paramName: "param", paramTypeName: "smallint", parameterDirection: "INOUT"),
                    new UniqueIdentifierService(),
                    1, errors);

            parameter.Should().NotBeNull();
            errors.Should().BeEmpty();
            parameter.Name.Should().Be("param");
            parameter.TypeUsage.EdmType.Name.Should().Be("smallint");
            parameter.Mode.Should().Be(ParameterMode.InOut);
        }

        [TestMethod]
        public void CreateFunctionParameter_applies_ECMA_name_conversion_for_parameter_name()
        {
            List<EdmSchemaError> errors = new List<EdmSchemaError>();

            var parameter = CreateStoreModelBuilder()
                .CreateFunctionParameter(
                    CreateFunctionDetailsRow(
                        functionName: "function", paramName: "p@r@m", paramTypeName: "smallint", parameterDirection: "INOUT"),
                    new UniqueIdentifierService(),
                    1, errors);

            parameter.Should().NotBeNull();
            errors.Should().BeEmpty();
            parameter.Name.Should().Be("p_r_m");
            parameter.TypeUsage.EdmType.Name.Should().Be("smallint");
            parameter.Mode.Should().Be(ParameterMode.InOut);
        }

        [TestMethod]
        public void CreateFunctionParameter_applies_ECMA_name_conversion_and_uniquifies_parameter_name()
        {
            List<EdmSchemaError> errors = new List<EdmSchemaError>();
            UniqueIdentifierService uniquifiedIdentifierService = new UniqueIdentifierService();
            uniquifiedIdentifierService.AdjustIdentifier("p_r_m");

            var parameter = CreateStoreModelBuilder()
                .CreateFunctionParameter(
                    CreateFunctionDetailsRow(
                        functionName: "function", paramName: "p@r@m", paramTypeName: "smallint", parameterDirection: "INOUT"),
                    uniquifiedIdentifierService,
                    1, errors);

            parameter.Should().NotBeNull();
            errors.Should().BeEmpty();
            parameter.Name.Should().Be("p_r_m1");
            parameter.TypeUsage.EdmType.Name.Should().Be("smallint");
            parameter.Mode.Should().Be(ParameterMode.InOut);
        }

        [TestMethod]
        public void CreateFunctionParameters_creates_parameters_for_all_valid_rows()
        {
            var functionDetailsRows =
                new[]
                    {
                        CreateFunctionDetailsRow(
                            functionName: "function", paramName: "param",
                            paramTypeName: "smallint", parameterDirection: "IN"),
                        CreateFunctionDetailsRow(
                            functionName: "function", paramName: "param2",
                            paramTypeName: "int", parameterDirection: "INOUT"),
                        CreateFunctionDetailsRow(
                            functionName: "function", paramName: "param3",
                            paramTypeName: "geometry", parameterDirection: "OUT")
                    };

            List<EdmSchemaError> errors = new List<EdmSchemaError>();
            var parameters = CreateStoreModelBuilder()
                .CreateFunctionParameters(functionDetailsRows, errors).ToArray();

            parameters.Length.Should().Be(3);
            parameters.Select(p => p.Name).Should().BeEquivalentTo(new[] { "param", "param2", "param3" });
            parameters.Select(p => p.TypeName).Should().BeEquivalentTo(new[] { "smallint", "int", "geometry" });
            parameters.Select(p => p.Mode).Should().BeEquivalentTo(
                new[] { ParameterMode.In, ParameterMode.InOut, ParameterMode.Out, });

            errors.Should().BeEmpty();
        }

        [TestMethod]
        public void CreateFunctionParameters_uniquifies_parameter_names()
        {
            var functionDetailsRows =
                new[]
                    {
                        CreateFunctionDetailsRow(
                            functionName: "function", paramName: "param",
                            paramTypeName: "smallint", parameterDirection: "IN"),
                        CreateFunctionDetailsRow(
                            functionName: "function", paramName: "param",
                            paramTypeName: "geometry", parameterDirection: "OUT")
                    };

            List<EdmSchemaError> errors = new List<EdmSchemaError>();
            var parameters = CreateStoreModelBuilder()
                .CreateFunctionParameters(functionDetailsRows, errors).ToArray();

            parameters.Length.Should().Be(2);
            parameters.Select(p => p.Name).Should().BeEquivalentTo(new[] { "param", "param1" });
            errors.Should().BeEmpty();
        }

        [TestMethod]
        public void CreateFunctionParameters_does_not_return_parameters_for_invalid_rows()
        {
            var functionDetailsRows =
                new[]
                    {
                        CreateFunctionDetailsRow(
                            functionName: "function", paramName: "param",
                            paramTypeName: "smallint", parameterDirection: "IN"),
                        CreateFunctionDetailsRow(
                            functionName: "function", paramName: "param2",
                            paramTypeName: "foo", parameterDirection: "INOUT"),
                        CreateFunctionDetailsRow(
                            functionName: "function", paramName: "param3",
                            paramTypeName: "geometry", parameterDirection: "OUT")
                    };

            List<EdmSchemaError> errors = new List<EdmSchemaError>();
            var parameters = CreateStoreModelBuilder()
                .CreateFunctionParameters(functionDetailsRows, errors).ToArray();

            parameters.Length.Should().Be(2);
            parameters.Any(p => p == null).Should().BeFalse();

            errors.Count.Should().Be(1);
            var error = errors.Single();
            error.Severity.Should().Be(EdmSchemaErrorSeverity.Warning);
            error.ErrorCode.Should().Be((int)ModelBuilderErrorCode.UnsupportedType);
            error.Message.Should().Be(
                string.Format(
                    CultureInfo.InvariantCulture,
                    Resources_VersioningFacade.UnsupportedFunctionParameterDataType,
                    "function", "param2", 1, "foo"));
        }

        [TestMethod]
        public void CreateReturnParameter_creates_return_parameter_for_scalar_function()
        {
            List<EdmSchemaError> errors = new List<EdmSchemaError>();

            var parameter = CreateStoreModelBuilder()
                .CreateReturnParameter(
                    CreateFunctionDetailsRow(functionName: "function", returnTypeName: "int"),
                    [],
                    errors);

            parameter.Should().NotBeNull();
            parameter.Name.Should().Be("ReturnType");
            parameter.TypeName.Should().Be("int");
            errors.Should().BeEmpty();
        }

        [TestMethod]
        public void CreateReturnParameter_returns_error_if_return_type_not_valid()
        {
            List<EdmSchemaError> errors = new List<EdmSchemaError>();

            var parameter = CreateStoreModelBuilder()
                .CreateReturnParameter(
                    CreateFunctionDetailsRow(functionName: "function", returnTypeName: "foo"),
                    [],
                    errors);

            parameter.Should().BeNull();
            errors.Count.Should().Be(1);

            var error = errors.Single();
            error.Severity.Should().Be(EdmSchemaErrorSeverity.Warning);
            error.ErrorCode.Should().Be((int)ModelBuilderErrorCode.UnsupportedType);
            error.Message.Should().Be(
                string.Format(
                    CultureInfo.InvariantCulture,
                    Resources_VersioningFacade.UnsupportedFunctionReturnDataType,
                    "function", "foo"));
        }

        [TestMethod]
        public void CreateReturnParameter_supports_geometry_return_type_in_Version3()
        {
            // In Version3, geometry type is fully supported as a return type
            List<EdmSchemaError> errors = new List<EdmSchemaError>();

            var parameter = CreateStoreModelBuilder(targetEntityFrameworkVersion: EntityFrameworkVersion.Version3)
                .CreateReturnParameter(
                    CreateFunctionDetailsRow(functionName: "function", returnTypeName: "geometry"),
                    [],
                    errors);

            parameter.Should().NotBeNull();
            errors.Should().BeEmpty();
            parameter.TypeName.Should().Be("geometry");
        }

        [TestMethod]
        public void CreateReturnParameter_returns_null_for_stored_proc()
        {
            List<EdmSchemaError> errors = new List<EdmSchemaError>();

            var parameter = CreateStoreModelBuilder()
                .CreateReturnParameter(
                    CreateFunctionDetailsRow(functionName: "function", returnTypeName: null, isTvf: false),
                    [],
                    errors);

            parameter.Should().BeNull();
            errors.Count.Should().Be(0);
        }

        [TestMethod]
        public void CreateFunction_returns_TVF_with_errors_if_return_rowtype_for_TVF_is_invalid_and_copies_errors_from_invalid_RowType()
        {
            List<TableDetailsRow> tvfReturnTypeDetailsRow =
                new List<TableDetailsRow>
                    {
                        CreateRow(
                            catalog: "myDb", schema: "dbo", table: "function", columnName: "Id",
                            dataType: "foo")
                    };

            var functionDetailsRow =
                CreateFunctionDetailsRow(catalog: "myDb", schema: "dbo", functionName: "function", isTvf: true);

            List<EdmSchemaError> errors = new List<EdmSchemaError>();
            var storeModelBuilder = CreateStoreModelBuilder();
            var tvfReturnTypes = storeModelBuilder.CreateTvfReturnTypes(tvfReturnTypeDetailsRow);
            var parameter = storeModelBuilder.CreateReturnParameter(functionDetailsRow, tvfReturnTypes, errors);

            parameter.Should().BeNull();

            errors.Count.Should().Be(2);
            errors[0].Message.Should().Be(
                string.Format(
                    CultureInfo.InvariantCulture,
                    Resources_VersioningFacade.TableReferencedByTvfWasNotFound,
                    "myDb.dbo.function"));

            errors[1].Message.Should().Be(
                string.Format(
                    CultureInfo.InvariantCulture,
                    Resources_VersioningFacade.UnsupportedDataType,
                    "foo", "myDb.dbo.function", "Id"));
        }

        [TestMethod]
        public void CreateReturnParameter_returns_null_if_return_type_for_TVF_not_found()
        {
            var functionDetailsRows =
                CreateFunctionDetailsRow(
                    catalog: "myDb", schema: "dbo",
                    functionName: "function", isTvf: true, paramName: "param",
                    paramTypeName: "smallint", parameterDirection: "IN");

            List<EdmSchemaError> errors = new List<EdmSchemaError>();
            var parameter =
                CreateStoreModelBuilder()
                    .CreateReturnParameter(functionDetailsRows, [], errors);

            parameter.Should().BeNull();

            errors.Count.Should().Be(1);
            errors.Single().Message.Should().Be(
                string.Format(
                    CultureInfo.InvariantCulture,
                    Resources_VersioningFacade.TableReferencedByTvfWasNotFound,
                    "myDb.dbo.function"));

            errors.Single().ErrorCode.Should().Be((int)ModelBuilderErrorCode.MissingTvfReturnTable);
        }

        [TestMethod]
        public void CreateFunction_creates_function_for_TVF_in_Version3()
        {
            // TVFs are supported in Version3
            List<TableDetailsRow> tvfReturnTypeDetailsRow =
                new List<TableDetailsRow>
                    {
                        CreateRow(catalog: "myDb", schema: "dbo", table: "function", columnName: "Id", dataType: "int")
                    };

            List<FunctionDetailsRowView> functionDetailsRows =
                new List<FunctionDetailsRowView>
                    {
                        CreateFunctionDetailsRow(catalog: "myDb", schema: "dbo", functionName: "function", isTvf: true)
                    };

            var storeModelBuilder = CreateStoreModelBuilder(targetEntityFrameworkVersion: EntityFrameworkVersion.Version3);
            var tvfReturnTypes = storeModelBuilder.CreateTvfReturnTypes(tvfReturnTypeDetailsRow);
            var function = storeModelBuilder.CreateFunction(functionDetailsRows, tvfReturnTypes);

            function.Should().NotBeNull();
            function.FullName.Should().Be("myModel.function");
        }

        [TestMethod]
        public void CreateFunction_creates_scalar_function_for_valid_function_details_rows()
        {
            List<FunctionDetailsRowView> functionDetailsRows =
                new List<FunctionDetailsRowView>
                    {
                        CreateFunctionDetailsRow(
                            catalog: "myDb", schema: "dbo",
                            functionName: "function", returnTypeName: "int",
                            isAggregate: true, isComposable: true, isBuiltIn: true,
                            isNiladic: true, isTvf: false, paramName: "param",
                            paramTypeName: "smallint", parameterDirection: "IN"),
                        CreateFunctionDetailsRow(
                            functionName: "function", paramName: "param2",
                            paramTypeName: "int", parameterDirection: "INOUT"),
                        CreateFunctionDetailsRow(
                            functionName: "function", paramName: "param3",
                            paramTypeName: "geometry", parameterDirection: "OUT")
                    };

            var function = CreateStoreModelBuilder()
                .CreateFunction(functionDetailsRows, []);

            function.Should().NotBeNull();
            function.FullName.Should().Be("myModel.function");
            function.StoreFunctionNameAttribute.Should().BeNull();
            function.Parameters.Count.Should().Be(3);
            function.ReturnParameter.TypeName.Should().Be("int");
            function.AggregateAttribute.Should().BeTrue();
            function.IsComposableAttribute.Should().BeTrue();
            function.BuiltInAttribute.Should().BeTrue();
            function.NiladicFunctionAttribute.Should().BeTrue();
            function.MetadataProperties.Any(p => p.Name == "EdmSchemaErrors").Should().BeFalse();
        }

        [TestMethod]
        public void CreateFunction_creates_TVF_function_for_valid_function_details_rows()
        {
            List<TableDetailsRow> tvfReturnTypeDetailsRow =
                new List<TableDetailsRow>
                    {
                        CreateRow(catalog: "myDb", schema: "dbo", table: "function", columnName: "Id", dataType: "int"),
                        CreateRow(catalog: "myDb", schema: "dbo", table: "function", columnName: "Name", dataType: "nvarchar(max)")
                    };

            List<FunctionDetailsRowView> functionDetailsRows =
                new List<FunctionDetailsRowView>
                    {
                        CreateFunctionDetailsRow(
                            catalog: "myDb", schema: "dbo",
                            functionName: "function", isTvf: true, paramName: "param",
                            paramTypeName: "smallint", parameterDirection: "IN"),
                    };

            var storeModelBuilder = CreateStoreModelBuilder();
            var tvfReturnTypes = storeModelBuilder.CreateTvfReturnTypes(tvfReturnTypeDetailsRow);
            var function = storeModelBuilder.CreateFunction(functionDetailsRows, tvfReturnTypes);

            function.Should().NotBeNull();
            function.FullName.Should().Be("myModel.function");
            function.ReturnParameter.TypeUsage.EdmType.Should().BeSameAs(tvfReturnTypes.Values.Single().GetCollectionType());
            function.MetadataProperties.Any(p => p.Name == "EdmSchemaErrors").Should().BeFalse();
        }

        [TestMethod]
        public void CreateFunction_creates_EdmSchemaErrors_metadata_property_for_invalid_functions()
        {
            List<FunctionDetailsRowView> functionDetailsRows =
                new List<FunctionDetailsRowView>
                    {
                        CreateFunctionDetailsRow(functionName: "function", isTvf: true),
                    };

            var function =
                CreateStoreModelBuilder()
                    .CreateFunction(functionDetailsRows, []);

            function.MetadataProperties.Any(p => p.Name == "EdmSchemaErrors").Should().BeTrue();
        }

        [TestMethod]
        public void CreateFunction_creates_stored_procedure_for_valid_function_details_rows()
        {
            List<FunctionDetailsRowView> functionDetailsRows =
                new List<FunctionDetailsRowView>
                    {
                        CreateFunctionDetailsRow(
                            catalog: "myDb", schema: "dbo",
                            functionName: "function", isTvf: false, paramName: "param",
                            paramTypeName: "smallint", parameterDirection: "IN"),
                    };

            var function = CreateStoreModelBuilder()
                .CreateFunction(functionDetailsRows, []);

            function.Should().NotBeNull();
            function.FullName.Should().Be("myModel.function");
            function.StoreFunctionNameAttribute.Should().BeNull();
            function.Parameters.Count.Should().Be(1);
            function.ReturnParameter.Should().BeNull();
            function.MetadataProperties.Any(p => p.Name == "EdmSchemaErrors").Should().BeFalse();
        }

        [TestMethod]
        public void Can_CreateFunction_without_parameters()
        {
            List<FunctionDetailsRowView> functionDetailsRows =
                new List<FunctionDetailsRowView>
                    {
                        CreateFunctionDetailsRow(
                            catalog: "myDb", schema: "dbo",
                            functionName: "function", isTvf: false, paramName: null),
                    };

            var function = CreateStoreModelBuilder()
                .CreateFunction(functionDetailsRows, []);

            function.Should().NotBeNull();
            function.FullName.Should().Be("myModel.function");
            function.StoreFunctionNameAttribute.Should().BeNull();
            function.Parameters.Count.Should().Be(0);
            function.ReturnParameter.Should().BeNull();
            function.MetadataProperties.Any(p => p.Name == "EdmSchemaErrors").Should().BeFalse();
        }

        [TestMethod]
        public void CreateFunction_applies_ECMA_name_conversion_and_uniquifies_parameter_name()
        {
            List<FunctionDetailsRowView> functionDetailsRows =
                new List<FunctionDetailsRowView>
                    {
                        CreateFunctionDetailsRow(functionName: "#@$&!", isTvf: false, paramName: null),
                    };

            var storeModelBuilder = CreateStoreModelBuilder();
            var function = storeModelBuilder.CreateFunction(functionDetailsRows, []);

            function.Should().NotBeNull();
            function.FullName.Should().Be("myModel.f_____");

            function = storeModelBuilder.CreateFunction(functionDetailsRows, []);

            function.Should().NotBeNull();
            function.FullName.Should().Be("myModel.f_____1");
        }

        [TestMethod]
        public void CreateFunction_sets_StoreFunctionName_if_the_original_name_was_changed()
        {
            List<FunctionDetailsRowView> functionDetailsRows =
                new List<FunctionDetailsRowView>
                    {
                        CreateFunctionDetailsRow(functionName: "#@$&!", isTvf: false, paramName: null),
                    };

            var function = CreateStoreModelBuilder()
                .CreateFunction(functionDetailsRows, []);

            function.Should().NotBeNull();
            function.FullName.Should().Be("myModel.f_____");
            function.StoreFunctionNameAttribute.Should().Be("#@$&!");
        }
    }
}
