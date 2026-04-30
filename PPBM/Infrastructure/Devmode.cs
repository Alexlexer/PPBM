using System.Runtime.InteropServices;

namespace PPBM.Infrastructure;

/// <summary>
/// Marshalled representation of the Win32 <c>DEVMODE</c> structure used by
/// <c>EnumDisplaySettings</c> to retrieve current display mode parameters.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public struct Devmode
{
    /// <summary>Device name (e.g. "\\.\DISPLAY1").</summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string dmDeviceName;

    /// <summary>Specification version.</summary>
    public short dmSpecVersion;

    /// <summary>Driver version.</summary>
    public short dmDriverVersion;

    /// <summary>Size of the structure in bytes.</summary>
    public short dmSize;

    /// <summary>Number of extra driver bytes following the structure.</summary>
    public short dmDriverExtra;

    /// <summary>Bitmask indicating which remaining fields are valid.</summary>
    public int dmFields;

    /// <summary>X position in device coordinates.</summary>
    public int dmPositionX;

    /// <summary>Y position in device coordinates.</summary>
    public int dmPositionY;

    /// <summary>Display orientation.</summary>
    public int dmDisplayOrientation;

    /// <summary>Fixed-panel output mode.</summary>
    public int dmDisplayFixedOutput;

    /// <summary>Color depth/resolution.</summary>
    public short dmColor;

    /// <summary>Duplex mode for printers.</summary>
    public short dmDuplex;

    /// <summary>Y resolution in DPI.</summary>
    public short dmYResolution;

    /// <summary>TrueType font option.</summary>
    public short dmTTOption;

    /// <summary>Collation for printers.</summary>
    public short dmCollate;

    /// <summary>Form name for printers.</summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string dmFormName;

    /// <summary>Logical pixels per inch.</summary>
    public short dmLogPixels;

    /// <summary>Bits per pixel.</summary>
    public int dmBitsPerPel;

    /// <summary>Horizontal resolution in pixels.</summary>
    public int dmPelsWidth;

    /// <summary>Vertical resolution in pixels.</summary>
    public int dmPelsHeight;

    /// <summary>Display flags.</summary>
    public int dmDisplayFlags;

    /// <summary>Vertical refresh rate in Hz.</summary>
    public int dmDisplayFrequency;
}
