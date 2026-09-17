namespace Baudr.Core.Models;

public record SerialPortInfo(
    string PortName,
    string? Description = null,
    string? Manufacturer = null,
    string? HardwareId = null,
    bool IsUsb = false)
{
    public string DisplayName => string.IsNullOrWhiteSpace(Description) 
        ? PortName 
        : $"{PortName} - {Description}";
}

public readonly record struct ModemSignals(
    bool CtsHolding,
    bool DsrHolding,
    bool CdHolding,
    bool RiHolding)
{
    public static readonly ModemSignals None = new(false, false, false, false);
}

