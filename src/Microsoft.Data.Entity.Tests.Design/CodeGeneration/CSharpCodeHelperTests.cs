// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration
{
    [TestClass]
    public class CSharpCodeHelperTests
    {
        private static DbModel _model;

        private static DbModel Model
        {
            get
            {
                if (_model == null)
                {
                    DbModelBuilder modelBuilder = new DbModelBuilder();
                    modelBuilder.Entity<Entity>().HasMany(e => e.Children).WithOptional(e => e.Parent);

                    _model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
                }

                return _model;
            }
        }

        [TestMethod]
        public void Type_evaluates_preconditions()
        {
            ArgumentException ex;
            CSharpCodeHelper code = new CSharpCodeHelper();

            ex = ((Action)(() => code.Type((EntityContainer)null))).Should().Throw<ArgumentNullException>().Which;
            ex.ParamName.Should().Be("container");

            ex = ((Action)(() => code.Type((EdmType)null))).Should().Throw<ArgumentNullException>().Which;
            ex.ParamName.Should().Be("edmType");

            ex = ((Action)(() => code.Type((EdmProperty)null))).Should().Throw<ArgumentNullException>().Which;
            ex.ParamName.Should().Be("property");

            ex = ((Action)(() => code.Type((NavigationProperty)null))).Should().Throw<ArgumentNullException>().Which;
            ex.ParamName.Should().Be("navigationProperty");
        }

        [TestMethod]
        public void Type_returns_container_name()
        {
            var container = Model.ConceptualModel.Container;
            CSharpCodeHelper code = new CSharpCodeHelper();

            code.Type(container).Should().Be("CodeFirstContainer");
        }

        [TestMethod, Ignore("Type lacks parameterless constructor in locally built")]
        public void Type_escapes_container_name()
        {
            Mock<EntityContainer> container = new Mock<EntityContainer>();
            container.SetupGet(c => c.Name).Returns("null");
            CSharpCodeHelper code = new CSharpCodeHelper();

            code.Type(container.Object).Should().Be("_null");
        }

        [TestMethod]
        public void Type_returns_type_name()
        {
            var container = Model.ConceptualModel.EntityTypes.First();
            CSharpCodeHelper code = new CSharpCodeHelper();

            code.Type(container).Should().Be("Entity");
        }

        [TestMethod, Ignore("Type lacks parameterless constructor in locally built")]
        public void Type_escapes_type_name()
        {
            Mock<EdmType> type = new Mock<EdmType>();
            type.SetupGet(c => c.Name).Returns("null");
            CSharpCodeHelper code = new CSharpCodeHelper();

            code.Type(type.Object).Should().Be("_null");
        }

        [TestMethod]
        public void Type_returns_property_type()
        {
            var property = Model.ConceptualModel.EntityTypes.First().Properties.First(
                p => p.Name == "Name");
            CSharpCodeHelper code = new CSharpCodeHelper();

            code.Type(property).Should().Be("string");
        }

        [TestMethod]
        public void Type_returns_value_property_type()
        {
            var property = Model.ConceptualModel.EntityTypes.First().Properties.First(
                p => p.Name == "Id");
            CSharpCodeHelper code = new CSharpCodeHelper();

            code.Type(property).Should().Be("int");
        }

        [TestMethod]
        public void Type_returns_nullable_value_property_type()
        {
            var property = Model.ConceptualModel.EntityTypes.First().Properties.First(
                p => p.Name == "ParentId");
            CSharpCodeHelper code = new CSharpCodeHelper();

            code.Type(property).Should().Be("int?");
        }

        [TestMethod]
        public void Type_unqualifies_property_type()
        {
            var property = Model.ConceptualModel.EntityTypes.First().Properties.First(
                p => p.Name == "Guid");
            CSharpCodeHelper code = new CSharpCodeHelper();

            code.Type(property).Should().Be("Guid");
        }

        [TestMethod]
        public void Type_returns_reference_property_type()
        {
            var property = Model.ConceptualModel.EntityTypes.First().NavigationProperties.First(
                p => p.Name == "Parent");
            CSharpCodeHelper code = new CSharpCodeHelper();

            code.Type(property).Should().Be("Entity");
        }

        [TestMethod]
        public void Type_returns_collection_property_type()
        {
            var property = Model.ConceptualModel.EntityTypes.First().NavigationProperties.First(
                p => p.Name == "Children");
            CSharpCodeHelper code = new CSharpCodeHelper();

            code.Type(property).Should().Be("ICollection<Entity>");
        }

        [TestMethod]
        public void Property_evaluates_preconditions()
        {
            ArgumentException ex;
            CSharpCodeHelper code = new CSharpCodeHelper();

            ex = ((Action)(() => code.Property((EntitySetBase)null))).Should().Throw<ArgumentNullException>().Which;
            ex.ParamName.Should().Be("entitySet");

            ex = ((Action)(() => code.Property((EdmMember)null))).Should().Throw<ArgumentNullException>().Which;
            ex.ParamName.Should().Be("member");
        }

        [TestMethod]
        public void Property_returns_entity_set_name()
        {
            var entitySet = Model.ConceptualModel.Container.EntitySets.First();
            CSharpCodeHelper code = new CSharpCodeHelper();

            code.Property(entitySet).Should().Be("Entities");
        }

        [TestMethod, Ignore("Type lacks parameterless constructor in locally built")]
        public void Property_escapes_entity_set_name()
        {
            Mock<EntitySetBase> set = new Mock<EntitySetBase>();
            set.SetupGet(s => s.Name).Returns("null");
            CSharpCodeHelper code = new CSharpCodeHelper();

            code.Property(set.Object).Should().Be("_null");
        }

        [TestMethod]
        public void Property_returns_member_name()
        {
            var member = Model.ConceptualModel.EntityTypes.First().Properties.First(p => p.Name == "Id");
            CSharpCodeHelper code = new CSharpCodeHelper();

            code.Property(member).Should().Be("Id");
        }

        [TestMethod, Ignore("Type lacks parameterless constructor in locally built")]
        public void Property_escapes_member_name()
        {
            Mock<EdmMember> member = new Mock<EdmMember>();
            member.SetupGet(m => m.Name).Returns("null");
            CSharpCodeHelper code = new CSharpCodeHelper();

            code.Property(member.Object).Should().Be("_null");
        }

        [TestMethod]
        public void Attribute_evaluates_preconditions()
        {
            CSharpCodeHelper code = new CSharpCodeHelper();

            var ex = ((Action)(() => code.Attribute(null))).Should().Throw<ArgumentNullException>().Which;
            ex.ParamName.Should().Be("configuration");
        }

        [TestMethod]
        public void Attribute_surrounds_body()
        {
            CSharpCodeHelper code = new CSharpCodeHelper();
            Mock<IAttributeConfiguration> configuration = new Mock<IAttributeConfiguration>();
            configuration.Setup(c => c.GetAttributeBody(code)).Returns("Required");

            code.Attribute(configuration.Object).Should().Be("[Required]");
        }

        [TestMethod]
        public void MethodChain_evaluates_preconditions()
        {
            CSharpCodeHelper code = new CSharpCodeHelper();

            var ex = ((Action)(() => code.MethodChain(null))).Should().Throw<ArgumentNullException>().Which;
            ex.ParamName.Should().Be("configuration");
        }

        [TestMethod]
        public void MethodChain_calls_GetMethodChain_on_configuration()
        {
            CSharpCodeHelper code = new CSharpCodeHelper();
            Mock<IFluentConfiguration> configuration = new Mock<IFluentConfiguration>();

            code.MethodChain(configuration.Object);

            configuration.Verify(c => c.GetMethodChain(code));
        }

        [TestMethod]
        public void Literal_returns_string_when_one()
        {
            CSharpCodeHelper code = new CSharpCodeHelper();

            code.Literal(new[] { "One" }).Should().Be("\"One\"");
        }

        [TestMethod]
        public void Literal_returns_string_array_when_more_than_one()
        {
            CSharpCodeHelper code = new CSharpCodeHelper();

            code.Literal(new[] { "One", "Two" }).Should().Be("new[] { \"One\", \"Two\" }");
        }

        [TestMethod]
        public void Literal_returns_string()
        {
            CSharpCodeHelper code = new CSharpCodeHelper();

            code.Literal("One").Should().Be("\"One\"");
        }

        [TestMethod]
        public void Literal_returns_int()
        {
            CSharpCodeHelper code = new CSharpCodeHelper();

            code.Literal(42).Should().Be("42");
        }

        [TestMethod]
        public void Literal_returns_bool()
        {
            CSharpCodeHelper code = new CSharpCodeHelper();

            code.Literal(true).Should().Be("true");
            code.Literal(false).Should().Be("false");
        }

        [TestMethod]
        public void BeginLambda_returns_lambda_beginning()
        {
            CSharpCodeHelper code = new CSharpCodeHelper();

            code.BeginLambda("x").Should().Be("x => ");
        }

        [TestMethod]
        public void Lambda_returns_property_accessor_when_one()
        {
            CSharpCodeHelper code = new CSharpCodeHelper();
            var member = Model.ConceptualModel.EntityTypes.First().Properties.First(p => p.Name == "Id");

            code.Lambda(member).Should().Be("e => e.Id");
        }

        [TestMethod]
        public void Lambda_returns_anonymous_type_when_one()
        {
            CSharpCodeHelper code = new CSharpCodeHelper();
            var id = Model.ConceptualModel.EntityTypes.First().Properties.First(p => p.Name == "Id");
            var name = Model.ConceptualModel.EntityTypes.First().Properties.First(p => p.Name == "Name");

            code.Lambda(new[] { id, name }).Should().Be("e => new { e.Id, e.Name }");
        }

        [TestMethod]
        public void TypeArgument_surrounds_value()
        {
            CSharpCodeHelper code = new CSharpCodeHelper();

            code.TypeArgument("Entity").Should().Be("<Entity>");
        }

        private class Entity
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public Guid Guid { get; set; }
            public int? ParentId { get; set; }
            public Entity Parent { get; set; }
            public ICollection<Entity> Children { get; set; }
        }
    }
}
