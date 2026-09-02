// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.Data.Entity.Tests.Design.Diagrams.Rendering.Export
{
    /// <summary>
    ///     Composes an EDMX v3 file from a short description of the model it should contain.
    /// </summary>
    /// <remarks>
    ///     <see cref="TestEdmx" /> is a literal, which is the right shape for one fixed two-entity model and the
    ///     wrong shape for grouping, where every test wants a different arrangement of a dozen entities. This
    ///     builder writes the conceptual model and the diagram only - the layout never reads the storage model or
    ///     the mappings, and generating them would double the size of this file for nothing.
    /// </remarks>
    /// <example>
    ///     <code>
    ///     var path = new TestEdmxBuilder()
    ///         .AddEntity("Post", "Title")
    ///         .AddEntity("PostType")
    ///         .Relate(principal: "PostType", dependent: "Post")
    ///         .Write();
    ///     </code>
    /// </example>
    internal sealed class TestEdmxBuilder
    {

        #region Fields

        private const string Namespace = "TestModel";

        private readonly List<Association> _associations = [];
        private readonly List<Entity> _entities = [];
        private bool _enableGrouping;
        private bool _generateGroupNames;

        #endregion

        #region Public Methods

        /// <summary>
        ///     Adds an entity with an <c>Id</c> key and the given extra scalar properties.
        /// </summary>
        /// <param name="name">The entity's name.</param>
        /// <param name="scalarProperties">Extra scalar property names.</param>
        /// <returns>This builder.</returns>
        public TestEdmxBuilder AddEntity(string name, params string[] scalarProperties)
        {
            _entities.Add(new Entity(name, scalarProperties));

            return this;
        }

        /// <summary>
        ///     Adds a one-to-many association, giving the dependent the foreign key property a real schema
        ///     would have.
        /// </summary>
        /// <param name="principal">The entity at the <c>1</c> end.</param>
        /// <param name="dependent">The entity at the <c>*</c> end.</param>
        /// <param name="optional">Whether the principal end is <c>0..1</c> rather than <c>1</c>.</param>
        /// <returns>This builder.</returns>
        /// <remarks>
        ///     The <c>{principal}Id</c> property is added automatically because the grouping's reference-table
        ///     rule looks for exactly that, and a fixture that declared the association without it would be
        ///     testing against a schema no database would produce.
        /// </remarks>
        public TestEdmxBuilder Relate(string principal, string dependent, bool optional = false)
        {
            _associations.Add(new Association(principal, dependent, optional));
            _entities.Single(entity => entity.Name == dependent).ScalarProperties.Add(principal + "Id");

            return this;
        }

        /// <summary>
        ///     Sets the shape attributes the diagram records for an entity.
        /// </summary>
        /// <param name="name">The entity whose shape is being described.</param>
        /// <param name="fillColor">The shape's fill colour, or <see langword="null" /> for the default.</param>
        /// <param name="groupName">The shape's group name, or <see langword="null" /> for none.</param>
        /// <returns>This builder.</returns>
        public TestEdmxBuilder Shape(string name, Color? fillColor = null, string groupName = null)
        {
            var entity = _entities.Single(candidate => candidate.Name == name);
            entity.FillColor = fillColor;
            entity.GroupName = groupName;

            return this;
        }

        /// <summary>
        ///     Sets the routing recorded on an association's connector, so a test can compose the exact - including
        ///     deliberately malformed - <c>ManuallyRouted</c> / <c>ConnectorPoint</c> combinations the loader has to
        ///     survive.
        /// </summary>
        /// <param name="principal">The association's principal entity.</param>
        /// <param name="dependent">The association's dependent entity.</param>
        /// <param name="manuallyRouted">The value to write for <c>ManuallyRouted</c>.</param>
        /// <param name="points">The connector points to write, if any.</param>
        /// <returns>This builder.</returns>
        public TestEdmxBuilder Route(string principal, string dependent, bool manuallyRouted, params (double X, double Y)[] points)
        {
            var association = _associations.Single(
                candidate => candidate.Principal == principal && candidate.Dependent == dependent);

            association.ManuallyRouted = manuallyRouted;
            association.ConnectorPoints = points;

            return this;
        }

        /// <summary>
        ///     Sets the diagram's grouping switches. Both default to off, matching a real diagram.
        /// </summary>
        /// <param name="enable">Whether a modern layout clusters shapes into groups.</param>
        /// <param name="generateNames">Whether it writes a detected group name onto a shape that has none.</param>
        /// <returns>This builder.</returns>
        public TestEdmxBuilder Grouping(bool enable, bool generateNames = false)
        {
            _enableGrouping = enable;
            _generateGroupNames = generateNames;

            return this;
        }

        /// <summary>
        ///     Writes the model to a temporary .edmx file.
        /// </summary>
        /// <returns>The full path of the file. The caller is responsible for deleting it.</returns>
        public string Write()
        {
            var path = Path.Combine(Path.GetTempPath(), $"EdmxGroupingTest_{Path.GetRandomFileName()}.edmx");
            File.WriteAllText(path, Build());

            return path;
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Renders the whole document.
        /// </summary>
        private string Build()
        {
            var edmx = new StringBuilder();

            edmx.AppendLine(@"<?xml version=""1.0"" encoding=""utf-8""?>");
            edmx.AppendLine(@"<edmx:Edmx Version=""3.0"" xmlns:edmx=""http://schemas.microsoft.com/ado/2009/11/edmx"">");
            edmx.AppendLine(@"  <edmx:Runtime>");
            edmx.AppendLine(@"    <edmx:ConceptualModels>");
            edmx.AppendLine(
                $@"      <Schema Namespace=""{Namespace}"" Alias=""Self"" xmlns=""http://schemas.microsoft.com/ado/2009/11/edm"">");

            foreach (var entity in _entities)
            {
                BuildEntity(edmx, entity);
            }

            foreach (var association in _associations)
            {
                BuildAssociation(edmx, association);
            }

            BuildContainer(edmx);

            edmx.AppendLine(@"      </Schema>");
            edmx.AppendLine(@"    </edmx:ConceptualModels>");
            edmx.AppendLine(@"  </edmx:Runtime>");
            edmx.AppendLine(@"  <Designer xmlns=""http://schemas.microsoft.com/ado/2009/11/edmx"">");
            edmx.AppendLine(@"    <Diagrams>");
            edmx.AppendLine(
                $@"      <Diagram DiagramId=""aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"" Name=""Diagram1"" ZoomLevel=""100"" LayoutMode=""Modern"" EnableGrouping=""{(_enableGrouping ? "true" : "false")}"" GenerateGroupNames=""{(_generateGroupNames ? "true" : "false")}"">");

            BuildDiagram(edmx);

            edmx.AppendLine(@"      </Diagram>");
            edmx.AppendLine(@"    </Diagrams>");
            edmx.AppendLine(@"  </Designer>");
            edmx.AppendLine(@"</edmx:Edmx>");

            return edmx.ToString();
        }

        /// <summary>
        ///     Renders one association and the navigation properties at both ends.
        /// </summary>
        private static void BuildAssociation(StringBuilder edmx, Association association)
        {
            var multiplicity = association.Optional ? "0..1" : "1";

            edmx.AppendLine($@"        <Association Name=""{association.Name}"">");
            edmx.AppendLine(
                $@"          <End Role=""{association.Principal}"" Type=""Self.{association.Principal}"" Multiplicity=""{multiplicity}"" />");
            edmx.AppendLine(
                $@"          <End Role=""{association.Dependent}"" Type=""Self.{association.Dependent}"" Multiplicity=""*"" />");
            edmx.AppendLine(@"        </Association>");
        }

        /// <summary>
        ///     Renders the entity container, which the model is not valid without.
        /// </summary>
        private void BuildContainer(StringBuilder edmx)
        {
            edmx.AppendLine($@"        <EntityContainer Name=""{Namespace}Container"">");

            foreach (var entity in _entities)
            {
                edmx.AppendLine($@"          <EntitySet Name=""{entity.Name}Set"" EntityType=""Self.{entity.Name}"" />");
            }

            foreach (var association in _associations)
            {
                edmx.AppendLine(
                    $@"          <AssociationSet Name=""{association.Name}"" Association=""Self.{association.Name}"">");
                edmx.AppendLine(
                    $@"            <End Role=""{association.Principal}"" EntitySet=""{association.Principal}Set"" />");
                edmx.AppendLine(
                    $@"            <End Role=""{association.Dependent}"" EntitySet=""{association.Dependent}Set"" />");
                edmx.AppendLine(@"          </AssociationSet>");
            }

            edmx.AppendLine(@"        </EntityContainer>");
        }

        /// <summary>
        ///     Renders the shapes and connectors the diagram holds.
        /// </summary>
        private void BuildDiagram(StringBuilder edmx)
        {
            var index = 0;

            foreach (var entity in _entities)
            {
                var fill = entity.FillColor.HasValue
                    ? $@" FillColor=""{ColorTranslator.ToHtml(entity.FillColor.Value)}"""
                    : string.Empty;
                var group = string.IsNullOrWhiteSpace(entity.GroupName)
                    ? string.Empty
                    : $@" GroupName=""{entity.GroupName}""";

                // Stacked in a column, so a test that asserts the layout moved things has somewhere to move them
                // from. The positions themselves carry no meaning.
                var y = (index++ * 2.0).ToString("F3", CultureInfo.InvariantCulture);

                edmx.AppendLine(
                    $@"        <EntityTypeShape EntityType=""{Namespace}.{entity.Name}"" Width=""1.5"" PointX=""0.750"" PointY=""{y}"" IsExpanded=""true""{fill}{group} />");
            }

            foreach (var association in _associations)
            {
                var manual = association.ManuallyRouted ? "true" : "false";

                if (association.ConnectorPoints is null || association.ConnectorPoints.Length == 0)
                {
                    edmx.AppendLine(
                        $@"        <AssociationConnector Association=""{Namespace}.{association.Name}"" ManuallyRouted=""{manual}"" />");
                    continue;
                }

                edmx.AppendLine(
                    $@"        <AssociationConnector Association=""{Namespace}.{association.Name}"" ManuallyRouted=""{manual}"">");
                foreach (var point in association.ConnectorPoints)
                {
                    edmx.AppendLine(
                        $@"          <ConnectorPoint PointX=""{point.X.ToString("F3", CultureInfo.InvariantCulture)}"" PointY=""{point.Y.ToString("F3", CultureInfo.InvariantCulture)}"" />");
                }
                edmx.AppendLine(@"        </AssociationConnector>");
            }
        }

        /// <summary>
        ///     Renders one entity, its key, its scalar properties and its navigation properties.
        /// </summary>
        private void BuildEntity(StringBuilder edmx, Entity entity)
        {
            edmx.AppendLine($@"        <EntityType Name=""{entity.Name}"">");
            edmx.AppendLine(@"          <Key><PropertyRef Name=""Id"" /></Key>");
            edmx.AppendLine(@"          <Property Name=""Id"" Type=""Int32"" Nullable=""false"" />");

            foreach (var property in entity.ScalarProperties)
            {
                edmx.AppendLine($@"          <Property Name=""{property}"" Type=""Int32"" Nullable=""true"" />");
            }

            foreach (var association in _associations.Where(candidate => candidate.Principal == entity.Name))
            {
                edmx.AppendLine(
                    $@"          <NavigationProperty Name=""{association.Dependent}Items"" Relationship=""Self.{association.Name}"" FromRole=""{association.Principal}"" ToRole=""{association.Dependent}"" />");
            }

            foreach (var association in _associations.Where(candidate => candidate.Dependent == entity.Name))
            {
                edmx.AppendLine(
                    $@"          <NavigationProperty Name=""{association.Principal}Ref"" Relationship=""Self.{association.Name}"" FromRole=""{association.Dependent}"" ToRole=""{association.Principal}"" />");
            }

            edmx.AppendLine(@"        </EntityType>");
        }

        #endregion

        #region Nested Types

        /// <summary>
        ///     One association waiting to be rendered.
        /// </summary>
        private sealed class Association
        {

            #region Properties

            public (double X, double Y)[] ConnectorPoints { get; set; }

            public string Dependent { get; }

            public bool ManuallyRouted { get; set; }

            public string Name { get; }

            public bool Optional { get; }

            public string Principal { get; }

            #endregion

            #region Constructors

            public Association(string principal, string dependent, bool optional)
            {
                Principal = principal;
                Dependent = dependent;
                Optional = optional;
                Name = $"FK_{dependent}_{principal}";
            }

            #endregion

        }

        /// <summary>
        ///     One entity waiting to be rendered.
        /// </summary>
        private sealed class Entity
        {

            #region Properties

            public Color? FillColor { get; set; }

            public string GroupName { get; set; }

            public string Name { get; }

            public List<string> ScalarProperties { get; }

            #endregion

            #region Constructors

            public Entity(string name, IEnumerable<string> scalarProperties)
            {
                Name = name;
                ScalarProperties = scalarProperties?.ToList() ?? [];
            }

            #endregion

        }

        #endregion

    }
}
