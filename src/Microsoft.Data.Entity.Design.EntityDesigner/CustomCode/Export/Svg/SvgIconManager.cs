// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.EntityDesigner.View.Export
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Manages SVG icon loading from embedded resources and converts them to reusable symbols.
    /// Icons are defined once as &lt;symbol&gt; elements in &lt;defs&gt; and referenced via &lt;use&gt;.
    /// </summary>
    internal class SvgIconManager
    {
        private const double DefaultIconSize = 16.0;
        private const string IconPrefix = "icon-";
        private const string IconResourcePath = ".CustomCode.Export.Svg.Icons.";

        private readonly Dictionary<string, string> _iconSymbols;
        private readonly HashSet<string> _usedIcons;

        /// <summary>
        /// Initializes a new instance of the SvgIconManager class and loads all icons from embedded resources.
        /// </summary>
        public SvgIconManager()
        {
            _iconSymbols = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _usedIcons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            LoadIconsFromResources();
        }

        /// <summary>
        /// Loads all SVG icons from embedded assembly resources.
        /// </summary>
        private void LoadIconsFromResources()
        {
            var assembly = typeof(SvgIconManager).Assembly;
            var resourceNames = assembly.GetManifestResourceNames()
                .Where(n => n.Contains(IconResourcePath) && n.EndsWith(".svg", StringComparison.OrdinalIgnoreCase));

            foreach (var resourceName in resourceNames)
            {
                try
                {
                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream is null)
                        {
                            continue;
                        }

                        using (var reader = new StreamReader(stream))
                        {
                            var svgContent = reader.ReadToEnd();
                            var iconName = ExtractIconName(resourceName);
                            var symbolContent = ConvertToSymbol(iconName, svgContent);

                            if (!string.IsNullOrEmpty(symbolContent))
                            {
                                _iconSymbols[iconName] = symbolContent;
                            }
                        }
                    }
                }
                catch
                {
                    // Skip icons that fail to load
                }
            }
        }

        /// <summary>
        /// Extracts the icon name from the resource name.
        /// </summary>
        private string ExtractIconName(string resourceName)
        {
            // Resource name format: Namespace.CustomCode.Export.Svg.Icons.IconName.svg
            var fileName = resourceName.Substring(resourceName.LastIndexOf('.', resourceName.Length - 5) + 1);
            return fileName.Replace(".svg", string.Empty);
        }

        /// <summary>
        /// Converts an SVG file content to a symbol definition.
        /// Extracts the content from the level-1 group and applies the viewBox.
        /// </summary>
        private string ConvertToSymbol(string iconName, string svgContent)
        {
            // Extract viewBox from the SVG
            var viewBoxMatch = Regex.Match(svgContent, @"viewBox=""([^""]+)""");
            var viewBox = viewBoxMatch.Success ? viewBoxMatch.Groups[1].Value : "0 0 16 16";

            // Extract the level-1 group content (the actual icon paths)
            var level1Match = Regex.Match(svgContent, @"<g\s+id=""level-1""[^>]*>(.*?)</g>\s*</svg>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

            if (!level1Match.Success)
            {
                // Fallback: try to extract any path elements
                var pathMatches = Regex.Matches(svgContent, @"<path[^>]+/>");
                if (pathMatches.Count == 0)
                {
                    return null;
                }

                var paths = new StringBuilder();
                foreach (Match match in pathMatches)
                {
                    var pathElement = match.Value;
                    // Skip canvas paths
                    if (!pathElement.Contains("class=\"canvas\""))
                    {
                        paths.AppendLine("      " + NormalizePathElement(pathElement));
                    }
                }

                return string.Format(
                    CultureInfo.InvariantCulture,
                    "    <symbol id=\"{0}{1}\" viewBox=\"{2}\" width=\"16\" height=\"16\">\n{3}    </symbol>",
                    IconPrefix,
                    iconName,
                    viewBox,
                    paths);
            }

            // Process the level-1 content
            var innerContent = level1Match.Groups[1].Value;
            var processedContent = ProcessSymbolContent(innerContent);

            return string.Format(
                CultureInfo.InvariantCulture,
                "    <symbol id=\"{0}{1}\" viewBox=\"{2}\" width=\"16\" height=\"16\">\n{3}    </symbol>",
                IconPrefix,
                iconName,
                viewBox,
                processedContent);
        }

        /// <summary>
        /// Processes the inner content of a symbol, normalizing class names and indentation.
        /// </summary>
        private string ProcessSymbolContent(string content)
        {
            var sb = new StringBuilder();

            // Find all path and g elements
            var elementRegex = new Regex(@"<(path|g|polygon|circle|rect|line)[^>]*(?:/>|>.*?</\1>)",
                RegexOptions.Singleline);

            foreach (Match match in elementRegex.Matches(content))
            {
                var element = match.Value;

                // Skip canvas elements
                if (element.Contains("class=\"canvas\""))
                {
                    continue;
                }

                // Normalize the element
                var normalized = NormalizePathElement(element);
                sb.AppendLine("      " + normalized);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Normalizes a path element by converting class references to use consolidated styles.
        /// </summary>
        private string NormalizePathElement(string element)
        {
            // Map original class names to our consolidated class names
            var classMap = new Dictionary<string, string>
            {
                { "light-defaultgrey-10", "icon-shadow" },
                { "light-defaultgrey", "icon-fill" },
                { "light-yellow-10", "icon-accent-shadow" },
                { "light-yellow", "icon-accent" },
                { "light-blue", "icon-blue" },
                { "cls-1", "icon-muted" }
            };

            var result = element;
            foreach (var mapping in classMap)
            {
                result = result.Replace(
                    string.Format(CultureInfo.InvariantCulture, "class=\"{0}\"", mapping.Key),
                    string.Format(CultureInfo.InvariantCulture, "class=\"{0}\"", mapping.Value));
            }

            return result;
        }

        /// <summary>
        /// Gets all symbol definitions for icons that have been used.
        /// Call this after all GetIconReference calls to get only the symbols needed.
        /// </summary>
        public string GetUsedSymbolDefinitions()
        {
            if (_usedIcons.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            foreach (var iconName in _usedIcons.OrderBy(n => n))
            {
                if (_iconSymbols.TryGetValue(iconName, out var symbol))
                {
                    sb.AppendLine(symbol);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets all symbol definitions regardless of usage.
        /// </summary>
        public string GetAllSymbolDefinitions()
        {
            var sb = new StringBuilder();
            foreach (var kvp in _iconSymbols.OrderBy(k => k.Key))
            {
                sb.AppendLine(kvp.Value);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets a &lt;use&gt; reference for a specific icon at the given position using default size (16x16).
        /// </summary>
        /// <param name="iconName">The name of the icon (e.g., "Property", "NavigationProperty").</param>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <returns>An SVG &lt;use&gt; element string.</returns>
        public string GetIconReference(string iconName, double x, double y)
        {
            return GetIconReference(iconName, x, y, small: false);
        }

        /// <summary>
        /// Gets a &lt;use&gt; reference for a specific icon at the given position with optional small size.
        /// Size is controlled via CSS classes (.icon for 16x16, .icon-sm for 14x14).
        /// </summary>
        /// <param name="iconName">The name of the icon (e.g., "Property", "NavigationProperty").</param>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <param name="small">If true, uses the small icon size (14x14). Default is false (16x16).</param>
        /// <returns>An SVG &lt;use&gt; element string.</returns>
        public string GetIconReference(string iconName, double x, double y, bool small)
        {
            // Track that this icon is being used
            _usedIcons.Add(iconName);

            var cssClass = small ? "icon-sm" : "icon";

            return string.Format(
                CultureInfo.InvariantCulture,
                "<use href=\"#{0}{1}\" x=\"{2}\" y=\"{3}\" class=\"{4}\"/>",
                IconPrefix,
                iconName,
                SvgStylesheetManager.FormatDouble(x),
                SvgStylesheetManager.FormatDouble(y),
                cssClass);
        }

        /// <summary>
        /// Resets the used icons tracking. Call before generating a new SVG.
        /// </summary>
        public void ResetUsedIcons()
        {
            _usedIcons.Clear();
        }

        /// <summary>
        /// Gets a list of all available icon names.
        /// </summary>
        public IEnumerable<string> GetAvailableIcons()
        {
            return _iconSymbols.Keys.OrderBy(k => k);
        }

        /// <summary>
        /// Checks if an icon with the given name exists.
        /// </summary>
        public bool HasIcon(string iconName)
        {
            return _iconSymbols.ContainsKey(iconName);
        }
    }
}
