// Verifies the resource and XAML lookups the compiler cannot see.
//
// Three failure modes, all of which build clean and throw at runtime:
//
//   1. ResourceManager("Name")            -> Name.resources must be embedded.
//   2. ComponentResourceManager(typeof(T)),
//      new Icon(typeof(T), "file")        -> resolve against T's FULL NAME, so moving a type
//                                            between namespaces silently breaks them.
//   3. {x:Static prefix:Type.Member}      -> BAML needs a PUBLIC type with a PUBLIC STATIC member.
//                                            SDK-style WPF no longer emits GeneratedInternalTypeHelper,
//                                            so an internal type is unreachable.
//
// Run after a Release build; it reads the built assemblies, not the build log.
//   dotnet run tools/resource-check.cs
// Exit code is the number of problems found, so it can gate a build.

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;

var root = @"D:\GitHub\EasyAF.EntityDesigner\src";

static bool IsBuildOutput(string p) =>
    p.Contains(@"\obj\", StringComparison.OrdinalIgnoreCase) ||
    p.Contains(@"\bin\", StringComparison.OrdinalIgnoreCase) ||
    p.Contains(@"\.vs\", StringComparison.OrdinalIgnoreCase);

// ---- built metadata -------------------------------------------------------
// Read each project's OWN output. Copies of an assembly sit in every consuming project's bin,
// and a stale copy there would happily vouch for a name that is no longer embedded.

var embedded = new HashSet<string>(StringComparer.Ordinal);
var publicTypes = new HashSet<string>(StringComparer.Ordinal);
var publicStatics = new HashSet<string>(StringComparer.Ordinal);
var outputs = 0;

foreach (var projDir in Directory.EnumerateDirectories(root))
{
    var own = Path.GetFileName(projDir) + ".dll";

    foreach (var dll in Directory.EnumerateFiles(projDir, own, SearchOption.AllDirectories))
    {
        if (!dll.Contains(@"\bin\Release", StringComparison.OrdinalIgnoreCase)) continue;

        try
        {
            using var fs = File.OpenRead(dll);
            using var pe = new PEReader(fs);
            if (!pe.HasMetadata) continue;

            var md = pe.GetMetadataReader();
            outputs++;

            foreach (var h in md.ManifestResources)
            {
                embedded.Add(md.GetString(md.GetManifestResource(h).Name));
            }

            foreach (var th in md.TypeDefinitions)
            {
                var td = md.GetTypeDefinition(th);
                if ((td.Attributes & TypeAttributes.VisibilityMask) != TypeAttributes.Public) continue;

                var ns = md.GetString(td.Namespace);
                var name = md.GetString(td.Name);
                var full = string.IsNullOrEmpty(ns) ? name : ns + "." + name;
                publicTypes.Add(full);

                foreach (var ph in td.GetProperties())
                {
                    var p = md.GetPropertyDefinition(ph);
                    var getter = p.GetAccessors().Getter;
                    if (getter.IsNil) continue;

                    var m = md.GetMethodDefinition(getter);
                    if ((m.Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public) continue;
                    if ((m.Attributes & MethodAttributes.Static) == 0) continue;

                    publicStatics.Add(full + "." + md.GetString(p.Name));
                }

                foreach (var fh in td.GetFields())
                {
                    var f = md.GetFieldDefinition(fh);
                    if ((f.Attributes & FieldAttributes.FieldAccessMask) != FieldAttributes.Public) continue;
                    if ((f.Attributes & FieldAttributes.Static) == 0) continue;

                    publicStatics.Add(full + "." + md.GetString(f.Name));
                }
            }
        }
        catch
        {
            // A native or unreadable dll in an output folder is not our concern.
        }
    }
}

// ---- source facts ---------------------------------------------------------

var sources = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
    .Where(f => !IsBuildOutput(f))
    .ToList();

// type name -> declaring namespace, for typeof(...) resolution
var nsOf = new Dictionary<string, string>(StringComparer.Ordinal);

foreach (var cs in sources)
{
    var text = File.ReadAllText(cs);
    var nm = Regex.Match(text, @"^\s*namespace\s+([A-Za-z0-9_.]+)", RegexOptions.Multiline);
    if (!nm.Success) continue;

    foreach (Match t in Regex.Matches(text, @"\b(?:partial\s+)?class\s+([A-Za-z0-9_]+)"))
    {
        nsOf.TryAdd(t.Groups[1].Value, nm.Groups[1].Value);
    }
}

var problems = new List<string>();
int okLiterals = 0, okTyped = 0, okStatic = 0, externalStatic = 0;

// ---- 1. ResourceManager("literal") ---------------------------------------
// ResXFileCodeGenerator splits long names as "abc" + "def", so the pieces must be joined
// before comparing. A per-line grep silently under-reads them.

var callRx = new Regex(@"ResourceManager\s*\(\s*((?:@?""(?:[^""\\]|\\.)*""\s*\+?\s*)+)");
var pieceRx = new Regex(@"""((?:[^""\\]|\\.)*)""");

foreach (var cs in sources)
{
    var text = File.ReadAllText(cs);
    if (!text.Contains("ResourceManager(", StringComparison.Ordinal)) continue;

    foreach (Match m in callRx.Matches(text))
    {
        var joined = string.Concat(pieceRx.Matches(m.Groups[1].Value).Select(p => p.Groups[1].Value));
        if (joined.Length == 0) continue;

        if (embedded.Contains(joined + ".resources"))
        {
            okLiterals++;
            continue;
        }

        problems.Add($"missing resource  {joined}.resources{Environment.NewLine}      ResourceManager literal in {Path.GetRelativePath(root, cs)}");
    }
}

// ---- 2. type-derived resource lookups ------------------------------------

foreach (var cs in sources)
{
    var text = File.ReadAllText(cs);

    foreach (Match m in Regex.Matches(text, @"ComponentResourceManager\(\s*typeof\(([A-Za-z0-9_]+)\)"))
    {
        var type = m.Groups[1].Value;
        if (!nsOf.TryGetValue(type, out var ns)) continue;

        var want = $"{ns}.{type}.resources";
        if (embedded.Contains(want))
        {
            okTyped++;
            continue;
        }

        problems.Add($"missing resource  {want}{Environment.NewLine}      ComponentResourceManager(typeof({type})) in {Path.GetRelativePath(root, cs)}");
    }

    foreach (Match m in Regex.Matches(text, @"new Icon\(\s*typeof\(([A-Za-z0-9_]+)\)\s*,\s*""([^""]+)"""))
    {
        var type = m.Groups[1].Value;
        var file = m.Groups[2].Value;
        if (!nsOf.TryGetValue(type, out var ns)) continue;

        var want = $"{ns}.{file}";
        if (embedded.Contains(want))
        {
            okTyped++;
            continue;
        }

        problems.Add($"missing resource  {want}{Environment.NewLine}      new Icon(typeof({type}), \"{file}\") in {Path.GetRelativePath(root, cs)}");
    }
}

// ---- 3. x:Static visibility ----------------------------------------------

foreach (var xaml in Directory.EnumerateFiles(root, "*.xaml", SearchOption.AllDirectories))
{
    if (IsBuildOutput(xaml)) continue;

    var text = File.ReadAllText(xaml);
    var prefixes = new Dictionary<string, string>(StringComparer.Ordinal);

    foreach (Match m in Regex.Matches(text, @"xmlns:([A-Za-z0-9_]+)\s*=\s*""clr-namespace:([^;""]+)"))
    {
        prefixes[m.Groups[1].Value] = m.Groups[2].Value;
    }

    foreach (Match m in Regex.Matches(text, @"x:Static\s+([A-Za-z0-9_]+):([A-Za-z0-9_]+)\.([A-Za-z0-9_]+)"))
    {
        var prefix = m.Groups[1].Value;
        var type = m.Groups[2].Value;
        var member = m.Groups[3].Value;

        if (!prefixes.TryGetValue(prefix, out var ns))
        {
            externalStatic++;
            continue;
        }

        var full = $"{ns}.{type}";

        if (!publicTypes.Contains(full))
        {
            // Only our own assemblies are indexed, so anything we do not declare is out of scope.
            if (!nsOf.ContainsKey(type))
            {
                externalStatic++;
                continue;
            }

            problems.Add($"x:Static on non-public type  {full}.{member}{Environment.NewLine}      {Path.GetRelativePath(root, xaml)}");
            continue;
        }

        if (!publicStatics.Contains($"{full}.{member}"))
        {
            problems.Add($"x:Static member is not public static  {full}.{member}{Environment.NewLine}      {Path.GetRelativePath(root, xaml)}");
            continue;
        }

        okStatic++;
    }
}

// ---- report ---------------------------------------------------------------

Console.WriteLine($"project outputs read : {outputs}");
Console.WriteLine($"manifest resources   : {embedded.Count}");
Console.WriteLine();
Console.WriteLine($"ResourceManager literals ok : {okLiterals}");
Console.WriteLine($"type-derived lookups ok     : {okTyped}");
Console.WriteLine($"x:Static ok                 : {okStatic}  ({externalStatic} external, not checked)");
Console.WriteLine();

if (problems.Count == 0)
{
    Console.WriteLine("no problems found");
    return 0;
}

Console.WriteLine($"PROBLEMS: {problems.Count}");
Console.WriteLine();

foreach (var p in problems.Distinct())
{
    Console.WriteLine("  " + p);
    Console.WriteLine();
}

return problems.Count;
