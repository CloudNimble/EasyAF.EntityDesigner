// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Core.Metadata.Edm;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb
{
   public partial class OneToOneMappingBuilderTests
    {
        private static DbProviderManifest ProviderManifest =>
            Utils.SqlProviderServicesInstance.GetProviderManifest("2008");

        [TestClass]
    public class BuildTests
        {
            [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
            public void Build_creates_mapping_context_populated_with_items_created_from_store_model_items()
            {
                //var storeEntityType =
                //    EntityType.Create(
                //        "foo", "bar", DataSpace.SSpace, new[] { "Id" },
                //        new[] { EdmProperty.CreatePrimitive("Id", GetStoreEdmType("int")) }, null);

                //var storeEntitySet = EntitySet.Create("foo", "bar", null, null, storeEntityType, null);

                //var rowTypeProperty = CreateProperty("p1", PrimitiveTypeKind.Int32);

                //var storeFunction =
                //    EdmFunction.Create(
                //        "f", "bar", DataSpace.SSpace,
                //        new EdmFunctionPayload
                //            {
                //                IsComposable = true,
                //                IsFunctionImport = false,
                //                ReturnParameters =
                //                    new[]
                //                        {
                //                            FunctionParameter.Create(
                //                                "ReturnType",
                //                                RowType.Create(new[] { rowTypeProperty }, null).GetCollectionType(),
                //                                ParameterMode.ReturnValue)
                //                        },
                //            }, null);

                //var storeContainer =
                //    EntityContainer.Create("storeContainer", DataSpace.SSpace, new[] { storeEntitySet }, null, null);

                //var storeModel = EdmModel.CreateStoreModel(storeContainer, null, null, 3.0);
                //storeModel.AddItem(storeFunction);

                //var mappingContext =
                //    CreateOneToOneMappingBuilder(containerName: "edmContainer")
                //        .Build(storeModel);

                //mappingContext.Should().NotBeNull();
                //Assert.Empty(mappingContext.Errors);
                //mappingContext[storeEntitySet].Should().NotBeNull();
                //mappingContext[storeEntityType].Should().NotBeNull();

                //var modelContainer = mappingContext[storeContainer];
                //modelContainer.Should().NotBeNull();
                //modelContainer.Name.Should().Be("edmContainer");

                //var entitySet = modelContainer.EntitySets.Single();
                //mappingContext[storeEntitySet].Should().BeSameAs(entitySet);
                //entitySet.Name.Should().Be("foos");

                //var entityType = entitySet.ElementType;
                //mappingContext[storeEntityType].Should().BeSameAs(entityType);
                //entityType.Name.Should().Be("foo");

                //mappingContext[rowTypeProperty].Should().NotBeNull();
                //mappingContext[rowTypeProperty].Name.Should().Be("p1");

                //mappingContext[storeFunction].Should().NotBeNull();
            }

            [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
            public void Build_creates_mapping_context_with_container_with_function_imports_from_store_model()
            {
                //var storeFunction =
                //    EdmFunction.Create(
                //        "foo",
                //        "bar",
                //        DataSpace.SSpace,
                //        new EdmFunctionPayload
                //            {
                //                ReturnParameters =
                //                    new[]
                //                        {
                //                            FunctionParameter.Create(
                //                                "ReturnType",
                //                                CreateRowType(CreateProperty("foo", PrimitiveTypeKind.Int32))
                //                                    .GetCollectionType(),
                //                                ParameterMode.ReturnValue)
                //                        }
                //            },
                //        null);

                //var storeFunction1 =
                //    EdmFunction.Create(
                //        "foo",
                //        "bar",
                //        DataSpace.SSpace,
                //        new EdmFunctionPayload
                //            {
                //                ReturnParameters =
                //                    new[]
                //                        {
                //                            FunctionParameter.Create(
                //                                "ReturnType",
                //                                CreateRowType(CreateProperty("foo", PrimitiveTypeKind.Int32))
                //                                    .GetCollectionType(),
                //                                ParameterMode.ReturnValue)
                //                        }
                //            },
                //        null);

                //var storeModel = EdmModel.CreateStoreModel(
                //    EntityContainer.Create(
                //        "storeContainer",
                //        DataSpace.SSpace,
                //        null,
                //        null,
                //        null),
                //    null,
                //    null);

                //storeModel.AddItem(storeFunction);
                //storeModel.AddItem(storeFunction1);

                //var mappingContext =
                //    CreateOneToOneMappingBuilder(namespaceName: "myModel")
                //        .Build(storeModel);

                //mappingContext.Should().NotBeNull();
                //var conceptualContainer = mappingContext[storeModel.Containers.Single()];
                //conceptualContainer.FunctionImports.Count.Should().Be(2);
                //Assert.Equal(new[] { "foo", "foo1" }, conceptualContainer.FunctionImports.Select(f => f.Name));
            }

            [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
            public void Build_adds_lazyLoading_metadata_property_to_v2_and_v3_CSpace_containers()
            {
                //GetLazyLoadingMetadataProperty(EntityFrameworkVersion.Version1.Should().BeNull());

                //var lazyLoadingMetadataProperty =
                //    GetLazyLoadingMetadataProperty(EntityFrameworkVersion.Version2);
                //lazyLoadingMetadataProperty.Should().NotBeNull();
                //Assert.Equal("true", (string)lazyLoadingMetadataProperty.Value);

                //lazyLoadingMetadataProperty =
                //    GetLazyLoadingMetadataProperty(EntityFrameworkVersion.Version3);
                //lazyLoadingMetadataProperty.Should().NotBeNull();
                //Assert.Equal("true", (string)lazyLoadingMetadataProperty.Value);
            }

            [TestMethod, Ignore("API Differences between official dll and locally built one")]
            private static MetadataProperty GetLazyLoadingMetadataProperty(Version targetSchemaVersion)
            {
                //    var storeModel =
                //        new EdmModel(
                //            DataSpace.SSpace, EntityFrameworkVersion.VersionToDouble(targetSchemaVersion));

                //    var mappingContext =
                //        new OneToOneMappingBuilder("ns", "container", null, true)
                //            .Build(storeModel);

                //    return GetAnnotationMetadataProperty(
                //        mappingContext.ConceptualContainers().Single(),
                //        "LazyLoadingEnabled");
                //}
                return null;
            }
        }

        [TestClass]
    public class GenerateEntitySetTests
        {
            [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
            public void GenerateEntitySet_creates_model_entity_set_for_store_entity_set()
            {
                //    var storeEntityType =
                //        EntityType.Create(
                //            "foo", "bar", DataSpace.SSpace, new[] { "Id" },
                //            new[] { EdmProperty.CreatePrimitive("Id", GetStoreEdmType("int")) }, null);

                //    var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
                //    var storeEntitySet = EntitySet.Create("foo", "bar", null, null, storeEntityType, null);

                //    CreateOneToOneMappingBuilder()
                //        .GenerateEntitySet(
                //            mappingContext,
                //            storeEntitySet,
                //            new UniqueIdentifierService(),
                //            new UniqueIdentifierService());

                //    var conceptualModelEntitySet = mappingContext[storeEntitySet];

                //    conceptualModelEntitySet.Name.Should().Be("foos");
                //    conceptualModelEntitySet.ElementType.Name.Should().Be("foo");

                //    mappingContext[storeEntitySet].Should().BeSameAs(conceptualModelEntitySet);
                //
            }

            [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
            public void GenerateEntitySet_entity_set_name_sanitized_and_uniquified()
            {
                //var storeEntityType =
                //    EntityType.Create(
                //        "foo", "bar", DataSpace.SSpace, new[] { "Id" },
                //        new[] { EdmProperty.CreatePrimitive("Id", GetStoreEdmType("int")) }, null);

                //var storeEntitySet = EntitySet.Create("foo$", "bar", null, null, storeEntityType, null);

                //var uniqueEntityContainerNames = new UniqueIdentifierService();
                //uniqueEntityContainerNames.AdjustIdentifier("foo_");

                //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);

                //CreateOneToOneMappingBuilder()
                //    .GenerateEntitySet(
                //        mappingContext,
                //        storeEntitySet,
                //        uniqueEntityContainerNames,
                //        new UniqueIdentifierService());

                //var conceptualModelEntitySet = mappingContext[storeEntitySet];

                //conceptualModelEntitySet.Name.Should().Be("foo_1");
                //conceptualModelEntitySet.ElementType.Name.Should().Be("foo");
            }
        }

        [TestClass]
    public class GenerateEntityTypeTests
        {
            [TestMethod, Ignore("Different API   Visibility between official dll and locally built one")]
            public void GenerateEntityType_creates_CSpace_entity_from_SSpace_entity()
            {
                //var storeEntityType =
                //    EntityType.Create(
                //        "foo", "bar", DataSpace.SSpace, new[] { "Id1", "Id2" },
                //        new[]
                //            {
                //                EdmProperty.CreatePrimitive("Id1", GetStoreEdmType("int")),
                //                EdmProperty.CreatePrimitive("Id2", GetStoreEdmType("int")),
                //                EdmProperty.CreatePrimitive("Name_", GetStoreEdmType("nvarchar")),
                //                EdmProperty.CreatePrimitive("Name$", GetStoreEdmType("nvarchar"))
                //            }, null);

                //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);

                //var conceptualEntityType =
                //    CreateOneToOneMappingBuilder().GenerateEntityType(mappingContext, storeEntityType, new UniqueIdentifierService());

                //conceptualEntityType.Name.Should().Be(storeEntityType.Name);
                //conceptualEntityType.NamespaceName.Should().Be("myModel");
                //Assert.Equal(
                //    new[] { "Id1", "Id2", "Name_", "Name_1" },
                //    conceptualEntityType.Properties.Select(p => p.Name).ToArray());

                //Assert.Equal(
                //    storeEntityType.KeyMembers.Select(p => p.Name),
                //    conceptualEntityType.KeyMembers.Select(p => p.Name).ToArray());

                //mappingContext[storeEntityType].Should().BeSameAs(conceptualEntityType);
            }

            [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
            public void GenerateEntityType_renames_property_whose_name_is_the_same_as_owning_entity_type()
            {
                //var storeEntityType =
                //    EntityType.Create(
                //        "foo", "bar", DataSpace.SSpace, new[] { "foo" },
                //        new[] { EdmProperty.CreatePrimitive("foo", GetStoreEdmType("int")) }, null);

                //var conceptualEntityType =
                //    CreateOneToOneMappingBuilder()
                //        .GenerateEntityType(
                //            new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true),
                //            storeEntityType,
                //            new UniqueIdentifierService());

                //conceptualEntityType.Name.Should().Be(storeEntityType.Name);
                //Assert.Equal(
                //    new[] { "foo1" },
                //    conceptualEntityType.Properties.Select(p => p.Name).ToArray());
            }

            [TestMethod, Ignore("Different API Visibility between official dll and locally built one")]
            public void GenerateEntityType_entity_type_name_is_sanitized_and_uniquified()
            {
                //var storeEntityType =
                //    EntityType.Create(
                //        "foo$", "bar", DataSpace.SSpace, new[] { "Id" },
                //        new[] { EdmProperty.CreatePrimitive("Id", GetStoreEdmType("int")) }, null);

                //var uniqueEntityTypeName = new UniqueIdentifierService();
                //uniqueEntityTypeName.AdjustIdentifier("foo_");
                //var conceptualEntityType =
                //    CreateOneToOneMappingBuilder()
                //        .GenerateEntityType(
                //            new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true),
                //            storeEntityType,
                //            uniqueEntityTypeName);

                //conceptualEntityType.Name.Should().Be("foo_1");
            }

            [TestMethod, Ignore("Different API Visibility between official dll and locally built one")]
            public void GenerateEntityType_singularizes_entity_type_name()
            {
                //var storeEntityType =
                //    EntityType.Create(
                //        "Entities", "bar", DataSpace.SSpace, new[] { "Id" },
                //        new[] { EdmProperty.CreatePrimitive("Id", GetStoreEdmType("int")) }, null);

                //var conceptualEntityType =
                //    CreateOneToOneMappingBuilder()
                //        .GenerateEntityType(
                //            new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true),
                //            storeEntityType,
                //            new UniqueIdentifierService());

                //conceptualEntityType.Name.Should().Be("Entity");
            }

            [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
            public void Property_for_foreign_key_added_if_foreign_keys_enabled()
            {
                //var foreignKeyColumn = EdmProperty.CreatePrimitive("ForeignKeyColumn", GetStoreEdmType("int"));

                //var storeEntityType =
                //    EntityType.Create(
                //        "foo", "bar", DataSpace.SSpace, new[] { "Id" },
                //        new[]
                //            {
                //                EdmProperty.CreatePrimitive("Id", GetStoreEdmType("int")),
                //                foreignKeyColumn,
                //            }, null);

                //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
                //mappingContext.StoreForeignKeyProperties.Add(foreignKeyColumn);

                //var conceptualEntityType =
                //    CreateOneToOneMappingBuilder(generateForeignKeyProperties: true)
                //        .GenerateEntityType(mappingContext, storeEntityType, new UniqueIdentifierService());

                //Assert.Equal(new[] { "Id", "ForeignKeyColumn" }, conceptualEntityType.Properties.Select(p => p.Name));
                //storeEntityType.Properties.Any(p => mappingContext[p] == null.Should().BeFalse());
            }

            [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
            public void Property_for_foreign_key_not_added_if_property_is_not_key_and_foreign_keys_disabled()
            {
                //var foreignKeyColumn = EdmProperty.CreatePrimitive("ForeignKeyColumn", GetStoreEdmType("int"));

                //var storeEntityType =
                //    EntityType.Create(
                //        "foo", "bar", DataSpace.SSpace, new[] { "Id" },
                //        new[]
                //            {
                //                EdmProperty.CreatePrimitive("Id", GetStoreEdmType("int")),
                //                foreignKeyColumn,
                //            }, null);

                //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
                //mappingContext.StoreForeignKeyProperties.Add(foreignKeyColumn);

                //var conceptualEntityType =
                //    CreateOneToOneMappingBuilder(generateForeignKeyProperties: false)
                //        .GenerateEntityType(mappingContext, storeEntityType, new UniqueIdentifierService());

                //Assert.Equal(new[] { "Id" }, conceptualEntityType.Properties.Select(p => p.Name));

                //// the mapping still should be added to be able to build association type mapping correctly
                //storeEntityType.Properties.Any(p => mappingContext[p] == null.Should().BeFalse());
            }

            [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
            public void Property_for_foreign_key_added_if_property_is_key_even_when_foreign_keys_disabled()
            {
                //var storeEntityType =
                //    EntityType.Create(
                //        "foo", "bar", DataSpace.SSpace, new[] { "IdPrimaryAndForeignKey" },
                //        new[]
                //            {
                //                EdmProperty.CreatePrimitive("IdPrimaryAndForeignKey", GetStoreEdmType("int")),
                //            }, null);

                //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
                //mappingContext.StoreForeignKeyProperties.Add(storeEntityType.Properties.Single());

                //var conceptualEntityType =
                //    CreateOneToOneMappingBuilder(generateForeignKeyProperties: false)
                //        .GenerateEntityType(mappingContext, storeEntityType, new UniqueIdentifierService());

                //Assert.Equal(new[] { "IdPrimaryAndForeignKey" }, conceptualEntityType.Properties.Select(p => p.Name));

                //// the mapping still should be added to be able to build association type mapping correctly
                //storeEntityType.Properties.Any(p => mappingContext[p] == null.Should().BeFalse());
            }
        }

        [TestClass]
    public class GenerateScalarPropertyTests
        {
            [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
            public void GenerateScalarProperty_creates_CSpace_property_from_SSpace_property()
            {
                //var storeProperty = EdmProperty.CreatePrimitive("p1", GetStoreEdmType("int"));

                //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
                //var conceptualProperty =
                //    OneToOneMappingBuilder
                //        .GenerateScalarProperty(mappingContext, storeProperty, new UniqueIdentifierService());

                //storeProperty.Name.Should().Be(conceptualProperty.Name);
                //Assert.Equal(conceptualProperty.TypeUsage.EdmType, PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32));
                //GetAnnotationMetadataProperty(conceptualProperty, "StoreGeneratedPattern".Should().BeNull());
                //mappingContext[storeProperty].Should().NotBeNull();
            }

            [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
            public void GenerateScalarProperty_adds_StoreGeneratedPattern_annotation_if_needed()
            {
                //var storeProperty = EdmProperty.CreatePrimitive("p1", GetStoreEdmType("int"));
                //storeProperty.StoreGeneratedPattern = StoreGeneratedPattern.Identity;

                //var mappingContext = new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true);
                //var conceptualProperty =
                //    OneToOneMappingBuilder
                //        .GenerateScalarProperty(mappingContext, storeProperty, new UniqueIdentifierService());

                //storeProperty.Name.Should().Be(conceptualProperty.Name);
                //Assert.Equal(conceptualProperty.TypeUsage.EdmType, PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32));
                //var storeGeneratedPatternMetadataProperty =
                //    GetAnnotationMetadataProperty(conceptualProperty, "StoreGeneratedPattern");
                //storeGeneratedPatternMetadataProperty.Should().NotBeNull();
                //storeGeneratedPatternMetadataProperty.Value.Should().Be("Identity");
                //mappingContext[storeProperty].Should().NotBeNull();
            }

            [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
            public void GenerateScalarProperty_converts_and_uniquifies_property_names()
            {
                //var uniquePropertyNameService = new UniqueIdentifierService();
                //uniquePropertyNameService.AdjustIdentifier("p_1");

                //var storeProperty = EdmProperty.CreatePrimitive("p*1", GetStoreEdmType("int"));

                //var conceptualProperty =
                //    OneToOneMappingBuilder
                //        .GenerateScalarProperty(
                //            new SimpleMappingContext(new EdmModel(DataSpace.SSpace), true),
                //            storeProperty,
                //            uniquePropertyNameService);

                //conceptualProperty.Name.Should().Be("p_11");
            }
        }
    }
}
