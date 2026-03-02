using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Xml;
using System.Xml.Linq;

namespace RimSearcher.Core;

public enum PatchOperationType
{
    Add,
    Remove,
    Replace,
    Insert,
    Unknown
}

public record PatchLocation
{
    public string FilePath { get; init; } = string.Empty;
    public string ModName { get; init; } = string.Empty;
    public PatchOperationType OperationType { get; init; } = PatchOperationType.Unknown;
    public string? TargetDefName { get; init; }
    public string? TargetDefType { get; init; }
    public string? XPath { get; init; }
    public int LineNumber { get; init; }
}

public class PatchIndexer
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<PatchLocation>> _defPatchIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, byte> _processedFiles = new(StringComparer.OrdinalIgnoreCase);
    
    private FrozenDictionary<string, PatchLocation[]>? _frozenDefPatchIndex;

    public void FreezeIndex()
    {
        _frozenDefPatchIndex = _defPatchIndex.ToFrozenDictionary(
            kv => kv.Key, 
            kv => kv.Value.Distinct().ToArray(), 
            StringComparer.OrdinalIgnoreCase);
    }

    public PatchIndexerSnapshot ExportSnapshot()
    {
        var defPatchIndex = _frozenDefPatchIndex != null
            ? _frozenDefPatchIndex.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase)
            : _defPatchIndex.ToDictionary(kv => kv.Key, kv => kv.Value.Distinct().ToArray(), StringComparer.OrdinalIgnoreCase);

        return new PatchIndexerSnapshot
        {
            DefPatchIndex = defPatchIndex,
            ProcessedFiles = _processedFiles.Keys.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
        };
    }

    public void ImportSnapshot(PatchIndexerSnapshot snapshot)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

        _defPatchIndex.Clear();
        _processedFiles.Clear();
        _frozenDefPatchIndex = null;

        foreach (var (key, values) in snapshot.DefPatchIndex)
        {
            _defPatchIndex[key] = new ConcurrentBag<PatchLocation>(values);
        }

        foreach (var file in snapshot.ProcessedFiles.Where(file => !string.IsNullOrWhiteSpace(file)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            _processedFiles[file] = 0;
        }
    }

    public void Scan(string rootPath, string modName)
    {
        if (!Directory.Exists(rootPath)) return;

        var allFiles = new List<string>();
        var stack = new Stack<string>();
        stack.Push(rootPath);

        while (stack.Count > 0)
        {
            var currentPath = stack.Pop();
            try
            {
                foreach (var file in Directory.GetFiles(currentPath, "*.xml")) allFiles.Add(file);
                foreach (var dir in Directory.GetDirectories(currentPath)) stack.Push(dir);
            }
            catch { }
        }

        var newFiles = allFiles.Where(f => _processedFiles.TryAdd(Path.GetFullPath(f), 0)).ToList();

        Parallel.ForEach(newFiles, file =>
        {
            var internedFile = string.Intern(file);
            
            try
            {
                var patches = ParsePatchFile(internedFile, modName);
                foreach (var patch in patches)
                {
                    if (!string.IsNullOrEmpty(patch.TargetDefName))
                    {
                        _defPatchIndex.GetOrAdd(patch.TargetDefName, _ => new ConcurrentBag<PatchLocation>()).Add(patch);
                    }
                }
            }
            catch { }
        });
    }

    private List<PatchLocation> ParsePatchFile(string filePath, string modName)
    {
        var results = new List<PatchLocation>();
        
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = XmlReader.Create(stream);
        var doc = XDocument.Load(reader);

        if (doc.Root == null) return results;

        ParsePatchElements(doc.Root, filePath, modName, results);

        return results;
    }

    private void ParsePatchElements(XElement element, string filePath, string modName, List<PatchLocation> results, int depth = 0)
    {
        if (depth > 20) return;

        var elementName = element.Name.LocalName;

        if (elementName.StartsWith("Operation", StringComparison.OrdinalIgnoreCase))
        {
            var patch = ParsePatchOperation(element, filePath, modName);
            if (patch != null) results.Add(patch);
        }

        foreach (var child in element.Elements())
        {
            ParsePatchElements(child, filePath, modName, results, depth + 1);
        }
    }

    private PatchLocation? ParsePatchOperation(XElement element, string filePath, string modName)
    {
        var opType = DetermineOperationType(element.Name.LocalName);
        
        var xpath = element.Element("xpath")?.Value;
        var valueElement = element.Element("value");
        
        string? targetDefName = null;
        string? targetDefType = null;

        if (!string.IsNullOrEmpty(xpath))
        {
            targetDefName = ExtractDefNameFromXPath(xpath);
            targetDefType = ExtractDefTypeFromXPath(xpath);
        }

        if (targetDefName == null && valueElement != null)
        {
            var defNameElement = valueElement.Element("defName");
            if (defNameElement != null)
            {
                targetDefName = defNameElement.Value;
            }

            if (valueElement.HasElements)
            {
                targetDefType = valueElement.Elements().FirstOrDefault()?.Name.LocalName;
            }
        }

        return new PatchLocation
        {
            FilePath = filePath,
            ModName = modName,
            OperationType = opType,
            TargetDefName = targetDefName,
            TargetDefType = targetDefType,
            XPath = xpath,
            LineNumber = ((IXmlLineInfo)element).LineNumber
        };
    }

    private PatchOperationType DetermineOperationType(string operationName)
    {
        return operationName.ToLowerInvariant() switch
        {
            "operation" => PatchOperationType.Unknown,
            var name when name.Contains("add", StringComparison.OrdinalIgnoreCase) => PatchOperationType.Add,
            var name when name.Contains("remove", StringComparison.OrdinalIgnoreCase) => PatchOperationType.Remove,
            var name when name.Contains("replace", StringComparison.OrdinalIgnoreCase) => PatchOperationType.Replace,
            var name when name.Contains("insert", StringComparison.OrdinalIgnoreCase) => PatchOperationType.Insert,
            _ => PatchOperationType.Unknown
        };
    }

    private string? ExtractDefNameFromXPath(string xpath)
    {
        if (string.IsNullOrEmpty(xpath)) return null;

        var defNameMatch = System.Text.RegularExpressions.Regex.Match(xpath, @"defName\s*=\s*['""]([^'""]+)['""]", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        if (defNameMatch.Success)
        {
            return defNameMatch.Groups[1].Value;
        }

        var parts = xpath.Split('/');
        foreach (var part in parts)
        {
            if (part.StartsWith("defName", StringComparison.OrdinalIgnoreCase))
            {
                var eqIndex = part.IndexOf('=');
                if (eqIndex >= 0)
                {
                    var value = part.Substring(eqIndex + 1).Trim(' ', '\'', '"', '[', ']');
                    return value;
                }
            }
        }

        return null;
    }

    private string? ExtractDefTypeFromXPath(string xpath)
    {
        if (string.IsNullOrEmpty(xpath)) return null;

        var defTypeMatch = System.Text.RegularExpressions.Regex.Match(xpath, @"/([^/\[\]]+)(?:\[|$)", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        if (defTypeMatch.Success)
        {
            var defType = defTypeMatch.Groups[1].Value;
            if (!string.IsNullOrEmpty(defType) && !defType.Equals("Defs", StringComparison.OrdinalIgnoreCase))
            {
                return defType;
            }
        }

        return null;
    }

    public List<PatchLocation> GetPatchesForDef(string defName)
    {
        if (_frozenDefPatchIndex != null && _frozenDefPatchIndex.TryGetValue(defName, out var frozen))
            return frozen.ToList();
        
        if (_defPatchIndex.TryGetValue(defName, out var bag))
            return bag.Distinct().ToList();
        
        return new List<PatchLocation>();
    }

    public int GetPatchCountForDef(string defName)
    {
        if (_frozenDefPatchIndex != null && _frozenDefPatchIndex.TryGetValue(defName, out var frozen))
            return frozen.Length;
        
        if (_defPatchIndex.TryGetValue(defName, out var bag))
            return bag.Distinct().Count();
        
        return 0;
    }
}

public sealed class PatchIndexerSnapshot
{
    public Dictionary<string, PatchLocation[]> DefPatchIndex { get; init; } = new();
    public string[] ProcessedFiles { get; init; } = Array.Empty<string>();
}
