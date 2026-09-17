namespace Baudr.Core.Models;

public class PackageVerificationReport
{
    public string Status { get; set; } = "passed";
    public string Application { get; set; } = "Baudr";
    public string Version { get; set; } = string.Empty;
    public string InformationalVersion { get; set; } = string.Empty;
    public string Os { get; set; } = string.Empty;
    public string Architecture { get; set; } = string.Empty;
    public string Framework { get; set; } = string.Empty;
    public bool IsAot { get; set; }
    public int DetectedPortsCount { get; set; }
    public bool SettingsSerializationSuccess { get; set; }
    public string Timestamp { get; set; } = string.Empty;
}

public class PackageVerificationErrorReport
{
    public string Status { get; set; } = "failed";
    public string Error { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
}

