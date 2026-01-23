// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using LegacyCommandTrees = System.Data.Common.CommandTrees;
using LegacyMetadata = System.Data.Metadata.Edm;

namespace Microsoft.Data.Entity.Tests.Design.VersioningFacade.LegacyProviderWrapper
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity.Core.Common.CommandTrees;
    using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Linq;
    using Microsoft.Data.Entity.Design.VersioningFacade.LegacyProviderWrapper.LegacyMetadataExtensions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

    [TestClass]
    public class LegacyDbExpressionConverterTests
    {
        private readonly LegacyDbExpressionConverter _legacyDbExpressionConverter;
        private readonly StoreItemCollection _storeItemCollection;
        private readonly LegacyMetadata.StoreItemCollection _legacyStoreItemCollection;

        public LegacyDbExpressionConverterTests()
        {
            const string ssdl =
                "<Schema Namespace='AdventureWorksModel.Store' Provider='System.Data.SqlClient' ProviderManifestToken='2008' xmlns='http://schemas.microsoft.com/ado/2009/11/edm/ssdl'>"
                +
                "  <EntityContainer Name='AdventureWorksModelStoreContainer'>" +
                "    <EntitySet Name='EntitiesSet' EntityType='AdventureWorksModel.Store.Entities' Schema='dbo' />" +
                "    <EntitySet Name='OtherEntitiesSet' EntityType='AdventureWorksModel.Store.OtherEntities' Schema='dbo' />" +
                "  </EntityContainer>" +
                "  <EntityType Name='Entities'>" +
                "    <Key>" +
                "      <PropertyRef Name='Id' />" +
                "    </Key>" +
                "    <Property Name='Id' Type='int' StoreGeneratedPattern='Identity' Nullable='false' />" +
                "    <Property Name='Name' Type='nvarchar(max)' Nullable='false' />" +
                "  </EntityType>" +
                "  <EntityType Name='OtherEntities'>" +
                "    <Key>" +
                "      <PropertyRef Name='Id' />" +
                "    </Key>" +
                "    <Property Name='Id' Type='int' StoreGeneratedPattern='Identity' Nullable='false' />" +
                "    <Property Name='Name' Type='nvarchar(max)' Nullable='false' />" +
                "  </EntityType>" +
                "</Schema>";

            _storeItemCollection = Utils.CreateStoreItemCollection(ssdl);
            _legacyStoreItemCollection = _storeItemCollection.ToLegacyStoreItemCollection();
            _legacyDbExpressionConverter = new LegacyDbExpressionConverter(_legacyStoreItemCollection);
        }

        [TestMethod]
        public void Visit_DbConstantExpression_creates_equivalent_legacy_DbConstantExpression()
        {
            var typeUsage =
                TypeUsage.CreateStringTypeUsage(
                    PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String),
                    isUnicode: false,
                    isFixedLength: true,
                    maxLength: 1000);

            var constantExpression = typeUsage.Constant("test");

            var legacyConstantExpression =
                _legacyDbExpressionConverter.Visit(constantExpression) as LegacyCommandTrees.DbConstantExpression;

            legacyConstantExpression.Should().NotBeNull();
            legacyConstantExpression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Constant);
            legacyConstantExpression.Value.Should().Be(constantExpression.Value);

            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyConstantExpression.ResultType, constantExpression.ResultType);
        }

        [TestMethod]
        public void
            Visit_DbVariableReferenceExpression_creates_equivalent_legacy_DbVariableReferenceExpression_for_CSpace_type()
        {
            var variableReference =
                TypeUsage
                    .CreateDefaultTypeUsage(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32))
                    .Variable("variable");

            var legacyVariableReference =
                _legacyDbExpressionConverter.Visit(variableReference) as
                LegacyCommandTrees.DbVariableReferenceExpression;

            legacyVariableReference.Should().NotBeNull();
            legacyVariableReference.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.VariableReference);
            legacyVariableReference.VariableName.Should().Be(variableReference.VariableName);

            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyVariableReference.ResultType, variableReference.ResultType);
        }

        [TestMethod]
        public void
            Visit_DbVariableReferenceExpression_creates_equivalent_legacy_DbVariableReferenceExpression_for_SSpace_type()
        {
            var variableReference =
                TypeUsage
                    .CreateDefaultTypeUsage(
                        _storeItemCollection.GetItems<EntityType>().Single(e => e.Name == "Entities"))
                    .Variable("variable");

            var legacyVariableReference =
                _legacyDbExpressionConverter.Visit(variableReference) as
                LegacyCommandTrees.DbVariableReferenceExpression;

            legacyVariableReference.Should().NotBeNull();
            legacyVariableReference.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.VariableReference);
            legacyVariableReference.VariableName.Should().Be(variableReference.VariableName);

            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyVariableReference.ResultType, variableReference.ResultType);
        }

        [TestMethod]
        public void Visit_DbScanExpression_creates_equivalent_legacy_DbScanExpression()
        {
            var scanExpression =
                _storeItemCollection
                    .GetEntityContainer("AdventureWorksModelStoreContainer")
                    .EntitySets.Single(e => e.Name == "EntitiesSet")
                    .Scan();

            var legacyScanExpression =
                _legacyDbExpressionConverter.Visit(scanExpression) as LegacyCommandTrees.DbScanExpression;

            legacyScanExpression.Should().NotBeNull();
            legacyScanExpression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Scan);
            legacyScanExpression.Target.Name.Should().Be(scanExpression.Target.Name);

            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyScanExpression.ResultType, scanExpression.ResultType);
        }

        [TestMethod]
        public void Visit_DbPropertyExpression_creates_equivalent_legacy_DbPropertyExpression()
        {
            var propertyExpression =
                TypeUsage
                    .CreateDefaultTypeUsage(
                        _storeItemCollection.GetItems<EntityType>().Single(e => e.Name == "Entities"))
                    .Variable("Table")
                    .Property("Id");

            var legacyPropertyExpression =
                _legacyDbExpressionConverter.Visit(propertyExpression) as LegacyCommandTrees.DbPropertyExpression;

            legacyPropertyExpression.Should().NotBeNull();
            legacyPropertyExpression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Property);
            Assert.Equal(
                LegacyCommandTrees.DbExpressionKind.VariableReference,
                legacyPropertyExpression.Instance.ExpressionKind);
            Assert.Equal(
                "Table",
                ((LegacyCommandTrees.DbVariableReferenceExpression)legacyPropertyExpression.Instance)
                    .VariableName);
            legacyPropertyExpression.Property.Name.Should().Be("Id");

            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyPropertyExpression.Property.TypeUsage, propertyExpression.Property.TypeUsage);

            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyPropertyExpression.ResultType, propertyExpression.ResultType);
        }

        [TestMethod]
        public void Visit_DbNewInstanceExpression_collection_creates_equivalent_legacy_DbNewInstanceExpression()
        {
            var propertyExpression =
                TypeUsage
                    .CreateDefaultTypeUsage(
                        _storeItemCollection.GetItems<EntityType>().Single(e => e.Name == "Entities"))
                    .Variable("Table")
                    .Property("Id");

            var newInstanceExpressionCollection =
                DbExpressionBuilder.NewCollection(propertyExpression, DbExpressionBuilder.Constant(42));

            var legacyNewInstanceExpressionCollection =
                _legacyDbExpressionConverter.Visit(newInstanceExpressionCollection) as
                LegacyCommandTrees.DbNewInstanceExpression;

            legacyNewInstanceExpressionCollection.Should().NotBeNull();
            Assert.Equal(
                LegacyCommandTrees.DbExpressionKind.NewInstance,
                legacyNewInstanceExpressionCollection.ExpressionKind);
            legacyNewInstanceExpressionCollection.Arguments.Count.Should().Be(2);
            Assert.IsType<LegacyCommandTrees.DbPropertyExpression>(legacyNewInstanceExpressionCollection.Arguments[0]);
            Assert.IsType<LegacyCommandTrees.DbConstantExpression>(legacyNewInstanceExpressionCollection.Arguments[1]);

            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyNewInstanceExpressionCollection.ResultType, newInstanceExpressionCollection.ResultType);
        }

        [TestMethod]
        public void Visit_DbNewInstanceExpression_rowtype_creates_equivalent_legacy_DbNewInstanceExpression()
        {
            var propertyExpression =
                TypeUsage
                    .CreateDefaultTypeUsage(
                        _storeItemCollection.GetItems<EntityType>().Single(e => e.Name == "Entities"))
                    .Variable("Table")
                    .Property("Id");

            var newInstanceExpressionRowType =
                DbExpressionBuilder.NewRow(
                    new[]
                        {
                            new KeyValuePair<string, DbExpression>("Id", propertyExpression),
                            new KeyValuePair<string, DbExpression>("Const", DbExpressionBuilder.Constant(42))
                        });

            var legacyNewInstanceExpressionRowType =
                _legacyDbExpressionConverter.Visit(newInstanceExpressionRowType) as
                LegacyCommandTrees.DbNewInstanceExpression;

            legacyNewInstanceExpressionRowType.Should().NotBeNull();
            Assert.Equal(
                LegacyCommandTrees.DbExpressionKind.NewInstance,
                legacyNewInstanceExpressionRowType.ExpressionKind);
            legacyNewInstanceExpressionRowType.Arguments.Count.Should().Be(2);
            Assert.IsType<LegacyCommandTrees.DbPropertyExpression>(legacyNewInstanceExpressionRowType.Arguments[0]);
            Assert.IsType<LegacyCommandTrees.DbConstantExpression>(legacyNewInstanceExpressionRowType.Arguments[1]);

            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyNewInstanceExpressionRowType.ResultType, newInstanceExpressionRowType.ResultType);
        }

        [TestMethod]
        public void Visit_DbProjectExpression_creates_equivalent_legacy_DbProjectExpression()
        {
            var scanExpression =
                _storeItemCollection
                    .GetEntityContainer("AdventureWorksModelStoreContainer")
                    .EntitySets.Single(e => e.Name == "EntitiesSet")
                    .Scan();

            var propertyExpression =
                TypeUsage
                    .CreateDefaultTypeUsage(
                        _storeItemCollection.GetItems<EntityType>().Single(e => e.Name == "Entities"))
                    .Variable("Table")
                    .Property("Id");

            var newInstanceExpression =
                DbExpressionBuilder.NewRow(
                    new[]
                        {
                            new KeyValuePair<string, DbExpression>("Id", propertyExpression),
                            new KeyValuePair<string, DbExpression>("Const", DbExpressionBuilder.Constant(42))
                        });

            var projectExpression =
                scanExpression
                    .BindAs("Table")
                    .Project(newInstanceExpression);

            var legacyProjectExpression =
                _legacyDbExpressionConverter.Visit(projectExpression) as LegacyCommandTrees.DbProjectExpression;

            legacyProjectExpression.Should().NotBeNull();
            legacyProjectExpression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Project);
            Assert.Equal(
                LegacyCommandTrees.DbExpressionKind.NewInstance,
                legacyProjectExpression.Projection.ExpressionKind);
            legacyProjectExpression.Input.VariableName.Should().Be(projectExpression.Input.VariableName);

            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyProjectExpression.Input.VariableType, projectExpression.Input.VariableType);

            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyProjectExpression.ResultType, projectExpression.ResultType);
        }

        [TestMethod]
        public void Visit_DbSortExpression_creates_equivalent_legacy_DbSortExpression()
        {
            var scanExpression =
                _storeItemCollection
                    .GetEntityContainer("AdventureWorksModelStoreContainer")
                    .EntitySets.Single(e => e.Name == "EntitiesSet")
                    .Scan();

            var idProperty =
                TypeUsage
                    .CreateDefaultTypeUsage(
                        _storeItemCollection.GetItems<EntityType>().Single(e => e.Name == "Entities"))
                    .Variable("Table")
                    .Property("Id");

            var nameProperty =
                TypeUsage
                    .CreateDefaultTypeUsage(
                        _storeItemCollection.GetItems<EntityType>().Single(e => e.Name == "Entities"))
                    .Variable("Table")
                    .Property("Name");

            var sortExpression =
                scanExpression
                    .BindAs("Table")
                    .Sort(
                        new[]
                            {
                                idProperty.ToSortClause(),
                                nameProperty.ToSortClause("testCollationAscending"),
                                nameProperty.ToSortClauseDescending(),
                                nameProperty.ToSortClauseDescending("testCollationDescending")
                            });

            var legacySortExpression =
                _legacyDbExpressionConverter.Visit(sortExpression) as LegacyCommandTrees.DbSortExpression;

            legacySortExpression.Should().NotBeNull();
            legacySortExpression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Sort);
            legacySortExpression.Input.Expression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Scan);
            legacySortExpression.Input.VariableName.Should().Be("Table");
            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacySortExpression.Input.VariableType, sortExpression.Input.VariableType);

            legacySortExpression.SortOrder.Count.Should().Be(4);
            (legacySortExpression.SortOrder.All(
                    e => e.Expression.ExpressionKind == LegacyCommandTrees.DbExpressionKind.Property));
            legacySortExpression.SortOrder[0].Ascending.Should().BeTrue();
            Assert.Empty(legacySortExpression.SortOrder[0].Collation);
            legacySortExpression.SortOrder[1].Ascending.Should().BeTrue();
            legacySortExpression.SortOrder[1].Collation.Should().Be("testCollationAscending");
            legacySortExpression.SortOrder[2].Ascending.Should().BeFalse();
            Assert.Empty(legacySortExpression.SortOrder[2].Collation);
            legacySortExpression.SortOrder[3].Ascending.Should().BeFalse();
            legacySortExpression.SortOrder[3].Collation.Should().Be("testCollationDescending");

            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacySortExpression.ResultType, sortExpression.ResultType);
        }

        [TestMethod]
        public void Visit_DbComparisonExpression_can_handle_all_comparison_operations()
        {
            var a = DbExpressionBuilder.Constant(42);
            var b = DbExpressionBuilder.Constant(911);

            ConvertAndVerifyComparisonExpression(a.Equal(b));
            ConvertAndVerifyComparisonExpression(a.NotEqual(b));
            ConvertAndVerifyComparisonExpression(a.LessThan(b));
            ConvertAndVerifyComparisonExpression(a.LessThanOrEqual(b));
            ConvertAndVerifyComparisonExpression(a.GreaterThan(b));
            ConvertAndVerifyComparisonExpression(a.GreaterThanOrEqual(b));
        }

        private void ConvertAndVerifyComparisonExpression(DbComparisonExpression comparisonExpression)
        {
            var legacyComparisonExpression =
                _legacyDbExpressionConverter.Visit(comparisonExpression) as LegacyCommandTrees.DbComparisonExpression;

            legacyComparisonExpression.Should().NotBeNull();
            Assert.Equal((int)comparisonExpression.ExpressionKind, (int)legacyComparisonExpression.ExpressionKind);
            legacyComparisonExpression.Left.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Constant);
            Assert.Equal(
                ((DbConstantExpression)comparisonExpression.Left).Value,
                ((LegacyCommandTrees.DbConstantExpression)legacyComparisonExpression.Left).Value);
            legacyComparisonExpression.Right.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Constant);
            Assert.Equal(
                ((DbConstantExpression)comparisonExpression.Right).Value,
                ((LegacyCommandTrees.DbConstantExpression)legacyComparisonExpression.Right).Value);

            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyComparisonExpression.ResultType, comparisonExpression.ResultType);
        }

        [TestMethod]
        public void Visit_DbArithmeticExpression_creates_equivalent_legacy_DbArithmeticExpressions_for_operators()
        {
            var a = DbExpressionBuilder.Constant(42);
            var b = DbExpressionBuilder.Constant(911);
            ConvertAndVerifyArithmeticExpression(a.Plus(b));
            ConvertAndVerifyArithmeticExpression(a.Minus(b));
            ConvertAndVerifyArithmeticExpression(a.Multiply(b));
            ConvertAndVerifyArithmeticExpression(a.Divide(b));
            ConvertAndVerifyArithmeticExpression(a.Modulo(b));
            ConvertAndVerifyArithmeticExpression(a.UnaryMinus());
            ConvertAndVerifyArithmeticExpression(a.Negate());
        }

        private void ConvertAndVerifyArithmeticExpression(DbArithmeticExpression arithmeticExpression)
        {
            var legacyArithmeticExpression =
                _legacyDbExpressionConverter.Visit(arithmeticExpression) as LegacyCommandTrees.DbArithmeticExpression;

            arithmeticExpression.Should().NotBeNull();

            Assert.Equal((int)arithmeticExpression.ExpressionKind, (int)legacyArithmeticExpression.ExpressionKind);
            legacyArithmeticExpression.Arguments.Count.Should().Be(arithmeticExpression.Arguments.Count);
            (arithmeticExpression.Arguments.Zip(
                    legacyArithmeticExpression.Arguments,
                    (e1, e2) => ((DbConstantExpression)e1).Value == ((LegacyCommandTrees.DbConstantExpression)e2).Value)
                    .All(r => r));

            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyArithmeticExpression.ResultType, arithmeticExpression.ResultType);
        }

        [TestMethod]
        public void Visit_DbCaseExpression_creates_equivalent_legacy_DbCaseExpression()
        {
            var whens =
                new[]
                    {
                        DbExpressionBuilder.Constant(42).NotEqual(DbExpressionBuilder.Constant(42)),
                        DbExpressionBuilder.Constant(911).Equal(DbExpressionBuilder.Constant(911)),
                    };

            var thens =
                new[]
                    {
                        DbExpressionBuilder.False,
                        DbExpressionBuilder.True
                    };

            var caseExpression =
                DbExpressionBuilder.Case(whens, thens, DbExpressionBuilder.False);

            var legacyCaseExpression =
                _legacyDbExpressionConverter.Visit(caseExpression) as LegacyCommandTrees.DbCaseExpression;

            legacyCaseExpression.Should().NotBeNull();
            legacyCaseExpression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Case);
            legacyCaseExpression.When.Count.Should().Be(2);
            legacyCaseExpression.When[0].ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.NotEquals);
            legacyCaseExpression.When[1].ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Equals);

            legacyCaseExpression.Then.Count.Should().Be(2);
            (bool.Should().BeFalse()((LegacyCommandTrees.DbConstantExpression)legacyCaseExpression.Then[0]).Value);
            (bool.Should().BeTrue()((LegacyCommandTrees.DbConstantExpression)legacyCaseExpression.Then[1]).Value);
            (bool.Should().BeFalse()((LegacyCommandTrees.DbConstantExpression)legacyCaseExpression.Else).Value);

            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyCaseExpression.ResultType, caseExpression.ResultType);
        }

        [TestMethod]
        public void Visit_DbFitlerExpression_creates_equivalent_legacy_DbFilterExpression()
        {
            var scanExpression =
                _storeItemCollection
                    .GetEntityContainer("AdventureWorksModelStoreContainer")
                    .EntitySets.Single(e => e.Name == "EntitiesSet")
                    .Scan();

            var filterExpression =
                scanExpression
                    .BindAs("Table")
                    .Filter(
                        DbExpressionBuilder.Constant(911).Equal(DbExpressionBuilder.Constant(911)));

            var legacyFilterExpression =
                _legacyDbExpressionConverter.Visit(filterExpression) as LegacyCommandTrees.DbFilterExpression;

            legacyFilterExpression.Should().NotBeNull();
            legacyFilterExpression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Filter);
            legacyFilterExpression.Input.Expression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Scan);
            legacyFilterExpression.Predicate.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Equals);

            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyFilterExpression.ResultType, filterExpression.ResultType);
        }

        [TestMethod]
        public void Visit_DbUnionAllExpression_creates_equivalent_legacy_DbUnionAllExpression()
        {
            var scanExpression =
                _storeItemCollection
                    .GetEntityContainer("AdventureWorksModelStoreContainer")
                    .EntitySets.Single(e => e.Name == "EntitiesSet")
                    .Scan();

            var unionAllExpression = scanExpression.UnionAll(scanExpression);

            var legacyUnionAllExpression =
                _legacyDbExpressionConverter.Visit(unionAllExpression) as LegacyCommandTrees.DbUnionAllExpression;

            legacyUnionAllExpression.Should().NotBeNull();
            legacyUnionAllExpression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.UnionAll);
            legacyUnionAllExpression.Left.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Scan);
            legacyUnionAllExpression.Right.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Scan);

            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyUnionAllExpression.ResultType, unionAllExpression.ResultType);
        }

        [TestMethod]
        public void Visit_DbAndExpression_creates_equivalent_legacy_DbAndExpression()
        {
            var andExpression =
                DbExpressionBuilder.False
                    .And(DbExpressionBuilder.True);

            var legacyAndExpression =
                _legacyDbExpressionConverter.Visit(andExpression) as LegacyCommandTrees.DbAndExpression;

            legacyAndExpression.Should().NotBeNull();
            legacyAndExpression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.And);
            (bool.Should().BeFalse()((LegacyCommandTrees.DbConstantExpression)legacyAndExpression.Left).Value);
            (bool.Should().BeTrue()((LegacyCommandTrees.DbConstantExpression)legacyAndExpression.Right).Value);

            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyAndExpression.ResultType, andExpression.ResultType);
        }

        [TestMethod]
        public void Visit_DbOrExpression_creates_equivalent_legacy_DbOrExpression()
        {
            var orExpression =
                DbExpressionBuilder.False
                    .Or(DbExpressionBuilder.True);

            var legacyOrExpression =
                _legacyDbExpressionConverter.Visit(orExpression) as LegacyCommandTrees.DbOrExpression;

            legacyOrExpression.Should().NotBeNull();
            legacyOrExpression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Or);
            (bool.Should().BeFalse()((LegacyCommandTrees.DbConstantExpression)legacyOrExpression.Left).Value);
            (bool.Should().BeTrue()((LegacyCommandTrees.DbConstantExpression)legacyOrExpression.Right).Value);

            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyOrExpression.ResultType, orExpression.ResultType);
        }

        [TestMethod]
        public void Visit_DbJoinExpression_creates_equivalent_InnerJoin_DbJoinExpression()
        {
            ConvertAndVerifyJoinExpressions(
                (left, right, joinCondition) => left.InnerJoin(right, joinCondition));
        }

        [TestMethod]
        public void Visit_DbJoinExpression_creates_equivalent_LeftOuterJoin_DbJoinExpression()
        {
            ConvertAndVerifyJoinExpressions(
                (left, right, joinCondition) => left.LeftOuterJoin(right, joinCondition));
        }

        [TestMethod]
        public void Visit_DbJoinExpression_creates_equivalent_FullOuterJoin_DbJoinExpression()
        {
            ConvertAndVerifyJoinExpressions(
                (left, right, joinCondition) => left.FullOuterJoin(right, joinCondition));
        }

        private void ConvertAndVerifyJoinExpressions(
            Func<DbExpressionBinding, DbExpressionBinding, DbExpression, DbJoinExpression> createJoinExpression)
        {
            var leftScanExpression =
                _storeItemCollection
                    .GetEntityContainer("AdventureWorksModelStoreContainer")
                    .EntitySets.Single(e => e.Name == "EntitiesSet")
                    .Scan();

            var leftPropertyExpression =
                TypeUsage
                    .CreateDefaultTypeUsage(
                        _storeItemCollection.GetItems<EntityType>().Single(e => e.Name == "Entities"))
                    .Variable("leftTable")
                    .Property("Id");

            var rightScanExpression =
                _storeItemCollection
                    .GetEntityContainer("AdventureWorksModelStoreContainer")
                    .EntitySets.Single(e => e.Name == "OtherEntitiesSet")
                    .Scan();

            var rightPropertyExpression =
                TypeUsage
                    .CreateDefaultTypeUsage(
                        _storeItemCollection.GetItems<EntityType>().Single(e => e.Name == "OtherEntities"))
                    .Variable("rightTable")
                    .Property("Id");

            var joinExpression =
                createJoinExpression(
                    leftScanExpression.BindAs("leftTable"),
                    rightScanExpression.BindAs("rightTable"),
                    leftPropertyExpression.Equal(rightPropertyExpression));

            var legacyJoinExpression =
                _legacyDbExpressionConverter.Visit(joinExpression) as LegacyCommandTrees.DbJoinExpression;

            legacyJoinExpression.Should().NotBeNull();
            Assert.Equal((int)joinExpression.ExpressionKind, (int)legacyJoinExpression.ExpressionKind);
            legacyJoinExpression.Left.Expression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Scan);
            Assert.Equal("EntitiesSet", ((LegacyCommandTrees.DbScanExpression)legacyJoinExpression.Left.Expression).Target.Name);
            legacyJoinExpression.Left.VariableName.Should().Be("leftTable");
            legacyJoinExpression.Right.Expression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Scan);
            Assert.Equal("OtherEntitiesSet", ((LegacyCommandTrees.DbScanExpression)legacyJoinExpression.Right.Expression).Target.Name);
            legacyJoinExpression.Right.VariableName.Should().Be("rightTable");
            legacyJoinExpression.JoinCondition.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Equals);
            var comparisonExpression = (LegacyCommandTrees.DbComparisonExpression)legacyJoinExpression.JoinCondition;
            Assert.Equal("Entities", ((LegacyCommandTrees.DbPropertyExpression)comparisonExpression.Left).Property.DeclaringType.Name);
            Assert.Equal("OtherEntities", ((LegacyCommandTrees.DbPropertyExpression)comparisonExpression.Right).Property.DeclaringType.Name);
            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyJoinExpression.ResultType, joinExpression.ResultType);
        }

        [TestMethod]
        public void Visit_DbCrossJoinExpression_creates_equivalent_DbCrossJoinExpression()
        {
            var bindings =
                new[]
                    {
                        _storeItemCollection
                            .GetEntityContainer("AdventureWorksModelStoreContainer")
                            .EntitySets.Single(e => e.Name == "EntitiesSet")
                            .Scan()
                            .BindAs("table1"),
                        _storeItemCollection
                            .GetEntityContainer("AdventureWorksModelStoreContainer")
                            .EntitySets.Single(e => e.Name == "OtherEntitiesSet")
                            .Scan()
                            .BindAs("table2")
                    };

            var crossJoinExpression = DbExpressionBuilder.CrossJoin(bindings);

            var legacyCrossJoinExpression =
                _legacyDbExpressionConverter.Visit(crossJoinExpression) as LegacyCommandTrees.DbCrossJoinExpression;

            legacyCrossJoinExpression.Should().NotBeNull();
            legacyCrossJoinExpression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.CrossJoin);
            legacyCrossJoinExpression.Inputs[0].Expression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Scan);
            Assert.Equal("EntitiesSet", ((LegacyCommandTrees.DbScanExpression)legacyCrossJoinExpression.Inputs[0].Expression).Target.Name);
            legacyCrossJoinExpression.Inputs[0].VariableName.Should().Be("table1");
            legacyCrossJoinExpression.Inputs[1].Expression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Scan);
            Assert.Equal(
                "OtherEntitiesSet", ((LegacyCommandTrees.DbScanExpression)legacyCrossJoinExpression.Inputs[1].Expression).Target.Name);
            legacyCrossJoinExpression.Inputs[1].VariableName.Should().Be("table2");
            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyCrossJoinExpression.ResultType, crossJoinExpression.ResultType);
        }

        [TestMethod]
        public void Visit_DbIsNullExpression_creates_equivalent_legacy_DbIsNullExpression()
        {
            var isNullExpression =
                TypeUsage.CreateDefaultTypeUsage(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32)).Null().IsNull();

            var legacyIsNullExpression =
                _legacyDbExpressionConverter.Visit(isNullExpression) as LegacyCommandTrees.DbIsNullExpression;

            legacyIsNullExpression.Should().NotBeNull();
            legacyIsNullExpression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.IsNull);
            legacyIsNullExpression.Argument.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Null);
            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyIsNullExpression.ResultType, isNullExpression.ResultType);
        }

        [TestMethod]
        public void Visit_DbNullExpression_creates_equivalent_legacy_DbNullExpression()
        {
            var nullExpression =
                TypeUsage.CreateDefaultTypeUsage(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32)).Null();

            var legacyNullExpression =
                _legacyDbExpressionConverter.Visit(nullExpression) as LegacyCommandTrees.DbNullExpression;

            legacyNullExpression.Should().NotBeNull();
            legacyNullExpression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Null);
            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyNullExpression.ResultType, nullExpression.ResultType);
        }

        [TestMethod]
        public void Visit_DbNotExpression_creates_equivalent_legacy_DbNotExpression()
        {
            var notExpression = DbExpressionBuilder.True.Not();

            var legacyNotExpression =
                _legacyDbExpressionConverter.Visit(notExpression) as LegacyCommandTrees.DbNotExpression;

            legacyNotExpression.Should().NotBeNull();
            legacyNotExpression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Not);
            (bool.Should().BeTrue()((LegacyCommandTrees.DbConstantExpression)legacyNotExpression.Argument).Value);
            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyNotExpression.ResultType, notExpression.ResultType);
        }

        [TestMethod]
        public void Visit_DbLikeExpression_creates_equivalent_legacy_DbLikeExpression()
        {
            var propertyExpression =
                TypeUsage
                    .CreateDefaultTypeUsage(
                        _storeItemCollection.GetItems<EntityType>().Single(e => e.Name == "Entities"))
                    .Variable("Table")
                    .Property("Name");

            var stringTypeUsage =
                TypeUsage.CreateDefaultTypeUsage(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String));

            var likeExpression = propertyExpression.Like(stringTypeUsage.Constant("foo"), stringTypeUsage.Constant("bar"));

            var legacyLikeExpression =
                _legacyDbExpressionConverter.Visit(likeExpression) as LegacyCommandTrees.DbLikeExpression;

            legacyLikeExpression.Should().NotBeNull();
            legacyLikeExpression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Like);
            legacyLikeExpression.Argument.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Property);
            Assert.Equal("foo", (string)((LegacyCommandTrees.DbConstantExpression)legacyLikeExpression.Pattern).Value);
            Assert.Equal("bar", (string)((LegacyCommandTrees.DbConstantExpression)legacyLikeExpression.Escape).Value);
            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyLikeExpression.ResultType, likeExpression.ResultType);
        }

        [TestMethod]
        public void Visit_DbParameterRefExpression_creates_equivalent_legacy_DbParameterRefExpression()
        {
            var paramRefExpression =
                TypeUsage.CreateDefaultTypeUsage(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String))
                    .Parameter("foo");

            var legacyParamRefExpression =
                _legacyDbExpressionConverter.Visit(paramRefExpression) as LegacyCommandTrees.DbParameterReferenceExpression;

            legacyParamRefExpression.Should().NotBeNull();
            legacyParamRefExpression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.ParameterReference);
            legacyParamRefExpression.ParameterName.Should().Be("foo");
            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyParamRefExpression.ResultType, paramRefExpression.ResultType);
        }

        [TestMethod]
        public void Visit_DbCastExpression_creates_equivalent_legacy_DbCastExpression()
        {
            var castExpression =
                DbExpressionBuilder.Constant(42).CastTo(
                    TypeUsage.CreateDefaultTypeUsage(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Decimal)));

            var legacyCastExpression =
                _legacyDbExpressionConverter.Visit(castExpression) as LegacyCommandTrees.DbCastExpression;

            legacyCastExpression.Should().NotBeNull();
            legacyCastExpression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Cast);
            Assert.Equal(42, (int)((LegacyCommandTrees.DbConstantExpression)legacyCastExpression.Argument).Value);
            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyCastExpression.ResultType, castExpression.ResultType);
        }

        [TestMethod]
        public void Visit_DbDistinctExpression_creates_equivalent_legacy_DbDistinctExpression()
        {
            var distinctExpression =
                _storeItemCollection
                    .GetEntityContainer("AdventureWorksModelStoreContainer")
                    .EntitySets.Single(e => e.Name == "EntitiesSet")
                    .Scan()
                    .Distinct();

            var legacyDistinctExpression =
                _legacyDbExpressionConverter.Visit(distinctExpression) as LegacyCommandTrees.DbDistinctExpression;

            legacyDistinctExpression.Should().NotBeNull();
            legacyDistinctExpression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Distinct);
            legacyDistinctExpression.Argument.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Scan);
            Assert.Equal("EntitiesSet", ((LegacyCommandTrees.DbScanExpression)legacyDistinctExpression.Argument).Target.Name);
            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyDistinctExpression.ResultType, distinctExpression.ResultType);
        }

        [TestMethod]
        public void Visit_DbSkipExpression_creates_equivalent_legacy_DbSkipExpression()
        {
            var scanExpression =
                _storeItemCollection
                    .GetEntityContainer("AdventureWorksModelStoreContainer")
                    .EntitySets.Single(e => e.Name == "EntitiesSet")
                    .Scan();

            var idProperty =
                TypeUsage
                    .CreateDefaultTypeUsage(
                        _storeItemCollection.GetItems<EntityType>().Single(e => e.Name == "Entities"))
                    .Variable("Table")
                    .Property("Id");

            var skipExpression =
                scanExpression
                    .BindAs("Table")
                    .Skip(new[] { idProperty.ToSortClause() }, DbExpressionBuilder.Constant(42));

            var legacySkipExpression =
                _legacyDbExpressionConverter.Visit(skipExpression) as LegacyCommandTrees.DbSkipExpression;

            legacySkipExpression.Should().NotBeNull();
            legacySkipExpression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Skip);
            legacySkipExpression.Input.VariableName.Should().Be("Table");
            legacySkipExpression.Input.Expression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Scan);
            Assert.Equal(
                "Id",
                ((LegacyCommandTrees.DbPropertyExpression)legacySkipExpression.SortOrder.Single().Expression).Property.Name);
            Assert.Equal(42, ((LegacyCommandTrees.DbConstantExpression)legacySkipExpression.Count).Value);
            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacySkipExpression.ResultType, skipExpression.ResultType);
        }

        [TestMethod]
        public void Visit_DbLimitExpression_creates_equivalent_legacy_DbLimitExpression()
        {
            var scanExpression =
                _storeItemCollection
                    .GetEntityContainer("AdventureWorksModelStoreContainer")
                    .EntitySets.Single(e => e.Name == "EntitiesSet")
                    .Scan();

            var limitExpression =
                scanExpression.Limit(DbExpressionBuilder.Constant(42));

            var legacyLimitExpression =
                _legacyDbExpressionConverter.Visit(limitExpression) as LegacyCommandTrees.DbLimitExpression;

            legacyLimitExpression.Should().NotBeNull();
            legacyLimitExpression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Limit);
            legacyLimitExpression.Argument.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Scan);
            Assert.Equal("EntitiesSet", ((LegacyCommandTrees.DbScanExpression)legacyLimitExpression.Argument).Target.Name);
            Assert.Equal(42, ((LegacyCommandTrees.DbConstantExpression)legacyLimitExpression.Limit).Value);
            legacyLimitExpression.WithTies.Should().Be(limitExpression.WithTies);
            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyLimitExpression.ResultType, limitExpression.ResultType);
        }

        [TestMethod]
        public void Visit_DbExceptExpression_creates_equivalent_legacy_DbExceptExpression()
        {
            var left = DbExpressionBuilder.NewCollection(new DbExpression[] { DbExpressionBuilder.Constant(42) });
            var right = DbExpressionBuilder.NewCollection(new DbExpression[] { DbExpressionBuilder.Constant(24) });

            var exceptExpression = left.Except(right);

            var legacyExceptExpression =
                _legacyDbExpressionConverter.Visit(exceptExpression) as LegacyCommandTrees.DbExceptExpression;

            legacyExceptExpression.Should().NotBeNull();
            legacyExceptExpression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Except);

            Assert.Equal(
                42,
                ((LegacyCommandTrees.DbConstantExpression)
                 ((LegacyCommandTrees.DbNewInstanceExpression)legacyExceptExpression.Left).Arguments.Single())
                    .Value);

            Assert.Equal(
                24,
                ((LegacyCommandTrees.DbConstantExpression)
                 ((LegacyCommandTrees.DbNewInstanceExpression)legacyExceptExpression.Right).Arguments.Single())
                    .Value);

            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyExceptExpression.ResultType, exceptExpression.ResultType);
        }

        [TestMethod]
        public void Visit_DbIntersectExpression_creates_equivalent_legacy_DbIntersectExpression()
        {
            var left = DbExpressionBuilder.NewCollection(new DbExpression[] { DbExpressionBuilder.Constant(42) });
            var right = DbExpressionBuilder.NewCollection(new DbExpression[] { DbExpressionBuilder.Constant(24) });

            var intersectExpression = left.Intersect(right);

            var legacyIntersectExpression =
                _legacyDbExpressionConverter.Visit(intersectExpression) as LegacyCommandTrees.DbIntersectExpression;

            legacyIntersectExpression.Should().NotBeNull();
            legacyIntersectExpression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Intersect);

            Assert.Equal(
                42,
                ((LegacyCommandTrees.DbConstantExpression)
                 ((LegacyCommandTrees.DbNewInstanceExpression)legacyIntersectExpression.Left).Arguments.Single())
                    .Value);

            Assert.Equal(
                24,
                ((LegacyCommandTrees.DbConstantExpression)
                 ((LegacyCommandTrees.DbNewInstanceExpression)legacyIntersectExpression.Right).Arguments.Single())
                    .Value);

            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyIntersectExpression.ResultType, intersectExpression.ResultType);
        }

        [TestMethod]
        public void Visit_DbIsEmptyExpression_creates_equivalent_legacy_DbIsEmptyExpression()
        {
            var isEmptyExpression =
                DbExpressionBuilder.NewCollection(new DbExpression[] { DbExpressionBuilder.Constant(42) })
                    .IsEmpty();

            var legacyIsEmptyExpression
                = _legacyDbExpressionConverter.Visit(isEmptyExpression) as LegacyCommandTrees.DbIsEmptyExpression;

            legacyIsEmptyExpression.Should().NotBeNull();
            legacyIsEmptyExpression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.IsEmpty);
            Assert.Equal(
                42,
                (((LegacyCommandTrees.DbConstantExpression)
                  ((LegacyCommandTrees.DbNewInstanceExpression)legacyIsEmptyExpression.Argument).Arguments.Single())
                    .Value));

            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyIsEmptyExpression.ResultType, isEmptyExpression.ResultType);
        }

        [TestMethod]
        public void Visit_DbElementExpression_creates_equivalent_legacy_DbElementExpression()
        {
            var elementExpression =
                DbExpressionBuilder.NewCollection(new DbExpression[] { DbExpressionBuilder.Constant(42) })
                    .Element();

            var legacyElementExpression
                = _legacyDbExpressionConverter.Visit(elementExpression) as LegacyCommandTrees.DbElementExpression;

            legacyElementExpression.Should().NotBeNull();
            legacyElementExpression.ExpressionKind.Should().Be(LegacyCommandTrees.DbExpressionKind.Element);
            Assert.Equal(
                42,
                (((LegacyCommandTrees.DbConstantExpression)
                  ((LegacyCommandTrees.DbNewInstanceExpression)legacyElementExpression.Argument).Arguments.Single())
                    .Value));

            TypeUsageVerificationHelper
                .VerifyTypeUsagesEquivalent(legacyElementExpression.ResultType, elementExpression.ResultType);
        }
    }
}
