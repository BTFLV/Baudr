using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Baudr.Core.Abstractions;
using Baudr.Core.Models;

namespace Baudr.Infrastructure.Serial;

public partial class SystemSerialPortEnumerator : ISerialPortEnumerator
{
    [GeneratedRegex(@"\d+")]
    private static partial Regex NumberRegex();

    public IReadOnlyList<SerialPortInfo> GetAvailablePorts()
    {
        var result = new List<SerialPortInfo>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            EnumerateWindowsPorts(result);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            EnumerateLinuxPorts(result);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            EnumerateMacPorts(result);
        }
        else
        {
            // Generic fallback using standard .NET API
            foreach (var name in SerialPort.GetPortNames())
            {
                result.Add(new SerialPortInfo(name));
            }
        }

        // Sort ports naturally (e.g. COM2 before COM10)
        return result.OrderBy(p => ExtractPortNumber(p.PortName)).ThenBy(p => p.PortName).ToList();
    }

    private static void EnumerateWindowsPorts(List<SerialPortInfo> list)
    {
        try
        {
            var portNames = SerialPort.GetPortNames();
            foreach (var port in portNames.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                list.Add(new SerialPortInfo(port, IsUsb: !port.Equals("COM1", StringComparison.OrdinalIgnoreCase)));
            }
        }
        catch
        {
            // Fallback
        }
    }

    private static void EnumerateLinuxPorts(List<SerialPortInfo> list)
    {
        try
        {
            // 1. Check /dev/serial/by-id/ for rich USB device identifiers
            if (Directory.Exists("/dev/serial/by-id"))
            {
                foreach (var file in Directory.GetFiles("/dev/serial/by-id"))
                {
                    try
                    {
                        var realPath = Path.GetFullPath(file);
                        var fileName = Path.GetFileName(file);
                        list.Add(new SerialPortInfo(realPath, Description: fileName, IsUsb: true));
                    }
                    catch
                    {
                        // Ignore individual symlink errors
                    }
                }
            }

            // 2. Scan /dev for ttyUSB*, ttyACM*, and ttyS*
            if (Directory.Exists("/dev"))
            {
                var devPorts = Directory.GetFiles("/dev", "ttyUSB*")
                    .Concat(Directory.GetFiles("/dev", "ttyACM*"))
                    .Concat(Directory.GetFiles("/dev", "ttyS*"));

                foreach (var dev in devPorts)
                {
                    if (!list.Any(p => p.PortName.Equals(dev, StringComparison.OrdinalIgnoreCase)))
                    {
                        bool isUsb = dev.Contains("USB", StringComparison.OrdinalIgnoreCase) || dev.Contains("ACM", StringComparison.OrdinalIgnoreCase);
                        list.Add(new SerialPortInfo(dev, IsUsb: isUsb));
                    }
                }
            }
        }
        catch
        {
            // Fall back to GetPortNames()
            foreach (var name in SerialPort.GetPortNames())
            {
                if (!list.Any(p => p.PortName.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    list.Add(new SerialPortInfo(name));
                }
            }
        }
    }

    private static void EnumerateMacPorts(List<SerialPortInfo> list)
    {
        try
        {
            if (Directory.Exists("/dev"))
            {
                // macOS creates /dev/cu.* and /dev/tty.* pairs.
                // cu.* (calling unit) is the standard modern device to open for serial communication.
                var cuPorts = Directory.GetFiles("/dev", "cu.*");
                foreach (var dev in cuPorts)
                {
                    var baseName = Path.GetFileName(dev);
                    // Filter out internal bluetooth modems if desired, or include them
                    bool isUsb = baseName.Contains("usb", StringComparison.OrdinalIgnoreCase);
                    list.Add(new SerialPortInfo(dev, Description: baseName, IsUsb: isUsb));
                }
            }
        }
        catch
        {
            foreach (var name in SerialPort.GetPortNames())
            {
                list.Add(new SerialPortInfo(name));
            }
        }
    }

    private static int ExtractPortNumber(string portName)
    {
        var match = NumberRegex().Match(portName);
        return match.Success && int.TryParse(match.Value, out int num) ? num : int.MaxValue;
    }
}

