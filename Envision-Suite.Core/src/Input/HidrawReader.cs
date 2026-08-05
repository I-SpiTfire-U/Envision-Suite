using EnvisionSuite.Core.Interop;

namespace EnvisionSuite.Core.Input;

/// <summary>
///   Reads raw HID reports from a Linux hidraw device.
///   Hidraw provides direct access to HID (Human Interface Device) reports,
///   which can contain data not exposed through the standard evdev interface.
///   Note: On Scuf Envision Pro V2 hardware, both triggers are available via evdev,
///   so hidraw reading is optional and currently disabled to avoid latency issues.
///   This class is kept for potential V1 hardware support or future use.
/// </summary>
public sealed class HidrawReader : IDisposable
{
  private Boolean _Disposed;

  private HidrawReader(Int32 fileDescriptor)
  {
    FileDescriptor = fileDescriptor;
  }

  public Int32 FileDescriptor { get; }

  public void Dispose()
  {
    if (_Disposed)
    {
      return;
    }

    _Disposed = true;
    Libc.Close(FileDescriptor);
  }

  /// <summary>
  ///   Opens a hidraw device for reading.
  /// </summary>
  /// <param name="devicePath">
  ///   Path to the hidraw device (e.g., "/dev/hidraw0").
  ///   If empty or null, returns null immediately.
  /// </param>
  /// <returns>
  ///   A <see cref="HidrawReader" /> instance, or null if the device could not be opened
  ///   or the path was empty.
  /// </returns>
  public static HidrawReader? Open(String? devicePath)
  {
    if (String.IsNullOrEmpty(devicePath))
    {
      return null;
    }

    Int32 fileDescriptor = Libc.Open(devicePath, Libc.O_RDONLY | Libc.O_NONBLOCK);
    if (fileDescriptor < 0)
    {
      return null;
    }

    try
    {
      HidrawReader reader = new(fileDescriptor);

      if (!reader.VerifyDevice())
      {
        Console.Error.WriteLine("[warning] Could not verify hidraw device identity");
      }

      return reader;
    }
    catch
    {
      Libc.Close(fileDescriptor);
      throw;
    }
  }

  /// <summary>
  ///   Verifies that the opened hidraw device is the expected Scuf controller
  ///   by checking its vendor and product IDs using the HIDIOCGRAWINFO ioctl.
  /// </summary>
  /// <returns>True if the device matches the expected Scuf controller IDs.</returns>
  private unsafe Boolean VerifyDevice()
  {
    HidrawDevInfo devInfo;
    Int32 result = Libc.Ioctl(FileDescriptor, HidrawIoctl.HIDIOCGRAWINFO, &devInfo);

    if (result < 0)
    {
      return false;
    }

    const UInt16 ScufVendorId = 0x1b1c;
    const UInt16 ScufProductId = 0x3a08;

    return (UInt16)devInfo.Vendor == ScufVendorId && (UInt16)devInfo.Product == ScufProductId;
  }

  /// <summary>
  ///   Reads a raw HID report from the device.
  ///   This method is non-blocking - if no report is available, it returns 0 immediately.
  /// </summary>
  /// <param name="buffer">
  ///   Buffer to receive the HID report. Should be at least 64 bytes for most controllers.
  /// </param>
  /// <returns>
  ///   The number of bytes read, 0 if no report was available, or -1 on error.
  /// </returns>
  public unsafe Int32 ReadReport(Span<Byte> buffer)
  {
    if (_Disposed)
    {
      return -1;
    }

    fixed (Byte* ptr = buffer)
    {
      IntPtr bytesRead = Libc.Read(FileDescriptor, ptr, (UIntPtr)buffer.Length);

      if (bytesRead < 0)
      {
        Int32 nativeErrorNumber = Libc.GetLastError();
        return nativeErrorNumber == Libc.EAGAIN ? 0 : -1;
      }
      return (Int32)bytesRead;
    }
  }
}