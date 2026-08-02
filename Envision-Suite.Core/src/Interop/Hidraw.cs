using System.Runtime.InteropServices;

namespace EnvisionSuite.Core.Interop;

/// <summary>
///   Device information structure returned by the HIDIOCGRAWINFO ioctl.
///   Contains the bus type and USB vendor/product IDs for the HID device.
/// </summary>
/// <remarks>
///   This maps to the kernel's <c>struct hidraw_devinfo</c> defined in
///   <c>linux/hidraw.h</c>.
///   This is a readonly struct because it is only read after the kernel fills it.
///   In production, the kernel fills this struct via ioctl; the constructor is for testing.
/// </remarks>
/// <remarks>
///   Creates a new hidraw device info structure (primarily for testing).
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public readonly struct HidrawDevInfo(UInt32 busType, Int16 vendor, Int16 product)
{
  /// <summary>
  ///   Bus type (e.g., BUS_USB = 0x03, BUS_BLUETOOTH = 0x05).
  /// </summary>
  public readonly UInt32 BusType = busType;

  /// <summary>
  ///   USB Vendor ID (VID) of the device.
  ///   Note: Kernel uses signed short, but USB IDs are semantically unsigned.
  /// </summary>
  public readonly Int16 Vendor = vendor;

  /// <summary>
  ///   USB Product ID (PID) of the device.
  ///   Note: Kernel uses signed short, but USB IDs are semantically unsigned.
  /// </summary>
  public readonly Int16 Product = product;
}