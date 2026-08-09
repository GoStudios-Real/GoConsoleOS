using System.Text.Json.Serialization;

namespace GoConsoleOS.Shared.Models;

public class StoreCatalog
{
    [JsonPropertyName("catalog_version")]
    public string CatalogVersion { get; set; } = "1.4.0";
    public string Updated { get; set; } = "";
    public List<CatalogItem> Items { get; set; } = new();
}

public class CatalogItem
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Author { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> Screenshots { get; set; } = new();
    public string Thumbnail { get; set; } = "";
    public string Icon { get; set; } = "";
    public int SizeKb { get; set; }
    public string DownloadUrl { get; set; } = "";
    public string InstallPath { get; set; } = "";
    public string LaunchExe { get; set; } = "";
    public string BundledExe { get; set; } = "";
    public string WebsiteUrl { get; set; } = "";
    public CatalogCompatibility Compatibility { get; set; } = new();
    public double Rating { get; set; }
    public int Downloads { get; set; }
    public List<string> Tags { get; set; } = new();
    [JsonIgnore]
    public string? LocalPath { get; set; }
    [JsonIgnore]
    public bool IsInstalled { get; set; }
    [JsonIgnore]
    public string StatusText { get; set; } = "INSTALL";
}

public class CatalogCompatibility
{
    public string MinVersion { get; set; } = "";
    public List<string> Platforms { get; set; } = new();
    public int? MinRamMb { get; set; }
    public string? MinGpu { get; set; }
}
