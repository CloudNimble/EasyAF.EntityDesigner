// Builds the rename catalog: every place an assembly name or namespace root appears as a STRING,
// where the compiler cannot help. Emits markdown.

using System.Text;
using System.Text.RegularExpressions;

var root = @"D:\GitHub\EasyAF.EntityDesigner\src";

// old assembly name -> new assembly name
var renames = new Dictionary<string, string>
{
    // Container renamed in step 2. Any hit on these two is remaining work, not a pending rename.
    ["Microsoft.Data.Tools.Design.XmlCore"] = "DONE — assembly is now Microsoft.Data.Entity.Design.XmlEngine",
    ["Microsoft.Data.Tools.XmlDesignerBase"] = "step 4 — old root namespace, Roslyn moves this",
    ["Microsoft.Data.Entity.Design.Model"] = "DONE - now Microsoft.Data.Entity.Design.Edmx",
    ["Microsoft.Data.Entity.Design.VersioningFacade"] = "DONE - now Microsoft.Data.Entity.Design.EntityFramework",
    ["Microsoft.Data.Entity.Design.Dsl"] = "DONE - now Microsoft.Data.Entity.Design.Diagrams",
    ["Microsoft.Data.Entity.Design.Renderer"] = "DONE - now Microsoft.Data.Entity.Design.Diagrams.Rendering",
    ["Microsoft.Data.Entity.Design.DatabaseGeneration"] = "Microsoft.Data.Entity.Design.DatabaseGeneration",
    ["Microsoft.VisualStudio.Data.Entity.Design"] = "DONE - now Microsoft.VisualStudio.Data.Entity.EdmxDesigner",
    ["Microsoft.VisualStudio.Data.Tools.Design.XmlCore"] = "DONE - now Microsoft.VisualStudio.Data.Entity.XmlDesigner",
    ["Microsoft.Data.Entity.Design.Extensibility"] = "DONE - now Microsoft.VisualStudio.Data.Entity.Extensibility",
    ["Microsoft.Data.Entity.Design.Package"] = "DONE - now Microsoft.VisualStudio.Data.Entity.Package",
    ["Microsoft.Data.Entity.Tools"] = "DONE - now Microsoft.Data.Entity.Design.Diagrams.Tools",
};

// Longest first so Design.Model never matches inside Design.Model.Something we care about separately.
var ordered = renames.Keys.OrderByDescending(k => k.Length).ToList();

var extensions = new[] { ".cs", ".csproj", ".xaml", ".resx", ".pkgdef", ".vsct", ".tt", ".dsl", ".vsixmanifest", ".slnx", ".props", ".targets", ".config", ".json", ".md" };

var files = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
    .Where(f => extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
    .Where(f => !f.Contains(@"\obj\") && !f.Contains(@"\bin\"))
    .OrderBy(f => f)
    .ToList();

// Contexts where a rename is a STRING, not a symbol the compiler resolves.
var stringContexts = new (string Label, Regex Pattern)[]
{
    ("InternalsVisibleTo",   new Regex(@"InternalsVisibleTo", RegexOptions.IgnoreCase)),
    ("pack:// URI",          new Regex(@"pack://", RegexOptions.IgnoreCase)),
    ("XAML clr-namespace",   new Regex(@"clr-namespace", RegexOptions.IgnoreCase)),
    ("assembly= in XAML",    new Regex(@"assembly=", RegexOptions.IgnoreCase)),
    (".dll literal",         new Regex(@"\.dll", RegexOptions.IgnoreCase)),
    ("pkgdef CodeBase",      new Regex(@"CodeBase", RegexOptions.IgnoreCase)),
    ("pkgdef Class",         new Regex(@"^\s*""Class""", RegexOptions.IgnoreCase | RegexOptions.Multiline)),
    ("ResXFileRef",          new Regex(@"ResXFileRef", RegexOptions.IgnoreCase)),
    ("DslDefinition ns",     new Regex(@"Namespace=""", RegexOptions.IgnoreCase)),
    ("T4 include/output",    new Regex(@"<#@", RegexOptions.IgnoreCase)),
    ("typeof/nameof string", new Regex(@"Type\.GetType\(|Assembly\.Load", RegexOptions.IgnoreCase)),
};

var hits = new List<(string File, int Line, string Context, string OldName, string Text)>();

foreach (var file in files)
{
    string[] lines;
    try { lines = File.ReadAllLines(file); } catch { continue; }

    for (var i = 0; i < lines.Length; i++)
    {
        var line = lines[i];
        var old = ordered.FirstOrDefault(o => line.Contains(o, StringComparison.Ordinal));

        if (old is null) continue;

        foreach (var (label, pattern) in stringContexts)
        {
            if (!pattern.IsMatch(line)) continue;

            hits.Add((
                Path.GetRelativePath(root, file).Replace('\\', '/'),
                i + 1,
                label,
                old,
                line.Trim().Length > 150 ? line.Trim()[..150] + "…" : line.Trim()));
            break;
        }
    }
}

var sb = new StringBuilder();
sb.AppendLine("| File | Line | Kind | Names | Text |");
sb.AppendLine("|---|---:|---|---|---|");

foreach (var h in hits.OrderBy(h => h.Context).ThenBy(h => h.File).ThenBy(h => h.Line))
{
    var text = h.Text.Replace("|", @"\|");
    sb.AppendLine($"| `{h.File}` | {h.Line} | {h.Context} | `{h.OldName}` | `{text}` |");
}

Console.WriteLine($"TOTAL STRING HITS: {hits.Count}");
Console.WriteLine();

foreach (var g in hits.GroupBy(h => h.Context).OrderByDescending(g => g.Count()))
{
    Console.WriteLine($"  {g.Count(),4}  {g.Key}");
}

File.WriteAllText(Path.Combine(root, "..", "specs", "_rename-string-hits.md"), sb.ToString());
Console.WriteLine();
Console.WriteLine("wrote specs/_rename-string-hits.md");
