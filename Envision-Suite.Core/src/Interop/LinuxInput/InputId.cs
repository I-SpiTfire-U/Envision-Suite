using System.Runtime.InteropServices;

namespace EnvisionSuite.Core.Interop.LinuxInput;

/// <summary>
///   Input device identification structure. Contains bus type and USB vendor/product/version IDs.
/// </summary>
/// <remarks>
///   This maps to the kernel's <c>struct input_id</c> defined in <c>linux/input.h</c>.
///   This is a readonly struct because it is only set during device initialization.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public readonly struct InputId
{
  /// <summary>Bus type (BUS_USB, BUS_VIRTUAL, etc.).</summary>
  public readonly UInt16 BusType;

  /// <summary>USB Vendor ID (VID).</summary>
  public readonly UInt16 VendorId;

  /// <summary>USB Product ID (PID).</summary>
  public readonly UInt16 ProductId;

  /// <summary>Device version number.</summary>
  public readonly UInt16 VersionNumber;

  /// <summary>
  ///   Creates a new input device identification.
  /// </summary>
  public InputId(UInt16 busType, UInt16 vendorId, UInt16 productId, UInt16 versionNumber)
  {
    BusType = busType;
    VendorId = vendorId;
    ProductId = productId;
    VersionNumber = versionNumber;
  }
}