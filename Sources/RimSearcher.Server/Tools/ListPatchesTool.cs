using System.Text.Json;
using RimSearcher.Core;

namespace RimSearcher.Server.Tools;

public class ListPatchesTool : ITool
{
    private readonly PatchIndexer _patchIndexer;
    private readonly HarmonyPatchIndexer _harmonyPatchIndexer;

    public string Name => "list_patches";
    public string Description => "List XML patches and Harmony patches targeting a specific Def or method. Use this to find patches that modify game behavior.";

    public ListPatchesTool(PatchIndexer patchIndexer, HarmonyPatchIndexer harmonyPatchIndexer)
    {
        _patchIndexer = patchIndexer;
        _harmonyPatchIndexer = harmonyPatchIndexer;
    }

    public object JsonSchema => new
    {
        type = "object",
        properties = new
        {
            target = new
            {
                type = "string",
                description = "The target Def name or method name to search patches for"
            },
            patchType = new
            {
                type = "string",
                @enum = new[] { "xml", "harmony", "all" },
                description = "Type of patches to search: 'xml' for XML patches, 'harmony' for Harmony patches, 'all' for both. Default: 'all'",
                @default = "all"
            }
        },
        required = new[] { "target" }
    };

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken, IProgress<double>? progress = null)
    {
        var target = arguments.GetProperty("target").GetString() ?? string.Empty;
        var patchType = arguments.TryGetProperty("patchType", out var typeProp) 
            ? typeProp.GetString() ?? "all" 
            : "all";

        if (string.IsNullOrWhiteSpace(target))
        {
            return new ToolResult("Target is required", true);
        }

        var results = new List<string>();

        if (patchType == "xml" || patchType == "all")
        {
            var xmlPatches = _patchIndexer.GetPatchesForDef(target);
            if (xmlPatches.Count > 0)
            {
                results.Add($"## XML Patches ({xmlPatches.Count})");
                foreach (var patch in xmlPatches.Take(20))
                {
                    results.Add($"- **{patch.OperationType}** on `{patch.TargetDefName ?? "unknown"}`");
                    results.Add($"  - Mod: {patch.ModName}");
                    results.Add($"  - File: {patch.FilePath}:{patch.LineNumber}");
                    if (!string.IsNullOrEmpty(patch.XPath))
                    {
                        results.Add($"  - XPath: `{patch.XPath}`");
                    }
                }
                if (xmlPatches.Count > 20)
                {
                    results.Add($"  ... and {xmlPatches.Count - 20} more patches");
                }
            }
        }

        if (patchType == "harmony" || patchType == "all")
        {
            var harmonyPatches = _harmonyPatchIndexer.GetPatchesForMethod(target);
            var typePatches = _harmonyPatchIndexer.GetPatchesForType(target);
            var allHarmonyPatches = harmonyPatches.Concat(typePatches).Distinct().ToList();

            if (allHarmonyPatches.Count > 0)
            {
                results.Add($"## Harmony Patches ({allHarmonyPatches.Count})");
                foreach (var patch in allHarmonyPatches.Take(20))
                {
                    results.Add($"- **{patch.PatchType}** `{patch.PatchMethodName}` in `{patch.PatchClassName}`");
                    results.Add($"  - Mod: {patch.ModName}");
                    results.Add($"  - Target: `{patch.TargetTypeName}.{patch.TargetMethodName ?? "*"}`");
                    results.Add($"  - File: {patch.FilePath}:{patch.LineNumber}");
                }
                if (allHarmonyPatches.Count > 20)
                {
                    results.Add($"  ... and {allHarmonyPatches.Count - 20} more patches");
                }
            }
        }

        if (results.Count == 0)
        {
            results.Add($"No patches found targeting `{target}`");
        }

        return new ToolResult(string.Join("\n", results));
    }
}
