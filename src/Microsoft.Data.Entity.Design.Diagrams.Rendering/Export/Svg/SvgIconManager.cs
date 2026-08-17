// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Data.Entity.Design.Dsl.View.Export
{

    /// <summary>
    /// Manages SVG icon loading from embedded resources and converts them to reusable symbols.
    /// Icons are defined once as &lt;symbol&gt; elements in &lt;defs&gt; and referenced via &lt;use&gt;.
    /// </summary>
    /// <remarks>
    /// Emitting each icon once as a symbol and referencing it keeps the exported document small even on diagrams
    /// with hundreds of properties, where the same handful of icons would otherwise be inlined repeatedly.
    /// Usage is tracked as references are handed out so that only the symbols actually needed end up in
    /// &lt;defs&gt;, and the emitted set is sorted by name so repeated exports of the same diagram are
    /// byte-for-byte identical.
    /// </remarks>
    internal class SvgIconManager
    {

        #region Fields

        /// <summary>
        /// The nominal edge length, in user units, of an icon as authored in its source SVG.
        /// </summary>
        private const double DefaultIconSize = 16.0;

        /// <summary>
        /// The prefix applied to every generated symbol id, keeping icon ids from colliding with other
        /// element ids in the exported document.
        /// </summary>
        private const string IconPrefix = "icon-";

        /// <summary>
        /// The fragment that identifies an embedded resource as an icon within the assembly's resource names.
        /// </summary>
        private const string IconResourcePath = ".Export.Svg.Icons.";

        /// <summary>
        /// The generated &lt;symbol&gt; markup for each successfully loaded icon, keyed by icon name.
        /// </summary>
        /// <remarks>
        /// Case-insensitive so callers can refer to icons without matching the exact resource file casing.
        /// </remarks>
        private readonly Dictionary<string, string> _iconSymbols;

        /// <summary>
        /// The names of icons referenced since the last reset, used to emit only the symbols that are needed.
        /// </summary>
        private readonly HashSet<string> _usedIcons;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the SvgIconManager class and loads all icons from embedded resources.
        /// </summary>
        /// <remarks>
        /// Loading happens once up front because the resource set is fixed at compile time, so there is nothing
        /// to gain from deferring it and a lot to gain from a single predictable pass.
        /// </remarks>
        public SvgIconManager()
        {
            _iconSymbols = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _usedIcons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            LoadIconsFromResources();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets all symbol definitions regardless of usage.
        /// </summary>
        /// <returns>The concatenated &lt;symbol&gt; markup for every loaded icon, one per line, ordered by icon name.</returns>
        /// <remarks>
        /// Intended for diagnostics and tooling; normal export uses <see cref="GetUsedSymbolDefinitions"/> so the
        /// document only carries the icons it actually references.
        /// </remarks>
        public string GetAllSymbolDefinitions()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var kvp in _iconSymbols.OrderBy(k => k.Key))
            {
                sb.AppendLine(kvp.Value);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets a list of all available icon names.
        /// </summary>
        /// <returns>The names of every successfully loaded icon, ordered alphabetically.</returns>
        public IEnumerable<string> GetAvailableIcons()
        {
            return _iconSymbols.Keys.OrderBy(k => k);
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
        /// <remarks>
        /// Requesting a reference is what marks an icon as used, so this must run before
        /// <see cref="GetUsedSymbolDefinitions"/> for the symbol to reach the document. The name is recorded even
        /// when no such icon was loaded; the resulting reference simply resolves to nothing, which keeps a missing
        /// resource from aborting an export. Size comes from a CSS class rather than width and height attributes
        /// so the dimensions are declared once in the stylesheet instead of on every element.
        /// </remarks>
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
        /// Gets all symbol definitions for icons that have been used.
        /// Call this after all GetIconReference calls to get only the symbols needed.
        /// </summary>
        /// <returns>
        /// The concatenated &lt;symbol&gt; markup for every referenced icon, one per line, ordered by icon name,
        /// or an empty string when no icon has been referenced.
        /// </returns>
        /// <remarks>
        /// Names that were referenced but never loaded are skipped silently, so a missing icon resource degrades
        /// to an unresolved reference rather than a failed export. Ordering by name keeps output deterministic
        /// despite the underlying set being unordered.
        /// </remarks>
        public string GetUsedSymbolDefinitions()
        {
            if (_usedIcons.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
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
        /// Checks if an icon with the given name exists.
        /// </summary>
        /// <param name="iconName">The name of the icon to look for. Matching is case-insensitive.</param>
        /// <returns><see langword="true"/> when a symbol was loaded for that name; otherwise <see langword="false"/>.</returns>
        public bool HasIcon(string iconName)
        {
            return _iconSymbols.ContainsKey(iconName);
        }

        /// <summary>
        /// Resets the used icons tracking. Call before generating a new SVG.
        /// </summary>
        /// <remarks>
        /// Without this, a manager reused across exports would carry icons from the previous diagram into the next
        /// document's &lt;defs&gt;. The loaded symbols themselves are untouched.
        /// </remarks>
        public void ResetUsedIcons()
        {
            _usedIcons.Clear();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Converts an SVG file content to a symbol definition.
        /// Extracts the content from the level-1 group and applies the viewBox.
        /// </summary>
        /// <param name="iconName">The icon name, used to build the symbol id.</param>
        /// <param name="svgContent">The raw text of the source SVG file.</param>
        /// <returns>
        /// The &lt;symbol&gt; markup for the icon, or <see langword="null"/> when the file contains nothing that
        /// can be turned into one.
        /// </returns>
        /// <remarks>
        /// The icons ship in the Visual Studio image format, where the drawing lives in a group named "level-1"
        /// alongside a transparent canvas rectangle. When that group is missing the loose &lt;path&gt; elements are
        /// used instead, so hand-authored icons still work. A viewBox of "0 0 16 16" is assumed when the source
        /// declares none, matching <see cref="DefaultIconSize"/>. Parsing is done with regular expressions rather
        /// than an XML reader because the input is a small, known-shape file and the output must be reproduced
        /// verbatim.
        /// </remarks>
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

                StringBuilder paths = new StringBuilder();
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
        /// Extracts the icon name from the resource name.
        /// </summary>
        /// <param name="resourceName">The fully qualified manifest resource name.</param>
        /// <returns>The bare icon name, without namespace or file extension.</returns>
        /// <remarks>
        /// The search for the separating dot starts before the ".svg" suffix so that the extension's own dot is
        /// not mistaken for the end of the namespace.
        /// </remarks>
        private string ExtractIconName(string resourceName)
        {
            // Resource name format: Namespace.CustomCode.Export.Svg.Icons.IconName.svg
            var fileName = resourceName.Substring(resourceName.LastIndexOf('.', resourceName.Length - 5) + 1);
            return fileName.Replace(".svg", string.Empty);
        }

        /// <summary>
        /// Loads all SVG icons from embedded assembly resources.
        /// </summary>
        /// <remarks>
        /// Failures are swallowed per icon on purpose: a single malformed or unreadable resource should cost that
        /// one icon, not the whole export. Icons that convert to nothing are left out of the dictionary so that
        /// <see cref="HasIcon"/> reports them honestly.
        /// </remarks>
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

                        using (StreamReader reader = new StreamReader(stream))
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
        /// Normalizes a path element by converting class references to use consolidated styles.
        /// </summary>
        /// <param name="element">The raw element markup taken from the source icon.</param>
        /// <returns>The element with its source class names rewritten to the exporter's shared class names.</returns>
        /// <remarks>
        /// The source icons carry Visual Studio theme class names. Rewriting them to a small shared set lets the
        /// colours be declared once in <see cref="SvgStylesheetManager.GetStyleDefinitions"/> instead of being
        /// repeated on every element, and keeps the exported document themeable.
        /// </remarks>
        private string NormalizePathElement(string element)
        {
            // Map original class names to our consolidated class names
            Dictionary<string, string> classMap = new Dictionary<string, string>
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
        /// Processes the inner content of a symbol, normalizing class names and indentation.
        /// </summary>
        /// <param name="content">The inner markup of the icon's level-1 group.</param>
        /// <returns>The drawable elements, one per line, each normalized and indented for the symbol body.</returns>
        /// <remarks>
        /// The transparent canvas element is dropped because the symbol is placed over the diagram's own
        /// background; keeping it would paint an opaque box behind every icon.
        /// </remarks>
        private string ProcessSymbolContent(string content)
        {
            StringBuilder sb = new StringBuilder();

            // Find all path and g elements
            Regex elementRegex = new Regex(@"<(path|g|polygon|circle|rect|line)[^>]*(?:/>|>.*?</\1>)",
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

        #endregion

    }

}
