// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration.Configuration;

namespace Microsoft.Data.Entity.Tests.Design.CodeGeneration
{
    [TestClass]
    public class VBCodeHelperTests
    {
        private static DbModel _model;

        private static DbModel Model
        {
            get
            {
                if (_model == null)
                {
                    DbModelBuilder modelBuilder = new DbModelBuilder();
                    modelBuilder.Entity<Entity>();

                    _model = modelBuilder.Build(new DbProviderInfo("System.Data.SqlClient", "2012"));
                }

                return _model;
            }
        }

        [TestMethod, Ignore("Type lacks parameterless constructor in locally built")]
        public void Type_escapes_container_name()
        {
            Mock<EntityContainer> container = new Mock<EntityContainer>();
            container.SetupGet(c => c.Name).Returns("Nothing");
            VBCodeHelper code = new VBCodeHelper();

            code.Type(container.Object).Should().Be("_Nothing");
        }

        [TestMethod]
        public void Type_returns_property_type()
        {
            var property = Model.ConceptualModel.EntityTypes.First().Properties.First(
                p => p.Name == "Name");
            VBCodeHelper code = new VBCodeHelper();

            code.Type(property).Should().Be("String");
        }

        [TestMethod]
        public void Type_returns_collection_property_type()
        {
            var property = Model.ConceptualModel.EntityTypes.First().NavigationProperties.First(
                p => p.Name == "Children");
            VBCodeHelper code = new VBCodeHelper();

            code.Type(property).Should().Be("ICollection(Of Entity)");
        }

        [TestMethod]
        public void Attribute_surrounds_body()
        {
            VBCodeHelper code = new VBCodeHelper();
            Mock<IAttributeConfiguration> configuration = new Mock<IAttributeConfiguration>();
            configuration.Setup(c => c.GetAttributeBody(code)).Returns("Required");

            code.Attribute(configuration.Object).Should().Be("<Required>");
        }

        [TestMethod]
        public void Literal_returns_string_array_when_more_than_one()
        {
            VBCodeHelper code = new VBCodeHelper();

            code.Literal(new[] { "One", "Two" }).Should().Be("{\"One\", \"Two\"}");
        }

        [TestMethod]
        public void Literal_returns_bool()
        {
            VBCodeHelper code = new VBCodeHelper();

            code.Literal(true).Should().Be("True");
            code.Literal(false).Should().Be("False");
        }

        [TestMethod]
        public void BeginLambda_returns_lambda_beginning()
        {
            VBCodeHelper code = new VBCodeHelper();

            code.BeginLambda("x").Should().Be("Function(x) ");
        }

        [TestMethod]
        public void Lambda_returns_property_accessor_when_one()
        {
            VBCodeHelper code = new VBCodeHelper();
            var member = Model.ConceptualModel.EntityTypes.First().Properties.First(p => p.Name == "Id");

            code.Lambda(member).Should().Be("Function(e) e.Id");
        }

        [TestMethod]
        public void Lambda_returns_anonymous_type_when_one()
        {
            VBCodeHelper code = new VBCodeHelper();
            var id = Model.ConceptualModel.EntityTypes.First().Properties.First(p => p.Name == "Id");
            var name = Model.ConceptualModel.EntityTypes.First().Properties.First(p => p.Name == "Name");

            code.Lambda(new[] { id, name }).Should().Be("Function(e) New With {e.Id, e.Name}");
        }

        [TestMethod]
        public void TypeArgument_surrounds_value()
        {
            VBCodeHelper code = new VBCodeHelper();

            code.TypeArgument("Entity").Should().Be("(Of Entity)");
        }

        private class Entity
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public ICollection<Entity> Children { get; set; }
        }
    }
}
