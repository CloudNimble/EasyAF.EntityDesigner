// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.EntityDesigner.View.Export
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Microsoft.Data.Entity.Design.EntityDesigner.ViewModel;
    using Microsoft.VisualStudio.Modeling.Diagrams;

    /// <summary>
    /// Exports an EntityDesignerDiagram to Mermaid ER diagram format.
    /// </summary>
    internal class MermaidExporter
    {
        /// <summary>
        /// Exports the diagram to a Mermaid (.mmd) file using DiagramExportOptions.
        /// </summary>
        /// <param name="diagram">The diagram to export.</param>
        /// <param name="options">The export options specifying path and settings.</param>
        public void Export(EntityDesignerDiagram diagram, DiagramExportOptions options)
        {
            if (diagram == null)
            {
                throw new ArgumentNullException("diagram");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            var mermaidContent = GenerateMermaid(diagram, options.ShowTypes);
            File.WriteAllText(options.FilePath, mermaidContent, Encoding.UTF8);
        }

        /// <summary>
        /// Exports the diagram to a Mermaid (.mmd) file.
        /// </summary>
        /// <param name="diagram">The diagram to export.</param>
        /// <param name="filePath">The path to save the Mermaid file.</param>
        public void ExportToMermaid(EntityDesignerDiagram diagram, string filePath)
        {
            ExportToMermaid(diagram, filePath, showTypes: true);
        }

        /// <summary>
        /// Exports the diagram to a Mermaid (.mmd) file with export options.
        /// </summary>
        /// <param name="diagram">The diagram to export.</param>
        /// <param name="filePath">The path to save the Mermaid file.</param>
        /// <param name="showTypes">If true, shows data types alongside property names.</param>
        public void ExportToMermaid(EntityDesignerDiagram diagram, string filePath, bool showTypes)
        {
            if (diagram == null)
            {
                throw new ArgumentNullException("diagram");
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            var mermaidContent = GenerateMermaid(diagram, showTypes);
            File.WriteAllText(filePath, mermaidContent, Encoding.UTF8);
        }

        /// <summary>
        /// Generates Mermaid ER diagram content for the diagram.
        /// </summary>
        public string GenerateMermaid(EntityDesignerDiagram diagram)
        {
            return GenerateMermaid(diagram, showTypes: true);
        }

        /// <summary>
        /// Generates Mermaid ER diagram content for the diagram with export options.
        /// </summary>
        /// <param name="diagram">The diagram to export.</param>
        /// <param name="showTypes">If true, shows data types alongside property names.</param>
        public string GenerateMermaid(EntityDesignerDiagram diagram, bool showTypes)
        {
            if (diagram == null)
            {
                throw new ArgumentNullException("diagram");
            }

            var sb = new StringBuilder();

            // Mermaid ER diagram header
            sb.AppendLine("erDiagram");

            // Collect all entities and relationships
            var entities = new List<EntityTypeShape>();
            var associations = new List<AssociationConnector>();
            var inheritances = new List<InheritanceConnector>();

            foreach (ShapeElement shape in diagram.NestedChildShapes)
            {
                var entityTypeShape = shape as EntityTypeShape;
                if (entityTypeShape != null)
                {
                    entities.Add(entityTypeShape);
                    continue;
                }

                var associationConnector = shape as AssociationConnector;
                if (associationConnector != null)
                {
                    associations.Add(associationConnector);
                    continue;
                }

                var inheritanceConnector = shape as InheritanceConnector;
                if (inheritanceConnector != null)
                {
                    inheritances.Add(inheritanceConnector);
                }
            }

            // Render relationships first (Mermaid convention)
            foreach (var connector in associations)
            {
                sb.Append(RenderAssociation(connector));
            }

            foreach (var connector in inheritances)
            {
                sb.Append(RenderInheritance(connector));
            }

            // Add a blank line between relationships and entity definitions
            if (associations.Count > 0 || inheritances.Count > 0)
            {
                sb.AppendLine();
            }

            // Render entity definitions
            foreach (var entityShape in entities)
            {
                sb.Append(RenderEntity(entityShape, showTypes));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Renders an entity type shape as a Mermaid entity definition.
        /// </summary>
        private string RenderEntity(EntityTypeShape shape, bool showTypes)
        {
            if (shape == null || shape.ModelElement == null)
            {
                return string.Empty;
            }

            var entityType = shape.ModelElement as EntityType;
            if (entityType == null)
            {
                return string.Empty;
            }

            var entityName = SanitizeName(entityType.Name);
            var sb = new StringBuilder();

            sb.AppendFormat(CultureInfo.InvariantCulture, "    {0} {{\n", entityName);

            // Render scalar properties
            var propertiesCompartment = shape.PropertiesCompartment;
            if (propertiesCompartment != null)
            {
                foreach (var item in propertiesCompartment.Items)
                {
                    var scalarProp = item as ScalarProperty;
                    if (scalarProp != null)
                    {
                        var propType = showTypes ? SanitizeType(scalarProp.Type) : "property";
                        var propName = SanitizeName(scalarProp.Name);
                        var keyIndicator = scalarProp.EntityKey ? " PK" : "";

                        sb.AppendFormat(
                            CultureInfo.InvariantCulture,
                            "        {0} {1}{2}\n",
                            propType,
                            propName,
                            keyIndicator);
                        continue;
                    }

                    var complexProp = item as ComplexProperty;
                    if (complexProp != null)
                    {
                        var propName = SanitizeName(complexProp.Name);
                        sb.AppendFormat(
                            CultureInfo.InvariantCulture,
                            "        complex {0}\n",
                            propName);
                    }
                }
            }

            // Render navigation properties
            var navigationCompartment = shape.NavigationCompartment;
            if (navigationCompartment != null)
            {
                foreach (var item in navigationCompartment.Items)
                {
                    var navProp = item as NavigationProperty;
                    if (navProp != null)
                    {
                        var propName = SanitizeName(navProp.Name);
                        sb.AppendFormat(
                            CultureInfo.InvariantCulture,
                            "        navigation {0}\n",
                            propName);
                    }
                }
            }

            sb.AppendLine("    }");

            return sb.ToString();
        }

        /// <summary>
        /// Renders an association connector as a Mermaid relationship.
        /// </summary>
        private string RenderAssociation(AssociationConnector connector)
        {
            if (connector == null || connector.ModelElement == null)
            {
                return string.Empty;
            }

            var association = connector.ModelElement as Association;
            if (association == null || association.SourceEntityType == null || association.TargetEntityType == null)
            {
                return string.Empty;
            }

            var sourceName = SanitizeName(association.SourceEntityType.Name);
            var targetName = SanitizeName(association.TargetEntityType.Name);
            var relationshipSymbol = GetMermaidRelationship(
                association.SourceMultiplicity,
                association.TargetMultiplicity);

            // Use association name as the relationship label, or empty if not available
            var relationshipLabel = !string.IsNullOrEmpty(association.Name)
                ? SanitizeName(association.Name)
                : "relates";

            return string.Format(
                CultureInfo.InvariantCulture,
                "    {0} {1} {2} : {3}\n",
                sourceName,
                relationshipSymbol,
                targetName,
                relationshipLabel);
        }

        /// <summary>
        /// Renders an inheritance connector as a Mermaid relationship.
        /// </summary>
        private string RenderInheritance(InheritanceConnector connector)
        {
            if (connector == null || connector.ModelElement == null)
            {
                return string.Empty;
            }

            var inheritance = connector.ModelElement as Inheritance;
            if (inheritance == null || inheritance.SourceEntityType == null || inheritance.TargetEntityType == null)
            {
                return string.Empty;
            }

            // Source is derived class, Target is base class
            var derivedName = SanitizeName(inheritance.SourceEntityType.Name);
            var baseName = SanitizeName(inheritance.TargetEntityType.Name);

            // Mermaid doesn't have native inheritance support in ER diagrams,
            // so we represent it as a special relationship
            return string.Format(
                CultureInfo.InvariantCulture,
                "    {0} ||--|| {1} : inherits\n",
                derivedName,
                baseName);
        }

        /// <summary>
        /// Converts EF multiplicity values to Mermaid relationship notation.
        /// </summary>
        /// <param name="sourceMultiplicity">Source end multiplicity (e.g., "1", "0..1", "*").</param>
        /// <param name="targetMultiplicity">Target end multiplicity (e.g., "1", "0..1", "*").</param>
        /// <returns>Mermaid relationship symbol.</returns>
        internal string GetMermaidRelationship(string sourceMultiplicity, string targetMultiplicity)
        {
            // Mermaid notation:
            // ||  exactly one
            // o|  zero or one
            // }|  one or more
            // }o  zero or more (many)
            //
            // Full relationship: <left-symbol>--<right-symbol>
            // Examples: ||--o{ means "exactly one to zero or more"

            var sourceSymbol = GetMermaidMultiplicity(sourceMultiplicity, isSource: true);
            var targetSymbol = GetMermaidMultiplicity(targetMultiplicity, isSource: false);

            return string.Format(CultureInfo.InvariantCulture, "{0}--{1}", sourceSymbol, targetSymbol);
        }

        /// <summary>
        /// Converts a single multiplicity value to Mermaid notation.
        /// </summary>
        internal string GetMermaidMultiplicity(string multiplicity, bool isSource)
        {
            // Normalize the multiplicity string
            var normalized = (multiplicity ?? "").Trim().ToLowerInvariant();

            // For source side (left of --), symbols read right-to-left
            // For target side (right of --), symbols read left-to-right
            switch (normalized)
            {
                case "1":
                    return "||";
                case "0..1":
                    return isSource ? "|o" : "o|";
                case "*":
                case "0..*":
                    return isSource ? "}o" : "o{";
                case "1..*":
                    return isSource ? "}|" : "|{";
                default:
                    // Default to zero or more
                    return isSource ? "}o" : "o{";
            }
        }

        /// <summary>
        /// Sanitizes a name for use in Mermaid diagrams.
        /// Mermaid entity names cannot contain spaces or special characters.
        /// </summary>
        internal static string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "Unknown";
            }

            // Replace spaces and special characters with underscores
            var result = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    result.Append(c);
                }
                else
                {
                    result.Append('_');
                }
            }

            // Ensure name doesn't start with a digit
            if (result.Length > 0 && char.IsDigit(result[0]))
            {
                result.Insert(0, '_');
            }

            return result.Length > 0 ? result.ToString() : "Unknown";
        }

        /// <summary>
        /// Sanitizes a type name for use in Mermaid diagrams.
        /// </summary>
        internal static string SanitizeType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return "unknown";
            }

            // Remove common prefixes and simplify type names
            var simplified = typeName
                .Replace("System.", "")
                .Replace("Nullable<", "")
                .Replace(">", "")
                .Replace("?", "");

            return SanitizeName(simplified).ToLowerInvariant();
        }
    }
}
