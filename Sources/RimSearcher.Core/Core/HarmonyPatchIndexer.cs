using System.Collections.Concurrent;
using System.Collections.Frozen;

namespace RimSearcher.Core;

public enum HarmonyPatchType
{
    Prefix,
    Postfix,
    Transpiler,
    Finalizer,
    Unknown
}

public record HarmonyPatchLocation
{
    public string FilePath { get; init; } = string.Empty;
    public string ModName { get; init; } = string.Empty;
    public string PatchMethodName { get; init; } = string.Empty;
    public string PatchClassName { get; init; } = string.Empty;
    public HarmonyPatchType PatchType { get; init; } = HarmonyPatchType.Unknown;
    public string? TargetMethodName { get; init; }
    public string? TargetTypeName { get; init; }
    public string? TargetMethodSignature { get; init; }
    public int LineNumber { get; init; }
}

public class HarmonyPatchIndexer
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<HarmonyPatchLocation>> _methodPatchIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ConcurrentBag<HarmonyPatchLocation>> _typePatchIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, byte> _processedFiles = new(StringComparer.OrdinalIgnoreCase);
    
    private FrozenDictionary<string, HarmonyPatchLocation[]>? _frozenMethodPatchIndex;
    private FrozenDictionary<string, HarmonyPatchLocation[]>? _frozenTypePatchIndex;

    public void FreezeIndex()
    {
        _frozenMethodPatchIndex = _methodPatchIndex.ToFrozenDictionary(
            kv => kv.Key, 
            kv => kv.Value.Distinct().ToArray(), 
            StringComparer.OrdinalIgnoreCase);
        _frozenTypePatchIndex = _typePatchIndex.ToFrozenDictionary(
            kv => kv.Key, 
            kv => kv.Value.Distinct().ToArray(), 
            StringComparer.OrdinalIgnoreCase);
    }

    public HarmonyPatchIndexerSnapshot ExportSnapshot()
    {
        var methodPatchIndex = _frozenMethodPatchIndex != null
            ? _frozenMethodPatchIndex.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase)
            : _methodPatchIndex.ToDictionary(kv => kv.Key, kv => kv.Value.Distinct().ToArray(), StringComparer.OrdinalIgnoreCase);

        var typePatchIndex = _frozenTypePatchIndex != null
            ? _frozenTypePatchIndex.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase)
            : _typePatchIndex.ToDictionary(kv => kv.Key, kv => kv.Value.Distinct().ToArray(), StringComparer.OrdinalIgnoreCase);

        return new HarmonyPatchIndexerSnapshot
        {
            MethodPatchIndex = methodPatchIndex,
            TypePatchIndex = typePatchIndex,
            ProcessedFiles = _processedFiles.Keys.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
        };
    }

    public void ImportSnapshot(HarmonyPatchIndexerSnapshot snapshot)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

        _methodPatchIndex.Clear();
        _typePatchIndex.Clear();
        _processedFiles.Clear();
        _frozenMethodPatchIndex = null;
        _frozenTypePatchIndex = null;

        foreach (var (key, values) in snapshot.MethodPatchIndex)
        {
            _methodPatchIndex[key] = new ConcurrentBag<HarmonyPatchLocation>(values);
        }

        foreach (var (key, values) in snapshot.TypePatchIndex)
        {
            _typePatchIndex[key] = new ConcurrentBag<HarmonyPatchLocation>(values);
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
                foreach (var file in Directory.GetFiles(currentPath, "*.cs")) allFiles.Add(file);
                foreach (var dir in Directory.GetDirectories(currentPath))
                {
                    if (!dir.EndsWith("bin", StringComparison.OrdinalIgnoreCase) && 
                        !dir.EndsWith("obj", StringComparison.OrdinalIgnoreCase))
                    {
                        stack.Push(dir);
                    }
                }
            }
            catch { }
        }

        var newFiles = allFiles.Where(f => _processedFiles.TryAdd(Path.GetFullPath(f), 0)).ToList();

        Parallel.ForEach(newFiles, file =>
        {
            var internedFile = string.Intern(file);
            
            try
            {
                var patches = RoslynHelper.ExtractHarmonyPatches(internedFile, modName);
                foreach (var patch in patches)
                {
                    if (!string.IsNullOrEmpty(patch.TargetMethodName))
                    {
                        _methodPatchIndex.GetOrAdd(patch.TargetMethodName, _ => new ConcurrentBag<HarmonyPatchLocation>()).Add(patch);
                    }
                    if (!string.IsNullOrEmpty(patch.TargetTypeName))
                    {
                        _typePatchIndex.GetOrAdd(patch.TargetTypeName, _ => new ConcurrentBag<HarmonyPatchLocation>()).Add(patch);
                    }
                }
            }
            catch { }
        });
    }

    public List<HarmonyPatchLocation> GetPatchesForMethod(string methodName)
    {
        var results = new List<HarmonyPatchLocation>();
        
        if (_frozenMethodPatchIndex != null && _frozenMethodPatchIndex.TryGetValue(methodName, out var frozen))
            results.AddRange(frozen);
        else if (_methodPatchIndex.TryGetValue(methodName, out var bag))
            results.AddRange(bag.Distinct());
        
        return results;
    }

    public List<HarmonyPatchLocation> GetPatchesForType(string typeName)
    {
        var results = new List<HarmonyPatchLocation>();
        
        if (_frozenTypePatchIndex != null && _frozenTypePatchIndex.TryGetValue(typeName, out var frozen))
            results.AddRange(frozen);
        else if (_typePatchIndex.TryGetValue(typeName, out var bag))
            results.AddRange(bag.Distinct());
        
        return results;
    }

    public int GetPatchCountForMethod(string methodName)
    {
        if (_frozenMethodPatchIndex != null && _frozenMethodPatchIndex.TryGetValue(methodName, out var frozen))
            return frozen.Length;
        
        if (_methodPatchIndex.TryGetValue(methodName, out var bag))
            return bag.Distinct().Count();
        
        return 0;
    }

    public int GetPatchCountForType(string typeName)
    {
        if (_frozenTypePatchIndex != null && _frozenTypePatchIndex.TryGetValue(typeName, out var frozen))
            return frozen.Length;
        
        if (_typePatchIndex.TryGetValue(typeName, out var bag))
            return bag.Distinct().Count();
        
        return 0;
    }
}

public sealed class HarmonyPatchIndexerSnapshot
{
    public Dictionary<string, HarmonyPatchLocation[]> MethodPatchIndex { get; init; } = new();
    public Dictionary<string, HarmonyPatchLocation[]> TypePatchIndex { get; init; } = new();
    public string[] ProcessedFiles { get; init; } = Array.Empty<string>();
}
