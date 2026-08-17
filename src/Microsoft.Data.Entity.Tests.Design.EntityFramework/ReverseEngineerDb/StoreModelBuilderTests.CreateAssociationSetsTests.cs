// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Data.Entity.Core.Metadata.Edm;
using Microsoft.Data.Entity.Design.VersioningFacade;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.ReverseEngineerDb
{
    public partial class StoreModelBuilderTests
    {
        [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
        public void CreateAssociationSets_creates_expected_association_types_and_sets()
        {
            //var tableDetails = new[]
            //    {
            //        CreateRow(
            //            "catalog", "schema", "source1", "Id", 0, isNullable: false, dataType: "int", isIdentity: true, isPrimaryKey: true)
            //        ,
            //        CreateRow(
            //            "catalog", "schema", "source1", "Other", 1, isNullable: false, dataType: "int", isIdentity: false,
            //            isPrimaryKey: true),
            //        CreateRow("catalog", "schema", "target1", "Id", 0, isNullable: false, dataType: "int", isIdentity: true),
            //        CreateRow("catalog", "schema", "target1", "Other", 1, isNullable: false, dataType: "int", isIdentity: false),
            //        CreateRow(
            //            "catalog", "schema", "source2", "Id", 0, isNullable: false, dataType: "int", isIdentity: true, isPrimaryKey: true)
            //        ,
            //        CreateRow("catalog", "schema", "target2", "Id", 0, isNullable: false, dataType: "int", isIdentity: true)
            //    };

            //var relationshipDetails = new List<RelationshipDetailsRow>
            //    {
            //        CreateRelationshipDetailsRow(
            //            "RelationshipId1", "name1", 0, false, "catalog", "schema", "source1", "Id", "catalog", "schema", "target1", "Id"),
            //        CreateRelationshipDetailsRow(
            //            "RelationshipId1", "name1", 1, false, "catalog", "schema", "source1", "Other", "catalog", "schema", "target1",
            //            "Other"),
            //        CreateRelationshipDetailsRow(
            //            "RelationshipId2", "name2", 0, false, "catalog", "schema", "source2", "Id", "catalog", "schema", "target2", "Id")
            //    };

            //var storeModelBuilder = CreateStoreModelBuilder();

            //var entityRegister = new StoreModelBuilder.EntityRegister();
            //var entityTypes = entityRegister.EntityTypes;
            //var entitySets = entityRegister.EntitySets;
            //storeModelBuilder.CreateEntitySets(tableDetails, new TableDetailsRow[0], entityRegister);

            //var associationTypes = new List<AssociationType>();
            //var associationSets = storeModelBuilder.CreateAssociationSets(relationshipDetails, entityRegister, associationTypes);

            //associationTypes.Count.Should().Be(2);
            //associationSets.Count.Should().Be(2);

            //var associationType1 = associationTypes[0];
            //var associationType2 = associationTypes[1];

            //associationType1.FullName.Should().Be("myModel.name1");
            //associationType2.FullName.Should().Be("myModel.name2");
            //associationType1.MetadataProperties.SingleOrDefault(p => p.Name == "EdmSchemaErrors").Should().BeNull();
            //associationType2.MetadataProperties.SingleOrDefault(p => p.Name == "EdmSchemaErrors").Should().BeNull();
            //MetadataItemHelper.IsInvalid(associationType1).Should().BeFalse();
            //MetadataItemHelper.IsInvalid(associationType2).Should().BeFalse();
        }

        [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
        public void CreateAssociationSets_does_not_create_set_for_shared_foreign_key()
        {
            //var tableDetails = new[]
            //    {
            //        CreateRow("catalog", "schema", "source1", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
            //        CreateRow("catalog", "schema", "source2", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
            //        CreateRow("catalog", "schema", "target", "Id", 0, false, "int", isIdentity: true)
            //    };

            //var relationshipDetails = new List<RelationshipDetailsRow>
            //    {
            //        CreateRelationshipDetailsRow(
            //            "RelationshipId1", "name1", 0, false, "catalog", "schema", "source1", "Id", "catalog", "schema", "target", "Id"),
            //        CreateRelationshipDetailsRow(
            //            "RelationshipId2", "name2", 0, false, "catalog", "schema", "source2", "Id", "catalog", "schema", "target", "Id"),
            //    };

            //var storeModelBuilder = CreateStoreModelBuilder();

            //var entityRegister = new StoreModelBuilder.EntityRegister();
            //storeModelBuilder.CreateEntitySets(tableDetails, new TableDetailsRow[0], entityRegister);

            //var associationTypes = new List<AssociationType>();
            //var associationSets = storeModelBuilder.CreateAssociationSets(relationshipDetails, entityRegister, associationTypes);

            //associationTypes.Count.Should().Be(2);
            //associationSets.Count.Should().Be(1);

            //var associationType1 = associationTypes[0];
            //var associationType2 = associationTypes[1];

            //MetadataItemHelper.IsInvalid(associationType1).Should().BeFalse();
            //MetadataItemHelper.IsInvalid(associationType2).Should().BeTrue();

            //associationType1.MetadataProperties.SingleOrDefault(p => p.Name == "EdmSchemaErrors").Should().BeNull();

            //var metaProperty = associationType2.MetadataProperties.SingleOrDefault(p => p.Name == "EdmSchemaErrors");

            //metaProperty.Should().NotBeNull();

            //var errors = metaProperty.Value as List<EdmSchemaError>;

            //errors.Should().NotBeNull();
            //errors.Count.Should().Be(1);

            //var error = errors[0];

            //error.Severity.Should().Be(EdmSchemaErrorSeverity.Warning);
            //error.ErrorCode.Should().Be((int)ModelBuilderErrorCode.SharedForeignKey);
        }

        [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
        public void TryCreateAssociationSet_creates_valid_association_type_and_set()
        {
            //var tableDetails = new[]
            //    {
            //        CreateRow("catalog", "schema", "source", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
            //        CreateRow("catalog", "schema", "target", "Id", 0, false, "int", isIdentity: true)
            //    };

            //var relationshipDetails = new List<RelationshipDetailsRow>
            //    {
            //        CreateRelationshipDetailsRow(
            //            "RelationshipId", "name", 0, false, "catalog", "schema", "source", "Id", "catalog", "schema", "target", "Id")
            //    };

            //var storeModelBuilder = CreateStoreModelBuilder();

            //var entityRegister = new StoreModelBuilder.EntityRegister();
            //var entityTypes = entityRegister.EntityTypes;
            //var entitySets = entityRegister.EntitySets;
            //storeModelBuilder.CreateEntitySets(tableDetails, new TableDetailsRow[0], entityRegister);

            //var associationTypes = new List<AssociationType>();
            //var associationSet = storeModelBuilder.TryCreateAssociationSet(relationshipDetails, entityRegister, associationTypes);

            //associationTypes.Count.Should().Be(1);
            //associationSet.Should().NotBeNull();

            //var associationType = associationTypes[0];

            //associationType.Should().NotBeNull();
            //associationType.AssociationEndMembers.Count.Should().Be(2);
            //associationType.Constraint.Should().NotBeNull();
            //MetadataItemHelper.IsInvalid(associationType).Should().BeFalse();
            //associationType.MetadataProperties.SingleOrDefault(p => p.Name == "EdmSchemaErrors").Should().BeNull();

            //var sourceEnd = associationType.AssociationEndMembers.FirstOrDefault();
            //var targetEnd = associationType.AssociationEndMembers.ElementAtOrDefault(1);

            //sourceEnd.GetEntityType().Should().Be(entityTypes[0]);
            //targetEnd.GetEntityType().Should().Be(entityTypes[1]);

            //var sourceEndSet = associationSet.AssociationSetEnds.FirstOrDefault();
            //var targetEndSet = associationSet.AssociationSetEnds.ElementAtOrDefault(1);

            //sourceEndSet.EntitySet.Should().Be(entitySets[0]);
            //targetEndSet.EntitySet.Should().Be(entitySets[1]);
        }

        [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
        public void TryCreateAssociationSet_does_not_create_set_if_end_entity_is_missing()
        {
            Check_does_not_create_set_if_end_entity_is_missing(sourceMissing: true, targetMissing: false);
            Check_does_not_create_set_if_end_entity_is_missing(sourceMissing: false, targetMissing: true);
            Check_does_not_create_set_if_end_entity_is_missing(sourceMissing: true, targetMissing: true);
        }

        [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
        private void Check_does_not_create_set_if_end_entity_is_missing(bool sourceMissing, bool targetMissing)
        {
            //var tableDetails = new[]
            //    {
            //        CreateRow("catalog", "schema", "source", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
            //        CreateRow("catalog", "schema", "target", "Id", 0, false, "int", isIdentity: true)
            //    };

            //var sourceColumn = sourceMissing ? "missing" : "source";
            //var targetColumn = targetMissing ? "missing" : "target";

            //var relationshipDetails = new List<RelationshipDetailsRow>
            //    {
            //        CreateRelationshipDetailsRow(
            //            "RelationshipId", "name", 0, false, "catalog", "schema", sourceColumn, "Id", "catalog", "schema", targetColumn,
            //            "Id")
            //    };

            //var storeModelBuilder = CreateStoreModelBuilder();

            //var entityRegister = new StoreModelBuilder.EntityRegister();
            //storeModelBuilder.CreateEntitySets(tableDetails, new TableDetailsRow[0], entityRegister);

            //var associationTypes = new List<AssociationType>();
            //var associationSet = storeModelBuilder.TryCreateAssociationSet(relationshipDetails, entityRegister, associationTypes);

            //associationTypes.Count.Should().Be(1);
            //associationSet.Should().BeNull();

            //var associationType = associationTypes[0];

            //associationType.Should().NotBeNull();
            //associationType.AssociationEndMembers.Count.Should().Be(0);
            //associationType.Constraint.Should().BeNull();
            //MetadataItemHelper.IsInvalid(associationType).Should().BeTrue();

            //var metaProperty = associationType.MetadataProperties.SingleOrDefault(p => p.Name == "EdmSchemaErrors");

            //metaProperty.Should().NotBeNull();

            //var errors = metaProperty.Value as List<EdmSchemaError>;
            //var expectedCount = (sourceMissing ? 1 : 0) + (targetMissing ? 1 : 0);

            //errors.Should().NotBeNull();
            //errors.Count.Should().Be(expectedCount);

            //foreach (var error in errors)
            //{
            //    error.Severity.Should().Be(EdmSchemaErrorSeverity.Error);
            //    error.ErrorCode.Should().Be((int)ModelBuilderErrorCode.MissingEntity);
            //}
        }

        [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
        public void TryCreateAssociationSet_does_not_create_set_if_relationship_column_count_does_not_match()
        {
            //var tableDetails = new[]
            //    {
            //        CreateRow("catalog", "schema", "source", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
            //        CreateRow("catalog", "schema", "source", "Other", 1, false, "int", isIdentity: false, isPrimaryKey: true),
            //        CreateRow("catalog", "schema", "target", "Id", 0, false, "int", isIdentity: true)
            //    };

            //var relationshipDetails = new List<RelationshipDetailsRow>
            //    {
            //        CreateRelationshipDetailsRow(
            //            "RelationshipId", "name", 0, false, "catalog", "schema", "source", "Id", "catalog", "schema", "target", "Id")
            //    };

            //var storeModelBuilder = CreateStoreModelBuilder();

            //var entityRegister = new StoreModelBuilder.EntityRegister();
            //storeModelBuilder.CreateEntitySets(tableDetails, new TableDetailsRow[0], entityRegister);

            //var associationTypes = new List<AssociationType>();
            //var associationSet = storeModelBuilder.TryCreateAssociationSet(relationshipDetails, entityRegister, associationTypes);

            //associationTypes.Count.Should().Be(1);
            //associationSet.Should().BeNull();

            //var associationType = associationTypes[0];

            //associationType.Should().NotBeNull();
            //associationType.AssociationEndMembers.Count.Should().Be(0);
            //associationType.Constraint.Should().BeNull();
            //MetadataItemHelper.IsInvalid(associationType).Should().BeTrue();

            //var metaProperty = associationType.MetadataProperties.SingleOrDefault(p => p.Name == "EdmSchemaErrors");

            //metaProperty.Should().NotBeNull();

            //var errors = metaProperty.Value as List<EdmSchemaError>;

            //errors.Should().NotBeNull();
            //errors.Count.Should().Be(1);

            //var error = errors[0];

            //error.Severity.Should().Be(EdmSchemaErrorSeverity.Warning);
            //error.ErrorCode.Should().Be((int)ModelBuilderErrorCode.UnsupportedDbRelationship);
        }

        [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
        public void TryCreateAssociationSet_does_not_create_set_if_relationship_column_name_does_not_match()
        {
            //var tableDetails = new[]
            //    {
            //        CreateRow("catalog", "schema", "source", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
            //        CreateRow("catalog", "schema", "target", "Id", 0, false, "int", isIdentity: true)
            //    };

            //var relationshipDetails = new List<RelationshipDetailsRow>
            //    {
            //        CreateRelationshipDetailsRow(
            //            "RelationshipId", "name", 0, false, "catalog", "schema", "source", "Mismatch", "catalog", "schema", "target",
            //            "Id")
            //    };

            //var storeModelBuilder = CreateStoreModelBuilder();

            //var entityRegister = new StoreModelBuilder.EntityRegister();
            //storeModelBuilder.CreateEntitySets(tableDetails, new TableDetailsRow[0], entityRegister);

            //var associationTypes = new List<AssociationType>();
            //var associationSet = storeModelBuilder.TryCreateAssociationSet(relationshipDetails, entityRegister, associationTypes);

            //associationTypes.Count.Should().Be(1);
            //associationSet.Should().BeNull();

            //var associationType = associationTypes[0];

            //associationType.Should().NotBeNull();
            //associationType.AssociationEndMembers.Count.Should().Be(0);
            //associationType.Constraint.Should().BeNull();
            //MetadataItemHelper.IsInvalid(associationType).Should().BeTrue();

            //var metaProperty = associationType.MetadataProperties.SingleOrDefault(p => p.Name == "EdmSchemaErrors");

            //metaProperty.Should().NotBeNull();

            //var errors = metaProperty.Value as List<EdmSchemaError>;

            //errors.Should().NotBeNull();
            //errors.Count.Should().Be(1);

            //var error = errors[0];

            //error.Severity.Should().Be(EdmSchemaErrorSeverity.Warning);
            //error.ErrorCode.Should().Be((int)ModelBuilderErrorCode.UnsupportedDbRelationship);
        }

        [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
        public void TryCreateAssociationSet_does_not_create_set_if_fk_is_partially_contained_in_pk()
        {
            //var tableDetails = new[]
            //    {
            //        CreateRow("catalog", "schema", "source", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
            //        CreateRow("catalog", "schema", "source", "Other", 1, false, "int", isIdentity: false, isPrimaryKey: true),
            //        CreateRow("catalog", "schema", "target", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
            //        CreateRow("catalog", "schema", "target", "Other", 1, false, "int", isIdentity: false, isPrimaryKey: false)
            //    };

            //var relationshipDetails = new List<RelationshipDetailsRow>
            //    {
            //        CreateRelationshipDetailsRow(
            //            "RelationshipId", "name", 0, false, "catalog", "schema", "source", "Id", "catalog", "schema", "target", "Id"),
            //        CreateRelationshipDetailsRow(
            //            "RelationshipId", "name", 0, false, "catalog", "schema", "source", "Other", "catalog", "schema", "target",
            //            "Other")
            //    };

            //var storeModelBuilder = CreateStoreModelBuilder();

            //var entityRegister = new StoreModelBuilder.EntityRegister();
            //storeModelBuilder.CreateEntitySets(tableDetails, new TableDetailsRow[0], entityRegister);

            //var associationTypes = new List<AssociationType>();
            //var associationSet = storeModelBuilder.TryCreateAssociationSet(relationshipDetails, entityRegister, associationTypes);

            //associationTypes.Count.Should().Be(1);
            //associationSet.Should().BeNull();

            //var associationType = associationTypes[0];

            //associationType.Should().NotBeNull();
            //associationType.AssociationEndMembers.Count.Should().Be(2);
            //associationType.Constraint.Should().NotBeNull();
            //MetadataItemHelper.IsInvalid(associationType).Should().BeTrue();

            //var metaProperty = associationType.MetadataProperties.SingleOrDefault(p => p.Name == "EdmSchemaErrors");

            //metaProperty.Should().NotBeNull();

            //var errors = metaProperty.Value as List<EdmSchemaError>;

            //errors.Should().NotBeNull();
            //errors.Count.Should().Be(1);

            //var error = errors[0];

            //error.Severity.Should().Be(EdmSchemaErrorSeverity.Warning);
            //error.ErrorCode.Should().Be((int)ModelBuilderErrorCode.UnsupportedForeignKeyPattern);
        }

        [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
        public void TryCreateAssociationSet_does_not_create_set_if_association_is_missing_key_column()
        {
            //var tableDetails = new[]
            //    {
            //        CreateRow("catalog", "schema", "source", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
            //        CreateRow("catalog", "schema", "source", "Other", 1, false, "int", isIdentity: false, isPrimaryKey: true),
            //        CreateRow("catalog", "schema", "target", "Id", 0, false, "int", isIdentity: true)
            //    };

            //var relationshipDetails = new List<RelationshipDetailsRow>
            //    {
            //        CreateRelationshipDetailsRow(
            //            "RelationshipId", "name", 0, false, "catalog", "schema", "source", "Id", "catalog", "schema", "target", "Id"),
            //        CreateRelationshipDetailsRow(
            //            "RelationshipId", "name", 1, false, "catalog", "schema", "source", "Other", "catalog", "schema", "target",
            //            "Other")
            //    };

            //var storeModelBuilder = CreateStoreModelBuilder();

            //var entityRegister = new StoreModelBuilder.EntityRegister();
            //storeModelBuilder.CreateEntitySets(tableDetails, new TableDetailsRow[0], entityRegister);

            //var associationTypes = new List<AssociationType>();
            //var associationSet = storeModelBuilder.TryCreateAssociationSet(relationshipDetails, entityRegister, associationTypes);

            //associationTypes.Count.Should().Be(1);
            //associationSet.Should().BeNull();

            //var associationType = associationTypes[0];

            //associationType.Should().NotBeNull();
            //associationType.AssociationEndMembers.Count.Should().Be(2);
            //associationType.Constraint.Should().BeNull();
            //MetadataItemHelper.IsInvalid(associationType).Should().BeTrue();

            //var metaProperty = associationType.MetadataProperties.SingleOrDefault(p => p.Name == "EdmSchemaErrors");

            //metaProperty.Should().NotBeNull();

            //var errors = metaProperty.Value as List<EdmSchemaError>;

            //errors.Should().NotBeNull();
            //errors.Count.Should().Be(1);

            //var error = errors[0];

            //error.Severity.Should().Be(EdmSchemaErrorSeverity.Warning);
            //error.ErrorCode.Should().Be((int)ModelBuilderErrorCode.AssociationMissingKeyColumn);
        }

        [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
        public void TryCreateAssociationSet_expected_association_end_multiplicity_pk_to_pk()
        {
            Check_two_column_relationship_expected_association_end_multiplicity_pk_to_pk(
                EntityFrameworkVersion.Version3);
        }

        [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
        private void Check_two_column_relationship_expected_association_end_multiplicity_pk_to_pk(
            Version targetEntityFrameworkVersion)
        {
            //var tableDetails = new[]
            //    {
            //        CreateRow(
            //            "catalog", "schema", "source", "Id", 0, isNullable: false, dataType: "int", isIdentity: true,
            //            isPrimaryKey: true),
            //        CreateRow(
            //            "catalog", "schema", "source", "Other", 1, isNullable: false, dataType: "int", isIdentity: false,
            //            isPrimaryKey: true),
            //        CreateRow(
            //            "catalog", "schema", "target", "Id", 0, isNullable: false, dataType: "int", isIdentity: true,
            //            isPrimaryKey: true),
            //        CreateRow(
            //            "catalog", "schema", "target", "Other", 1, isNullable: false, dataType: "int", isIdentity: false,
            //            isPrimaryKey: true)
            //    };

            //var relationshipDetails = new List<RelationshipDetailsRow>
            //    {
            //        CreateRelationshipDetailsRow(
            //            "RelationshipId", "name", 0, false, "catalog", "schema", "source", "Id", "catalog", "schema", "target", "Id"),
            //        CreateRelationshipDetailsRow(
            //            "RelationshipId", "name", 1, false, "catalog", "schema", "source", "Other", "catalog", "schema", "target",
            //            "Other")
            //    };

            //var storeModelBuilder = CreateStoreModelBuilder("System.Data.SqlClient", "2008", targetEntityFrameworkVersion);

            //var entityRegister = new StoreModelBuilder.EntityRegister();
            //storeModelBuilder.CreateEntitySets(tableDetails, new TableDetailsRow[0], entityRegister);

            //var associationTypes = new List<AssociationType>();
            //var associationSet = storeModelBuilder.TryCreateAssociationSet(relationshipDetails, entityRegister, associationTypes);

            //associationTypes.Count.Should().Be(1);
            //associationSet.Should().NotBeNull();

            //var associationType = associationTypes[0];

            //MetadataItemHelper.IsInvalid(associationType).Should().BeFalse();
            //associationType.MetadataProperties.SingleOrDefault(p => p.Name == "EdmSchemaErrors").Should().BeNull();

            //var sourceEnd = associationType.AssociationEndMembers.FirstOrDefault();
            //var targetEnd = associationType.AssociationEndMembers.ElementAtOrDefault(1);

            //sourceEnd.RelationshipMultiplicity.Should().Be(RelationshipMultiplicity.One);
            //targetEnd.RelationshipMultiplicity.Should().Be(RelationshipMultiplicity.ZeroOrOne);
        }

        [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
        public void TryCreateAssociationSet_expected_association_end_multiplicity_pk_to_fk()
        {
            // Version3 behavior: multiplicity is ZeroOrOne when any FK column is nullable
            Check_two_column_relationship_expected_association_end_multiplicity_pk_to_fk(
                EntityFrameworkVersion.Version3,
                column1Nullable: true,
                column2Nullable: true,
                expectedSourceEndMultiplicity: RelationshipMultiplicity.ZeroOrOne,
                expectedTargetEndMultiplicity: RelationshipMultiplicity.Many);

            Check_two_column_relationship_expected_association_end_multiplicity_pk_to_fk(
                EntityFrameworkVersion.Version3,
                column1Nullable: false,
                column2Nullable: true,
                expectedSourceEndMultiplicity: RelationshipMultiplicity.ZeroOrOne,
                expectedTargetEndMultiplicity: RelationshipMultiplicity.Many);

            Check_two_column_relationship_expected_association_end_multiplicity_pk_to_fk(
                EntityFrameworkVersion.Version3,
                column1Nullable: false,
                column2Nullable: false,
                expectedSourceEndMultiplicity: RelationshipMultiplicity.One,
                expectedTargetEndMultiplicity: RelationshipMultiplicity.Many);
        }

        [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
        private void Check_two_column_relationship_expected_association_end_multiplicity_pk_to_fk(
            Version targetEntityFrameworkVersion,
            bool column1Nullable,
            bool column2Nullable,
            RelationshipMultiplicity expectedSourceEndMultiplicity,
            RelationshipMultiplicity expectedTargetEndMultiplicity)
        {
            //var tableDetails = new[]
            //    {
            //        CreateRow(
            //            "catalog", "schema", "source", "Id", 0, isNullable: false, dataType: "int", isIdentity: true,
            //            isPrimaryKey: true),
            //        CreateRow(
            //            "catalog", "schema", "source", "Other", 1, isNullable: false, dataType: "int", isIdentity: false,
            //            isPrimaryKey: true),
            //        CreateRow(
            //            "catalog", "schema", "target", "TargetId", 0, isNullable: false, dataType: "int", isIdentity: true,
            //            isPrimaryKey: true),
            //        CreateRow(
            //            "catalog", "schema", "target", "Id", 0, isNullable: column1Nullable, dataType: "int", isIdentity: true,
            //            isPrimaryKey: false),
            //        CreateRow(
            //            "catalog", "schema", "target", "Other", 1, isNullable: column2Nullable, dataType: "int", isIdentity: false,
            //            isPrimaryKey: false)
            //    };

            //var relationshipDetails = new List<RelationshipDetailsRow>
            //    {
            //        CreateRelationshipDetailsRow(
            //            "RelationshipId", "name", 0, false, "catalog", "schema", "source", "Id", "catalog", "schema", "target", "Id"),
            //        CreateRelationshipDetailsRow(
            //            "RelationshipId", "name", 1, false, "catalog", "schema", "source", "Other", "catalog", "schema", "target",
            //            "Other")
            //    };

            //var storeModelBuilder = CreateStoreModelBuilder("System.Data.SqlClient", "2008", targetEntityFrameworkVersion);

            //var entityRegister = new StoreModelBuilder.EntityRegister();
            //storeModelBuilder.CreateEntitySets(tableDetails, new TableDetailsRow[0], entityRegister);

            //var associationTypes = new List<AssociationType>();
            //var associationSet = storeModelBuilder.TryCreateAssociationSet(relationshipDetails, entityRegister, associationTypes);

            //associationTypes.Count.Should().Be(1);
            //associationSet.Should().NotBeNull();

            //var associationType = associationTypes[0];

            //MetadataItemHelper.IsInvalid(associationType).Should().BeFalse();
            //associationType.MetadataProperties.SingleOrDefault(p => p.Name == "EdmSchemaErrors").Should().BeNull();

            //var sourceEnd = associationType.AssociationEndMembers.FirstOrDefault();
            //var targetEnd = associationType.AssociationEndMembers.ElementAtOrDefault(1);

            //sourceEnd.RelationshipMultiplicity.Should().Be(expectedSourceEndMultiplicity);
            //targetEnd.RelationshipMultiplicity.Should().Be(expectedTargetEndMultiplicity);
        }

        [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
        public void TryCreateAssociationSet_cascade_delete_flag_is_reflected_by_delete_behavior()
        {
            Check_cascade_delete_flag_is_reflected_by_delete_behavior(
                isCascadeDelete: false, expectedDeleteBehavior: OperationAction.None);
            Check_cascade_delete_flag_is_reflected_by_delete_behavior(
                isCascadeDelete: true, expectedDeleteBehavior: OperationAction.Cascade);
        }

        [TestMethod, Ignore("Different API Visibility between official dll and locally built")]
        private void Check_cascade_delete_flag_is_reflected_by_delete_behavior(
            bool isCascadeDelete, OperationAction expectedDeleteBehavior)
        {
            //var tableDetails = new[]
            //    {
            //        CreateRow("catalog", "schema", "source", "Id", 0, false, "int", isIdentity: true, isPrimaryKey: true),
            //        CreateRow("catalog", "schema", "target", "Id", 0, false, "int", isIdentity: true)
            //    };

            //var relationshipDetails = new List<RelationshipDetailsRow>
            //    {
            //        CreateRelationshipDetailsRow(
            //            "RelationshipId", "name", 0, isCascadeDelete, "catalog", "schema", "source", "Id", "catalog", "schema", "target",
            //            "Id")
            //    };

            //var storeModelBuilder = CreateStoreModelBuilder();

            //var entityRegister = new StoreModelBuilder.EntityRegister();
            //storeModelBuilder.CreateEntitySets(tableDetails, new TableDetailsRow[0], entityRegister);

            //var associationTypes = new List<AssociationType>();
            //var associationSet = storeModelBuilder.TryCreateAssociationSet(relationshipDetails, entityRegister, associationTypes);

            //associationTypes.Count.Should().Be(1);
            //associationSet.Should().NotBeNull();

            //var associationType = associationTypes[0];

            //MetadataItemHelper.IsInvalid(associationType).Should().BeFalse();
            //associationType.MetadataProperties.SingleOrDefault(p => p.Name == "EdmSchemaErrors").Should().BeNull();

            //var sourceEnd = associationType.AssociationEndMembers.FirstOrDefault();
            //var targetEnd = associationType.AssociationEndMembers.ElementAtOrDefault(1);

            //sourceEnd.DeleteBehavior.Should().Be(expectedDeleteBehavior);
            //targetEnd.DeleteBehavior.Should().Be(OperationAction.None);
        }
    }
}
