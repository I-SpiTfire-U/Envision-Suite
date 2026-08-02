using System.Runtime.InteropServices;

namespace EnvisionSuite.Core.Interop.LinuxInput;

/// <summary>
///     Input device identification structure.
///     Contains bus type and USB vendor/product/version IDs.
/// </summary>
/// <remarks>
///     This maps to the kernel's <c>struct input_id</c> defined in <c>linux/input.h</c>.
///     This is a readonly struct because it is only set during device initialization.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public readonly struct InputId
{
  /// <summary>Bus type (BUS_USB, BUS_VIRTUAL, etc.).</summary>
  public readonly UInt16 BusType;

  /// <summary>USB Vendor ID (VID).</summary>
  public readonly UInt16 Vendor;

  /// <summary>USB Product ID (PID).</summary>
  public readonly UInt16 Product;

  /// <summary>Device version number.</summary>
  public readonly UInt16 Version;

  /// <summary>
  ///     Creates a new input device identification.
  /// </summary>
  public InputId(UInt16 busType, UInt16 vendor, UInt16 product, UInt16 version)
  {
    BusType = busType;
    Vendor = vendor;
    Product = product;
    Version = version;
  }
}