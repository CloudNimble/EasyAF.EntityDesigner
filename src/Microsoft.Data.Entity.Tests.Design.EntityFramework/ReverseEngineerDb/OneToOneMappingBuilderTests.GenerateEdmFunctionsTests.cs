// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure.Pluralization;
using System.Linq;
using Microsoft.Data.Entity.Design.VersioningFacade;
using Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb;
using Microsoft.Data.Entity.Design.VersioningFacade.ReverseEngineerDb.SchemaDiscovery;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb
{
   public partial class OneToOneMappingBuilderTests
   {
      [TestClass]
    public class GenerateEdmFunctionsTests
      {
         [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
         public void CreateFunctionImportParameters_creates_function_import_parameters_from_store_function_parameters() {
            //var storeFunctionParameters =
            //    new[]
            //        {
            //            CreatePrimitiveParameter("numberParam", PrimitiveTypeKind.Int32, ParameterMode.In),
            //            CreatePrimitiveParameter("stringParam", PrimitiveTypeKind.String, ParameterMode.In)
            //        };

            //var returnParameters =
            //    new[]
            //        {
            //            FunctionParameter.Create(
            //                "ReturnType",
            //                CreateRowType(CreateProperty("foo", PrimitiveTypeKind.Int32)).GetCollectionType(),
            //                ParameterMode.ReturnValue)
            //        };

            //var storeFunction =
            //    EdmFunction.Create(
            //        "foo",
            //        "bar",
            //        DataSpace.SSpace,
            //        new EdmFunctionPayload
            //            {
            //                Parameters = storeFunctionParameters,
            //                ReturnParameters = returnParameters
            //            },
            //        null);

            //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
            //var functionImportParameters =
            //    OneToOneMappingBuilder.CreateFunctionImportParameters(mappingContext, storeFunction);

            //functionImportParameters.Should().NotBeNull();
            //Assert.Equal(storeFunctionParameters.Select(p => p.Name), functionImportParameters.Select(p => p.Name));
            //Assert.Equal(
            //    new[] { "Edm.Int32", "Edm.String" },
            //    functionImportParameters.Select(p => p.TypeUsage.EdmType.FullName));
            //functionImportParameters.All(p => p.Mode == ParameterMode.In.Should().BeTrue());
            //Assert.Empty(mappingContext.Errors);
         }

         [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
         public void CreateFunctionImportParameters_returns_null_if_store_function_parameters_had_to_be_renamed() {
            //var storeFunctionParameters =
            //    new[]
            //        {
            //            CreatePrimitiveParameter("numberParam_", PrimitiveTypeKind.Int32, ParameterMode.In),
            //            CreatePrimitiveParameter("numberParam*", PrimitiveTypeKind.Int32, ParameterMode.In)
            //        };

            //var returnParameters =
            //    new[]
            //        {
            //            FunctionParameter.Create(
            //                "ReturnType",
            //                CreateRowType(CreateProperty("foo", PrimitiveTypeKind.Int32)).GetCollectionType(),
            //                ParameterMode.ReturnValue)
            //        };

            //var storeFunction =
            //    EdmFunction.Create(
            //        "foo",
            //        "bar",
            //        DataSpace.SSpace,
            //        new EdmFunctionPayload
            //            {
            //                Parameters = storeFunctionParameters,
            //                ReturnParameters = returnParameters
            //            },
            //        null);

            //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
            //var functionImportParameters =
            //    OneToOneMappingBuilder.CreateFunctionImportParameters(mappingContext, storeFunction);

            //functionImportParameters.Should().BeNull();
            //mappingContext.Errors.Count.Should().Be(1);
            //Assert.Equal(
            //    String.Format(
            //        CultureInfo.InvariantCulture,
            //        Resources_VersioningFacade.UnableToGenerateFunctionImportParameterName,
            //        "numberParam*",
            //        "foo"),
            //    mappingContext.Errors.Single().Message);
            //Assert.Equal(
            //    (int)ModelBuilderErrorCode.UnableToGenerateFunctionImportParameterName,
            //    mappingContext.Errors.Single().ErrorCode);
            //Assert.Equal(EdmSchemaErrorSeverity.Warning, mappingContext.Errors.Single().Severity);
         }

         [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
         public void GetStoreTvfReturnType_returns_null_if_store_function_has_no_return_type() {
            //var storeFunction = EdmFunction.Create("foo", "bar", DataSpace.SSpace, new EdmFunctionPayload(), null);

            //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
            //OneToOneMappingBuilder.GetStoreTvfReturnType(mappingContext, storeFunction.Should().BeNull());

            //mappingContext.Errors.Count.Should().Be(1);
            //Assert.Equal(
            //    (int)ModelBuilderErrorCode.UnableToGenerateFunctionImportReturnType,
            //    mappingContext.Errors.Single().ErrorCode);
         }

         [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
         public void GetStoreTvfReturnType_returns_null_if_store_function_return_type_is_not_collection() {
            //var returnParameter =
            //    FunctionParameter.Create(
            //        "ReturnType",
            //        ProviderManifest.GetStoreTypes().First(),
            //        ParameterMode.ReturnValue);

            //var storeFunction =
            //    EdmFunction.Create(
            //        "foo",
            //        "bar",
            //        DataSpace.SSpace,
            //        new EdmFunctionPayload { ReturnParameters = new[] { returnParameter } },
            //        null);

            //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
            //OneToOneMappingBuilder.GetStoreTvfReturnType(mappingContext, storeFunction.Should().BeNull());
            //mappingContext.Errors.Count.Should().Be(1);
            //Assert.Equal(
            //    (int)ModelBuilderErrorCode.UnableToGenerateFunctionImportReturnType,
            //    mappingContext.Errors.Single().ErrorCode);
         }

         [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
         public void GetStoreTvfReturnType_returns_null_if_store_function_return_type_is_not_rowType_collection() {
            //var returnParameter =
            //    FunctionParameter.Create(
            //        "ReturnType",
            //        ProviderManifest.GetStoreTypes().First().GetCollectionType(),
            //        ParameterMode.ReturnValue);

            //var storeFunction =
            //    EdmFunction.Create(
            //        "foo",
            //        "bar",
            //        DataSpace.SSpace,
            //        new EdmFunctionPayload { ReturnParameters = new[] { returnParameter } },
            //        null);

            //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
            //OneToOneMappingBuilder.GetStoreTvfReturnType(mappingContext, storeFunction.Should().BeNull());
            //mappingContext.Errors.Count.Should().Be(1);
            //Assert.Equal(
            //    (int)ModelBuilderErrorCode.UnableToGenerateFunctionImportReturnType,
            //    mappingContext.Errors.Single().ErrorCode);
         }

         [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
         public void GetStoreTvfReturnType_returns_function_return_edm_type_if_return_type_valid() {
            //var rowType = RowType.Create(new EdmProperty[0], null);

            //var returnParameter =
            //    FunctionParameter.Create(
            //        "ReturnType",
            //        rowType.GetCollectionType(),
            //        ParameterMode.ReturnValue);

            //var storeFunction =
            //    EdmFunction.Create(
            //        "foo",
            //        "bar",
            //        DataSpace.SSpace,
            //        new EdmFunctionPayload { ReturnParameters = new[] { returnParameter } },
            //        null);

            //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
            //Assert.Same(
            //    rowType,
            //    OneToOneMappingBuilder.GetStoreTvfReturnType(mappingContext, storeFunction));
            //Assert.Empty(mappingContext.Errors);
         }

         [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
         public void CreateComplexTypeFromRowType_creates_CSpace_ComplexType_from_valid_SSpace_RowType() {
            //var rowType =
            //    CreateRowType(
            //        CreateProperty("number", PrimitiveTypeKind.Int32),
            //        CreateProperty("string", PrimitiveTypeKind.String));

            //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
            //var complexType =
            //    CreateOneToOneMappingBuilder(namespaceName: "bar")
            //        .CreateComplexTypeFromRowType(mappingContext, rowType, "foo");

            //complexType.Should().NotBeNull();
            //complexType.FullName.Should().Be("bar.foo");
            //Assert.Equal(new[] { "number", "string" }, complexType.Properties.Select(p => p.Name));
            //complexType.Properties.All(p => p.TypeUsage.EdmType.NamespaceName == "Edm".Should().BeTrue());
            //Assert.Equal(complexType.Properties, rowType.Properties.Select(p => mappingContext[p]));
         }

         [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
         public void CreateComplexTypeFromRowType_uniquifies_property_names() {
            //var rowType =
            //    CreateRowType(
            //        CreateProperty("number_", PrimitiveTypeKind.Int32),
            //        CreateProperty("number*", PrimitiveTypeKind.Int32));

            //var complexType =
            //    CreateOneToOneMappingBuilder(namespaceName: "bar")
            //        .CreateComplexTypeFromRowType(
            //            new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true),
            //            rowType,
            //            "foo");

            //complexType.Should().NotBeNull();
            //Assert.Equal(new[] { "number_", "number_1" }, complexType.Properties.Select(p => p.Name));
         }

         [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
         public void CreateComplexTypeFromRowType_does_not_create_properties_with_names_of_owning_type() {
            //var rowType = CreateRowType(CreateProperty("foo", PrimitiveTypeKind.Int32));

            //var complexType =
            //    CreateOneToOneMappingBuilder(namespaceName: "bar")
            //        .CreateComplexTypeFromRowType(
            //            new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true),
            //            rowType,
            //            "foo");

            //complexType.Should().NotBeNull();
            //Assert.Equal("foo1", complexType.Properties.Select(p => p.Name).Single());
         }

         [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
         public void GenerateFunction_returns_null_if_return_type_is_not_valid_TVF_return_type() {
            //var returnParameter =
            //    FunctionParameter.Create(
            //        "ReturnType",
            //        PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32),
            //        ParameterMode.ReturnValue);

            //var storeFunction =
            //    EdmFunction.Create(
            //        "foo",
            //        "bar",
            //        DataSpace.SSpace,
            //        new EdmFunctionPayload { ReturnParameters = new[] { returnParameter } },
            //        null);

            //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
            //Assert.Null(
            //    CreateOneToOneMappingBuilder()
            //        .GenerateFunction(
            //            mappingContext, storeFunction, new UniqueIdentifierService(),
            //            new UniqueIdentifierService()));

            //mappingContext.Errors.Count.Should().Be(1);
            //Assert.Equal(
            //    String.Format(
            //        CultureInfo.InvariantCulture,
            //        Resources_VersioningFacade.UnableToGenerateFunctionImportReturnType,
            //        "foo"),
            //    mappingContext.Errors.Single().Message);
            //Assert.Equal(
            //    (int)ModelBuilderErrorCode.UnableToGenerateFunctionImportReturnType,
            //    mappingContext.Errors.Single().ErrorCode);
            //Assert.Equal(EdmSchemaErrorSeverity.Warning, mappingContext.Errors.Single().Severity);
         }

         [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
         public void
             GenerateFunction_returns_null_if_function_import_parameter_name_different_from_corresponding_store_function_parameter_name
             () {
            //var parameter = CreatePrimitiveParameter("numberParam*", PrimitiveTypeKind.Int32, ParameterMode.In);
            //var storeReturnType =
            //    CreateRowType(CreateProperty("foo", PrimitiveTypeKind.Int32)).GetCollectionType();
            //var returnParameter =
            //    FunctionParameter.Create("ReturnType", storeReturnType, ParameterMode.ReturnValue);
            //var storeFunction =
            //    EdmFunction.Create(
            //        "foo",
            //        "bar",
            //        DataSpace.SSpace,
            //        new EdmFunctionPayload
            //            {
            //                Parameters = new[] { parameter },
            //                ReturnParameters = new[] { returnParameter }
            //            },
            //        null);

            //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
            //var functionImport =
            //    CreateOneToOneMappingBuilder()
            //        .GenerateFunction(
            //            mappingContext,
            //            storeFunction,
            //            new UniqueIdentifierService(),
            //            new UniqueIdentifierService());

            //functionImport.Should().BeNull();
            //mappingContext.Errors.Count.Should().Be(1);
            //Assert.Equal(
            //    (int)ModelBuilderErrorCode.UnableToGenerateFunctionImportParameterName,
            //    mappingContext.Errors.Single().ErrorCode);
         }

         [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
         public void GenerateFunction_returns_function_import_for_supported_store_function() {
            //var storeFunctionParameters =
            //    new[]
            //        {
            //            CreatePrimitiveParameter("numberParam", PrimitiveTypeKind.Int32, ParameterMode.In),
            //            CreatePrimitiveParameter("stringParam", PrimitiveTypeKind.String, ParameterMode.In)
            //        };
            //var storeReturnType = CreateRowType(CreateProperty("foo", PrimitiveTypeKind.Int32)).GetCollectionType();
            //var returnParameter =
            //    FunctionParameter.Create("ReturnType", storeReturnType, ParameterMode.ReturnValue);

            //var storeFunction =
            //    EdmFunction.Create(
            //        "foo",
            //        "bar",
            //        DataSpace.SSpace,
            //        new EdmFunctionPayload
            //            {
            //                Parameters = storeFunctionParameters,
            //                ReturnParameters = new[] { returnParameter }
            //            },
            //        null);

            //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);

            //var functionImport =
            //    CreateOneToOneMappingBuilder()
            //        .GenerateFunction(mappingContext, storeFunction, new UniqueIdentifierService(), new UniqueIdentifierService());

            //Assert.Empty(mappingContext.Errors);

            //functionImport.Should().NotBeNull();
            //functionImport.FullName.Should().Be("myModel.foo");

            //Assert.Equal(storeFunctionParameters.Select(p => p.Name), functionImport.Parameters.Select(p => p.Name));

            //functionImport.ReturnParameter.Name.Should().Be("ReturnType");
            //var returnType = ((CollectionType)functionImport.ReturnParameter.TypeUsage.EdmType);
            //Assert.IsType<ComplexType>(returnType.TypeUsage.EdmType);
            //returnType.TypeUsage.EdmType.FullName.Should().Be("myModel.foo_Result");
            //functionImport.IsComposableAttribute.Should().BeTrue();
         }

         [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
         public void GenerateFunction_returns_function_with_unique_name() {
            //var storeReturnType = CreateRowType(CreateProperty("foo", PrimitiveTypeKind.Int32)).GetCollectionType();
            //var returnParameter =
            //    FunctionParameter.Create("ReturnType", storeReturnType, ParameterMode.ReturnValue);

            //var storeFunction =
            //    EdmFunction.Create(
            //        "foo*",
            //        "bar",
            //        DataSpace.SSpace,
            //        new EdmFunctionPayload { ReturnParameters = new[] { returnParameter } },
            //        null);

            //var uniqueContainerNames = new UniqueIdentifierService();
            //uniqueContainerNames.AdjustIdentifier("foo_");

            //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
            //var functionImport =
            //    CreateOneToOneMappingBuilder()
            //        .GenerateFunction(mappingContext, storeFunction, uniqueContainerNames, new UniqueIdentifierService());

            //functionImport.Should().NotBeNull();
            //functionImport.FullName.Should().Be("myModel.foo_1");
            //Assert.Empty(mappingContext.Errors);
         }

         [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
         public void GenerateFunction_returns_function_with_unique_return_type_name() {
            //var storeReturnType = CreateRowType(CreateProperty("foo", PrimitiveTypeKind.Int32)).GetCollectionType();
            //var returnParameter =
            //    FunctionParameter.Create("ReturnType", storeReturnType, ParameterMode.ReturnValue);

            //var storeFunction =
            //    EdmFunction.Create(
            //        "foo*",
            //        "bar",
            //        DataSpace.SSpace,
            //        new EdmFunctionPayload { ReturnParameters = new[] { returnParameter } },
            //        null);

            //var uniqueContainerNames = new UniqueIdentifierService();
            //uniqueContainerNames.AdjustIdentifier("foo_");

            //var globallyUniqueTypeNames = new UniqueIdentifierService();
            //globallyUniqueTypeNames.AdjustIdentifier("foo_1_Result");

            //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
            //var functionImport =
            //    CreateOneToOneMappingBuilder()
            //        .GenerateFunction(mappingContext, storeFunction, uniqueContainerNames, globallyUniqueTypeNames);

            //functionImport.Should().NotBeNull();
            //Assert.Equal(
            //    "myModel.foo_1_Result1",
            //    ((CollectionType)functionImport.ReturnParameter.TypeUsage.EdmType).TypeUsage.EdmType.FullName);
            //Assert.Empty(mappingContext.Errors);
         }

         [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
         public void GenerateFunctions_does_not_generate_functions_for_non_v3_schema_versions() {
            //var storeReturnType = CreateRowType(CreateProperty("foo", PrimitiveTypeKind.Int32)).GetCollectionType();
            //var returnParameter =
            //    FunctionParameter.Create("ReturnType", storeReturnType, ParameterMode.ReturnValue);

            //var storeFunction =
            //    EdmFunction.Create(
            //        "foo*",
            //        "bar",
            //        DataSpace.SSpace,
            //        new EdmFunctionPayload { ReturnParameters = new[] { returnParameter } },
            //        null);

            //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
            //Assert.Empty(
            //    CreateOneToOneMappingBuilder()
            //        .GenerateFunctions(
            //            mappingContext,
            //            CreateStoreModel(EntityFrameworkVersion.Version1, storeFunction),
            //            new UniqueIdentifierService(),
            //            new UniqueIdentifierService()));
            //Assert.Empty(mappingContext.MappedStoreFunctions());
            //Assert.Empty(mappingContext.Errors);

            //Assert.Empty(
            //    CreateOneToOneMappingBuilder()
            //        .GenerateFunctions(
            //            mappingContext,
            //            CreateStoreModel(EntityFrameworkVersion.Version2, storeFunction),
            //            new UniqueIdentifierService(),
            //            new UniqueIdentifierService()));
            //Assert.Empty(mappingContext.MappedStoreFunctions());
            //Assert.Empty(mappingContext.Errors);
         }

         [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
         public void GenerateFunctions_does_not_generate_functions_for_aggregate_or_non_composable_store_function() {
            //var testCases =
            //    new[]
            //        {
            //            new { IsComposable = true, IsAggregate = true },
            //            new { IsComposable = false, IsAggregate = false },
            //            new { IsComposable = false, IsAggregate = true }
            //        };

            //foreach (var testCase in testCases)
            //{
            //    var storeReturnType =
            //        CreateRowType(CreateProperty("foo", PrimitiveTypeKind.Int32)).GetCollectionType();
            //    var returnParameter =
            //        FunctionParameter.Create("ReturnType", storeReturnType, ParameterMode.ReturnValue);

            //    var storeFunction =
            //        EdmFunction.Create(
            //            "foo",
            //            "bar",
            //            DataSpace.SSpace,
            //            new EdmFunctionPayload
            //                {
            //                    ReturnParameters = new[] { returnParameter },
            //                    IsComposable = testCase.IsComposable,
            //                    IsAggregate = testCase.IsAggregate
            //                },
            //            null);

            //    var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
            //    Assert.Empty(
            //        CreateOneToOneMappingBuilder()
            //            .GenerateFunctions(
            //                mappingContext,
            //                CreateStoreModel(storeFunction),
            //                new UniqueIdentifierService(),
            //                new UniqueIdentifierService()));

            //    Assert.Empty(mappingContext.Errors);
            //}
         }

         [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
         public void GenerateFunctions_does_not_generate_function_imports_if_store_function_has_non_IN_parameter() {
            //foreach (var parameterMode in new[] { ParameterMode.InOut, ParameterMode.Out })
            //{
            //    var parameters =
            //        new[]
            //            {
            //                CreatePrimitiveParameter("numberParam", PrimitiveTypeKind.Int32, parameterMode),
            //                CreatePrimitiveParameter("numberParam1", PrimitiveTypeKind.Int32, ParameterMode.In)
            //            };

            //    var storeReturnType =
            //        CreateRowType(CreateProperty("foo", PrimitiveTypeKind.Int32)).GetCollectionType();
            //    var returnParameter =
            //        FunctionParameter.Create("ReturnType", storeReturnType, ParameterMode.ReturnValue);

            //    var storeFunction =
            //        EdmFunction.Create(
            //            "foo",
            //            "bar",
            //            DataSpace.SSpace,
            //            new EdmFunctionPayload
            //                {
            //                    Parameters = parameters,
            //                    ReturnParameters = new[] { returnParameter },
            //                    IsComposable = true,
            //                    IsAggregate = false
            //                },
            //            null);

            //    var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
            //    Assert.Empty(
            //        CreateOneToOneMappingBuilder()
            //            .GenerateFunctions(
            //                mappingContext,
            //                CreateStoreModel(storeFunction),
            //                new UniqueIdentifierService(),
            //                new UniqueIdentifierService()));
            //    Assert.Empty(mappingContext.Errors);
            //}
         }

         [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
         public void GenerateFunctions_generates_functions_for_valid_store_functions() {
            //var storeFunction =
            //    EdmFunction.Create(
            //        "foo",
            //        "bar",
            //        DataSpace.SSpace,
            //        new EdmFunctionPayload
            //            {
            //                Parameters =
            //                    new[]
            //                        {
            //                            CreatePrimitiveParameter(
            //                                "numberParam", PrimitiveTypeKind.Int32, ParameterMode.In)
            //                        },
            //                ReturnParameters =
            //                    new[]
            //                        {
            //                            FunctionParameter.Create(
            //                                "ReturnType",
            //                                CreateRowType(CreateProperty("foo", PrimitiveTypeKind.Int32)).GetCollectionType(),
            //                                ParameterMode.ReturnValue)
            //                        },
            //                IsComposable = true,
            //                IsAggregate = false
            //            },
            //        null);

            //var storeFunction1 =
            //    EdmFunction.Create(
            //        "foo",
            //        "bar",
            //        DataSpace.SSpace,
            //        new EdmFunctionPayload
            //            {
            //                Parameters =
            //                    new[]
            //                        {
            //                            CreatePrimitiveParameter(
            //                                "numberParam", PrimitiveTypeKind.Int32, ParameterMode.In)
            //                        },
            //                ReturnParameters =
            //                    new[]
            //                        {
            //                            FunctionParameter.Create(
            //                                "ReturnType",
            //                                CreateRowType(CreateProperty("foo", PrimitiveTypeKind.Int32)).GetCollectionType(),
            //                                ParameterMode.ReturnValue)
            //                        },
            //                IsComposable = true,
            //                IsAggregate = false
            //            },
            //        null);

            //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);

            //Assert.Equal(
            //    2,
            //    CreateOneToOneMappingBuilder()
            //        .GenerateFunctions(
            //            mappingContext,
            //            CreateStoreModel(storeFunction, storeFunction1),
            //            new UniqueIdentifierService(),
            //            new UniqueIdentifierService())
            //        .Count());
         }

         [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
         public void GenerateFunctions_does_not_add_invalid_functions_to_mapping_context() {
            //    var returnParameter =
            //        FunctionParameter.Create(
            //            "ReturnType",
            //            PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32),
            //            ParameterMode.ReturnValue);

            //    var storeFunction =
            //        EdmFunction.Create(
            //            "foo",
            //            "bar",
            //            DataSpace.SSpace,
            //            new EdmFunctionPayload { ReturnParameters = new[] { returnParameter } },
            //            null);

            //    var storeModel = new EdmModel(DataSpace.SSpace);
            //    storeModel.AddItem(storeFunction);

            //    var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
            //    Assert.Empty(
            //        CreateOneToOneMappingBuilder()
            //            .GenerateFunctions(
            //                mappingContext,
            //                storeModel,
            //                new UniqueIdentifierService(),
            //                new UniqueIdentifierService()));

            //    mappingContext.Errors.Count.Should().Be(1);
            //    Assert.Empty(mappingContext.MappedStoreFunctions());
            //}
         }

         private static OneToOneMappingBuilder CreateOneToOneMappingBuilder(
             string namespaceName = "myModel",
             string containerName = "myContainer",
             bool generateForeignKeyProperties = true) {
            return new OneToOneMappingBuilder(
                namespaceName,
                containerName,
                new EnglishPluralizationService(),
                generateForeignKeyProperties);
         }

         [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
         // When in doubt, use targetSchemaVersion == EntityFrameworkVersion.Version3
         private static EdmModel CreateStoreModel(Version targetSchemaVersion, params EdmFunction[] functions) {
            //var storeModel = new EdmModel(
            //    DataSpace.SSpace,
            //    EntityFrameworkVersion.VersionToDouble(targetSchemaVersion));

            //foreach (var function in functions)
            //{
            //    storeModel.AddItem(function);
            //}
            //return storeModel;

            return null;
         }

         private static PrimitiveType GetStoreEdmType(string typeName) {
            return ProviderManifest.GetStoreTypes().Single(t => t.Name == typeName);
         }

         private static EdmProperty CreateProperty(string propertyName, PrimitiveTypeKind propertyType) {
            return EdmProperty.Create(
                propertyName,
                ProviderManifest.GetStoreType(
                    TypeUsage.CreateDefaultTypeUsage(
                        PrimitiveType.GetEdmPrimitiveType(propertyType))));
         }

         private static RowType CreateRowType(params EdmProperty[] properties) {
            return RowType.Create(properties, null);
         }

         private static FunctionParameter CreatePrimitiveParameter(string name, PrimitiveTypeKind type, ParameterMode mode) {
            return
                FunctionParameter.Create(
                    name,
                    ProviderManifest.GetStoreType(
                        TypeUsage.CreateDefaultTypeUsage(
                            PrimitiveType.GetEdmPrimitiveType(type))).EdmType,
                    mode);
         }

         [TestClass]
    public class AssociationSetsTests
         {
            private static EdmModel BuildStoreModel(
                TableDetailsRow[] tableDetails,
                RelationshipDetailsRow[] relationshipDetails) {
                    StoreSchemaDetails storeSchemaDetails = new StoreSchemaDetails(
                   tableDetails,
                   new TableDetailsRow[0],
                   relationshipDetails,
                   new FunctionDetailsRowView[0],
                   new TableDetailsRow[0]);

               var storeModelBuilder = StoreModelBuilderTests.CreateStoreModelBuilder();

               return storeModelBuilder.Build(storeSchemaDetails);
            }

            [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
            public static void CreateCollapsibleItems_creates_collapsible_item() {
               //var tableDetails = new[]
               //    {
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "A", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "B", "Col1", 0, false, "int", isIdentity: false, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "C", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "C", "Col1", 1, false, "int", isIdentity: false, isPrimaryKey: true)
               //    };

               //var relationshipDetails = new[]
               //    {
               //        StoreModelBuilderTests.CreateRelationshipDetailsRow(
               //            "R1", "R1", 0, false, "catalog", "schema", "A", "Id", "catalog", "schema", "C", "Id"),
               //        StoreModelBuilderTests.CreateRelationshipDetailsRow(
               //            "R2", "R2", 0, false, "catalog", "schema", "B", "Col1", "catalog", "schema", "C", "Col1")
               //    };

               //var storeModel = BuildStoreModel(tableDetails, relationshipDetails);

               //IEnumerable<AssociationSet> associationSetsFromNonCollapsibleItems;
               //var collapsibleItems = CollapsibleEntityAssociationSets.CreateCollapsibleItems(
               //    storeModel.Containers.Single().BaseEntitySets,
               //    out associationSetsFromNonCollapsibleItems);

               //Assert.Equal(1, collapsibleItems.Count());
               //Assert.Equal(0, associationSetsFromNonCollapsibleItems.Count());

               //var item = collapsibleItems.FirstOrDefault();
               //item.EntitySet.Name.Should().Be("C");
               //item.AssociationSets.Count.Should().Be(2);
               //item.AssociationSets[0].Name.Should().Be("R1");
               //item.AssociationSets[1].Name.Should().Be("R2");
            }

            [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
            public static void CreateCollapsibleItems_does_not_create_collapsible_item_if_not_IsEntityDependentSideOfBothAssociations() {
               //var tableDetails = new[]
               //    {
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "A", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "B", "Col1", 0, false, "int", isIdentity: false, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "C", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "C", "Col1", 1, false, "int", isIdentity: false, isPrimaryKey: true)
               //    };

               //var relationshipDetails = new[]
               //    {
               //        StoreModelBuilderTests.CreateRelationshipDetailsRow(
               //            "R1", "R1", 0, false, "catalog", "schema", "A", "Id", "catalog", "schema", "C", "Id"),
               //        StoreModelBuilderTests.CreateRelationshipDetailsRow(
               //            "R2", "R2", 0, false, "catalog", "schema", "A", "Id", "catalog", "schema", "B", "Col1")
               //    };

               //var storeModel = BuildStoreModel(tableDetails, relationshipDetails);

               //IEnumerable<AssociationSet> associationSetsFromNonCollapsibleItems;
               //var collapsibleItems = CollapsibleEntityAssociationSets.CreateCollapsibleItems(
               //    storeModel.Containers.Single().BaseEntitySets,
               //    out associationSetsFromNonCollapsibleItems);

               //Assert.Equal(0, collapsibleItems.Count());
               //Assert.Equal(2, associationSetsFromNonCollapsibleItems.Count());
               //Assert.Equal("R1", associationSetsFromNonCollapsibleItems.ElementAtOrDefault(0).Name);
               //Assert.Equal("R2", associationSetsFromNonCollapsibleItems.ElementAtOrDefault(1).Name);
            }

            [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
            public static void
            CreateCollapsibleItems_does_not_create_collapsible_item_if_not_IsAtLeastOneColumnOfBothDependentRelationshipColumnSetsNonNullable
            () {
               //var tableDetails = new[]
               //    {
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "A", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "B", "Col1", 0, false, "int", isIdentity: false, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "C", "CId", 0, false, "int", isIdentity: true, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "C", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: false),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "C", "Col1", 1, true, "int", isIdentity: false, isPrimaryKey: false)
               //    };

               //var relationshipDetails = new[]
               //    {
               //        StoreModelBuilderTests.CreateRelationshipDetailsRow(
               //            "R1", "R1", 0, false, "catalog", "schema", "A", "Id", "catalog", "schema", "C", "Id"),
               //        StoreModelBuilderTests.CreateRelationshipDetailsRow(
               //            "R2", "R2", 0, false, "catalog", "schema", "B", "Col1", "catalog", "schema", "C", "Col1")
               //    };

               //var storeModel = BuildStoreModel(tableDetails, relationshipDetails);

               //IEnumerable<AssociationSet> associationSetsFromNonCollapsibleItems;
               //var collapsibleItems = CollapsibleEntityAssociationSets.CreateCollapsibleItems(
               //    storeModel.Containers.Single().BaseEntitySets,
               //    out associationSetsFromNonCollapsibleItems);

               //Assert.Equal(0, collapsibleItems.Count());
               //Assert.Equal(2, associationSetsFromNonCollapsibleItems.Count());
               //Assert.Equal("R1", associationSetsFromNonCollapsibleItems.ElementAtOrDefault(0).Name);
               //Assert.Equal("R2", associationSetsFromNonCollapsibleItems.ElementAtOrDefault(1).Name);
            }

            [TestMethod, Ignore("Different API Visibility between official dll and locally built")]

            public static void CreateCollapsibleItems_does_not_create_collapsible_item_if_not_AreAllEntityColumnsMappedAsToColumns() {
               //var tableDetails = new[]
               //    {
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "A", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "B", "Col1", 0, false, "int", isIdentity: false, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "C", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "C", "Col1", 1, false, "int", isIdentity: false, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "C", "Col2", 2, false, "int", isIdentity: false, isPrimaryKey: false)
               //    };

               //var relationshipDetails = new[]
               //    {
               //        StoreModelBuilderTests.CreateRelationshipDetailsRow(
               //            "R1", "R1", 0, false, "catalog", "schema", "A", "Id", "catalog", "schema", "C", "Id"),
               //        StoreModelBuilderTests.CreateRelationshipDetailsRow(
               //            "R2", "R2", 0, false, "catalog", "schema", "B", "Col1", "catalog", "schema", "C", "Col1")
               //    };

               //var storeModel = BuildStoreModel(tableDetails, relationshipDetails);

               //IEnumerable<AssociationSet> associationSetsFromNonCollapsibleItems;
               //var collapsibleItems = CollapsibleEntityAssociationSets.CreateCollapsibleItems(
               //    storeModel.Containers.Single().BaseEntitySets,
               //    out associationSetsFromNonCollapsibleItems);

               //Assert.Equal(0, collapsibleItems.Count());
               //Assert.Equal(2, associationSetsFromNonCollapsibleItems.Count());
               //Assert.Equal("R1", associationSetsFromNonCollapsibleItems.ElementAtOrDefault(0).Name);
               //Assert.Equal("R2", associationSetsFromNonCollapsibleItems.ElementAtOrDefault(1).Name);
            }

            [TestMethod, Ignore("Different API Visibility between official dll and locally built")]

            public static void CreateCollapsibleItems_does_not_create_collapsible_item_if_IsAtLeastOneColumnFkInBothAssociations() {
               //var tableDetails = new[]
               //    {
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "A", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "A", "Col1", 1, false, "int", isIdentity: false, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "B", "Col1", 0, false, "int", isIdentity: false, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "B", "Col2", 1, false, "int", isIdentity: false, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "C", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "C", "Col1", 1, false, "int", isIdentity: false, isPrimaryKey: true),
               //        StoreModelBuilderTests.CreateRow(
               //            "catalog", "schema", "C", "Col2", 2, false, "int", isIdentity: false, isPrimaryKey: true)
               //    };

               //var relationshipDetails = new[]
               //    {
               //        StoreModelBuilderTests.CreateRelationshipDetailsRow(
               //            "R1", "R1", 0, false, "catalog", "schema", "A", "Id", "catalog", "schema", "C", "Id"),
               //        StoreModelBuilderTests.CreateRelationshipDetailsRow(
               //            "R1", "R1", 1, false, "catalog", "schema", "A", "Col1", "catalog", "schema", "C", "Col1"),
               //        StoreModelBuilderTests.CreateRelationshipDetailsRow(
               //            "R2", "R2", 0, false, "catalog", "schema", "B", "Col1", "catalog", "schema", "C", "Col1"),
               //        StoreModelBuilderTests.CreateRelationshipDetailsRow(
               //            "R2", "R2", 1, false, "catalog", "schema", "B", "Col2", "catalog", "schema", "C", "Col2")
               //    };

               //var storeSchemaDetails = new StoreSchemaDetails(
               //    tableDetails,
               //    new TableDetailsRow[0],
               //    relationshipDetails,
               //    new FunctionDetailsRowView[0],
               //    new TableDetailsRow[0]);

               //var storeModelBuilder = StoreModelBuilderTests.CreateStoreModelBuilder(
               //    "System.Data.SqlClient",
               //    "2008",
               //    null,
               //    "myModel",
               //    generateForeignKeyProperties: true);

               //var storeModel = storeModelBuilder.Build(storeSchemaDetails);

               //IEnumerable<AssociationSet> associationSetsFromNonCollapsibleItems;
               //var collapsibleItems = CollapsibleEntityAssociationSets.CreateCollapsibleItems(
               //    storeModel.Containers.Single().BaseEntitySets,
               //    out associationSetsFromNonCollapsibleItems);

               //Assert.Equal(0, collapsibleItems.Count());
               //Assert.Equal(2, associationSetsFromNonCollapsibleItems.Count());
               //Assert.Equal("R1", associationSetsFromNonCollapsibleItems.ElementAtOrDefault(0).Name);
               //Assert.Equal("R2", associationSetsFromNonCollapsibleItems.ElementAtOrDefault(1).Name);
            }

            [TestMethod, Ignore("Different API Visibility between official dll and locally built")]

            public static void GenerateAssociationSets_from_store_association_sets_creates_expected_mappings() {
               var tableDetails = new[]
                   {
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "A", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "B", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true)
                    };

               var relationshipDetails = new[]
                   {
                        StoreModelBuilderTests.CreateRelationshipDetailsRow(
                            "R1", "R1", 0, false, "catalog", "schema", "A", "Id", "catalog", "schema", "B", "Id")
                    };

               var storeModel = BuildStoreModel(tableDetails, relationshipDetails);

               var mappingContext = CreateOneToOneMappingBuilder().Build(storeModel);

               mappingContext.StoreAssociationTypes().Count().Should().Be(1);
               mappingContext.StoreAssociationSets().Count().Should().Be(1);
               mappingContext.StoreAssociationEndMembers().Count().Should().Be(2);
               mappingContext.StoreAssociationSetEnds().Count().Should().Be(2);

               var storeAssociationType = mappingContext.StoreAssociationTypes().ElementAt(0);
               var storeAssociationSet = mappingContext.StoreAssociationSets().ElementAt(0);
               var storeAssociationEndMember0 = mappingContext.StoreAssociationEndMembers().ElementAt(0);
               var storeAssociationEndMember1 = mappingContext.StoreAssociationEndMembers().ElementAt(1);
               var storeAssociationSetEnd0 = mappingContext.StoreAssociationSetEnds().ElementAt(0);
               var storeAssociationSetEnd1 = mappingContext.StoreAssociationSetEnds().ElementAt(1);

               mappingContext.ConceptualAssociationTypes().ElementAt(0).Should().BeSameAs(mappingContext[storeAssociationType]);
               mappingContext.ConceptualAssociationSets().ElementAt(0).Should().BeSameAs(mappingContext[storeAssociationSet]);
               mappingContext.ConceptualAssociationEndMembers().ElementAt(0).Should().BeSameAs(mappingContext[storeAssociationEndMember0]);
               mappingContext.ConceptualAssociationEndMembers().ElementAt(1).Should().BeSameAs(mappingContext[storeAssociationEndMember1]);
               mappingContext.ConceptualAssociationSetEnds().ElementAt(0).Should().BeSameAs(mappingContext[storeAssociationSetEnd0]);
               mappingContext.ConceptualAssociationSetEnds().ElementAt(1).Should().BeSameAs(mappingContext[storeAssociationSetEnd1]);
               mappingContext.StoreForeignKeyProperties.Should().Equal(new[] { storeAssociationType.ReferentialConstraints[0].ToProperties[0] });
            }

            [TestMethod]
            public static void GenerateAssociationSets_from_collapsible_items_creates_expected_mappings() {
               var tableDetails = new[]
                   {
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "A", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "B", "Col1", 0, false, "int", isIdentity: false, isPrimaryKey: true),
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "C", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "C", "Col1", 1, false, "int", isIdentity: false, isPrimaryKey: true)
                    };

               var relationshipDetails = new[]
                   {
                        StoreModelBuilderTests.CreateRelationshipDetailsRow(
                            "R1", "R1", 0, false, "catalog", "schema", "A", "Id", "catalog", "schema", "C", "Id"),
                        StoreModelBuilderTests.CreateRelationshipDetailsRow(
                            "R2", "R2", 0, false, "catalog", "schema", "B", "Col1", "catalog", "schema", "C", "Col1")
                    };

               var storeModel = BuildStoreModel(tableDetails, relationshipDetails);

               var mappingContext = CreateOneToOneMappingBuilder().Build(storeModel);

               mappingContext.StoreAssociationTypes().Count().Should().Be(0);
               mappingContext.StoreAssociationSets().Count().Should().Be(0);
               mappingContext.StoreAssociationEndMembers().Count().Should().Be(2);
               mappingContext.StoreAssociationSetEnds().Count().Should().Be(2);

               var storeAssociationEndMember0 = mappingContext.StoreAssociationEndMembers().ElementAt(0);
               var storeAssociationEndMember1 = mappingContext.StoreAssociationEndMembers().ElementAt(1);
               var storeAssociationSetEnd0 = mappingContext.StoreAssociationSetEnds().ElementAt(0);
               var storeAssociationSetEnd1 = mappingContext.StoreAssociationSetEnds().ElementAt(1);

               mappingContext.ConceptualAssociationEndMembers().ElementAt(0).Should().BeSameAs(mappingContext[storeAssociationEndMember0]);
               mappingContext.ConceptualAssociationEndMembers().ElementAt(1).Should().BeSameAs(mappingContext[storeAssociationEndMember1]);
               mappingContext.ConceptualAssociationSetEnds().ElementAt(0).Should().BeSameAs(mappingContext[storeAssociationSetEnd0]);
               mappingContext.ConceptualAssociationSetEnds().ElementAt(1).Should().BeSameAs(mappingContext[storeAssociationSetEnd1]);

               // the mapping for the collapsed entity set should be replaced with a mapping to a conceptual association set
               mappingContext.ConceptualEntitySets().All(e => e.Name != "C").Should().BeTrue();
               mappingContext.ConceptualAssociationSets().Count().Should().Be(1);

               mappingContext.StoreForeignKeyProperties.Select(p => p.Name).Should().Equal(new[] { "Id", "Col1" });
            }

            [TestMethod]
            public static void GenerateAssociationSets_from_store_association_sets_creates_expected_instances() {
               var tableDetails = new[]
                   {
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "A", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "B", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true)
                    };

               var relationshipDetails = new[]
                   {
                        StoreModelBuilderTests.CreateRelationshipDetailsRow(
                            "R1", "R1", 0, false, "catalog", "schema", "A", "Id", "catalog", "schema", "B", "Id")
                    };

               var storeModel = BuildStoreModel(tableDetails, relationshipDetails);

               var mappingContext = CreateOneToOneMappingBuilder().Build(storeModel);

               mappingContext.ConceptualAssociationTypes().Count().Should().Be(1);
               mappingContext.ConceptualAssociationSets().Count().Should().Be(1);
               mappingContext.ConceptualAssociationEndMembers().Count().Should().Be(2);
               mappingContext.ConceptualAssociationSetEnds().Count().Should().Be(2);

               var associationType = mappingContext.ConceptualAssociationTypes().ElementAt(0);
               var associationSet = mappingContext.ConceptualAssociationSets().ElementAt(0);
               var associationEndMember0 = mappingContext.ConceptualAssociationEndMembers().ElementAt(0);
               var associationEndMember1 = mappingContext.ConceptualAssociationEndMembers().ElementAt(1);
               var associationSetEnd0 = mappingContext.ConceptualAssociationSetEnds().ElementAt(0);
               var associationSetEnd1 = mappingContext.ConceptualAssociationSetEnds().ElementAt(1);

               associationType.Name.Should().Be("R1");
               associationType.IsForeignKey.Should().BeTrue();
               associationType.ReferentialConstraints.Count.Should().Be(1);
               associationType.AssociationEndMembers.Count.Should().Be(2);

               var constraint = associationType.ReferentialConstraints[0];
               constraint.FromRole.Should().Be(associationEndMember0);
               constraint.ToRole.Should().Be(associationEndMember1);

               associationEndMember0.Name.Should().Be("A");
               associationEndMember0.RelationshipMultiplicity.Should().Be(RelationshipMultiplicity.One);
               associationEndMember0.DeleteBehavior.Should().Be(OperationAction.None);
               associationType.AssociationEndMembers[0].Should().BeSameAs(associationEndMember0);

               associationEndMember1.Name.Should().Be("B");
               associationEndMember1.RelationshipMultiplicity.Should().Be(RelationshipMultiplicity.ZeroOrOne);
               associationEndMember1.DeleteBehavior.Should().Be(OperationAction.None);
               associationType.AssociationEndMembers[1].Should().BeSameAs(associationEndMember1);

               associationSet.Name.Should().Be("R1");
               associationSet.AssociationSetEnds.Count.Should().Be(2);

               associationSetEnd0.Name.Should().Be("A");
               associationSet.AssociationSetEnds[0].Should().BeSameAs(associationSetEnd0);
               associationSetEnd1.Name.Should().Be("B");
               associationSet.AssociationSetEnds[1].Should().BeSameAs(associationSetEnd1);

               mappingContext.StoreForeignKeyProperties.Should().Equal(new[] { mappingContext.StoreAssociationTypes().Single().ReferentialConstraints[0].ToProperties[0] });
            }

            [TestMethod]
            public static void GenerateAssociationSets_from_collapsible_items_creates_expected_instances() {
               var tableDetails = new[]
                   {
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "A", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "B", "Col1", 0, false, "int", isIdentity: false, isPrimaryKey: true),
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "C", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "C", "Col1", 1, false, "int", isIdentity: false, isPrimaryKey: true)
                    };

               var relationshipDetails = new[]
                   {
                        StoreModelBuilderTests.CreateRelationshipDetailsRow(
                            "R1", "R1", 0, false, "catalog", "schema", "A", "Id", "catalog", "schema", "C", "Id"),
                        StoreModelBuilderTests.CreateRelationshipDetailsRow(
                            "R2", "R2", 0, false, "catalog", "schema", "B", "Col1", "catalog", "schema", "C", "Col1")
                    };

               var storeModel = BuildStoreModel(tableDetails, relationshipDetails);

               var mappingContext = CreateOneToOneMappingBuilder().Build(storeModel);

               mappingContext.ConceptualContainers().Count().Should().Be(1);
               mappingContext.ConceptualAssociationTypes().Count().Should().Be(0);
               mappingContext.ConceptualAssociationTypes().Count().Should().Be(0);
               mappingContext.ConceptualAssociationSets().Count().Should().Be(1);
               mappingContext.ConceptualAssociationEndMembers().Count().Should().Be(2);
               mappingContext.ConceptualAssociationSetEnds().Count().Should().Be(2);

               var container = mappingContext.ConceptualContainers().ElementAt(0);
               container.AssociationSets.Count().Should().Be(1);

               var associationSet = container.AssociationSets.ElementAt(0);
               var associationType = associationSet.ElementType;
               var associationEndMember0 = mappingContext.ConceptualAssociationEndMembers().ElementAt(0);
               var associationEndMember1 = mappingContext.ConceptualAssociationEndMembers().ElementAt(1);
               var associationSetEnd0 = mappingContext.ConceptualAssociationSetEnds().ElementAt(0);
               var associationSetEnd1 = mappingContext.ConceptualAssociationSetEnds().ElementAt(1);

               associationType.Name.Should().Be("C");
               associationType.IsForeignKey.Should().BeFalse();
               associationType.ReferentialConstraints.Count.Should().Be(0);
               associationType.AssociationEndMembers.Count.Should().Be(2);

               associationEndMember0.Name.Should().Be("A");
               associationEndMember0.RelationshipMultiplicity.Should().Be(RelationshipMultiplicity.Many);
               associationEndMember0.DeleteBehavior.Should().Be(OperationAction.None);
               associationType.AssociationEndMembers[0].Should().BeSameAs(associationEndMember0);

               associationEndMember1.Name.Should().Be("B");
               associationEndMember1.RelationshipMultiplicity.Should().Be(RelationshipMultiplicity.Many);
               associationEndMember1.DeleteBehavior.Should().Be(OperationAction.None);
               associationType.AssociationEndMembers[1].Should().BeSameAs(associationEndMember1);

               associationSet.Name.Should().Be("C");
               associationSet.AssociationSetEnds.Count.Should().Be(2);

               associationSetEnd0.Name.Should().Be("A");
               associationSet.AssociationSetEnds[0].Should().BeSameAs(associationSetEnd0);
               associationSetEnd1.Name.Should().Be("B");
               associationSet.AssociationSetEnds[1].Should().BeSameAs(associationSetEnd1);

               mappingContext.StoreForeignKeyProperties.Select(p => p.Name).Should().Equal(new[] { "Id", "Col1" });
            }

            [TestMethod]
            public static void GenerateAssociationType_from_store_association_type_creates_FK_association_in_Version3() {
               var tableDetails = new[]
                   {
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "A", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "B", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true)
                    };

               var relationshipDetails = new[]
                   {
                        StoreModelBuilderTests.CreateRelationshipDetailsRow(
                            "R1", "R1", 0, false, "catalog", "schema", "A", "Id", "catalog", "schema", "B", "Id")
                    };

                    StoreSchemaDetails storeSchemaDetails = new StoreSchemaDetails(
                   tableDetails,
                   new TableDetailsRow[0],
                   relationshipDetails,
                   new FunctionDetailsRowView[0],
                   new TableDetailsRow[0]);

               var storeModelBuilder = StoreModelBuilderTests.CreateStoreModelBuilder(
                   "System.Data.SqlClient",
                   "2008",
                   EntityFrameworkVersion.Version3);

               var storeModel = storeModelBuilder.Build(storeSchemaDetails);

               var mappingContext = CreateOneToOneMappingBuilder().Build(storeModel);

               mappingContext.ConceptualAssociationTypes().Count().Should().Be(1);

               var associationType = mappingContext.ConceptualAssociationTypes().ElementAt(0);

               // In Version3, associations are created as FK associations
               associationType.IsForeignKey.Should().BeTrue();
               associationType.ReferentialConstraints.Count.Should().Be(1);

               mappingContext.StoreForeignKeyProperties.Should().Equal(new[] { mappingContext.StoreAssociationTypes().Single().ReferentialConstraints[0].ToProperties[0] });
            }

            [TestMethod]
            public static void
                GenerateAssociationType_from_store_association_type_creates_non_FK_association_if_does_not_require_referential_constraint() {
               var tableDetails = new[]
                   {
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "A", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "B", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
                        StoreModelBuilderTests.CreateRow(
                            "catalog", "schema", "B", "Col1", 0, false, "int", isIdentity: false, isPrimaryKey: false)
                    };

               var relationshipDetails = new[]
                   {
                        StoreModelBuilderTests.CreateRelationshipDetailsRow(
                            "R1", "R1", 0, false, "catalog", "schema", "A", "Id", "catalog", "schema", "B", "Col1")
                    };

               var storeModel = BuildStoreModel(tableDetails, relationshipDetails);

               var mappingContext = CreateOneToOneMappingBuilder(generateForeignKeyProperties: false).Build(storeModel);

               mappingContext.ConceptualAssociationTypes().Count().Should().Be(1);

               var associationType = mappingContext.ConceptualAssociationTypes().ElementAt(0);

               associationType.IsForeignKey.Should().BeFalse();
               associationType.ReferentialConstraints.Count.Should().Be(0);

               mappingContext.StoreForeignKeyProperties.Should().Equal(new[] { mappingContext.StoreAssociationTypes().Single().ReferentialConstraints[0].ToProperties[0] });
            }
         }

         private static MetadataProperty GetAnnotationMetadataProperty(MetadataItem item, string metadataPropertyName) {
            return item
                .MetadataProperties
                .SingleOrDefault(
                    p => p.Name == "http://schemas.microsoft.com/ado/2009/02/edm/annotation:" + metadataPropertyName);
         }
      }
   }
}
