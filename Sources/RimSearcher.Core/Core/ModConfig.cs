namespace RimSearcher.Core;

public record ModConfig
{
    public string Name { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public bool Enabled { get; init; } = true;
    public List<string> CsharpPaths { get; init; } = new();
    public List<string> XmlPaths { get; init; } = new();
}
