using EnvisionSuite.Core.Interop;

namespace EnvisionSuite.Core.Input;

/// <summary>
///   Reads raw HID reports from a Linux hidraw device.
/// </summary>
/// <remarks>
///   Hidraw provides direct access to human interface device reports,
///   which may contain data not exposed through the standard evdev interface.
///   On SCUF Envision Pro V2 hardware, hidraw reading is optional as both
///   triggers are available via evdev. Envision Suite currently leaves hidraw
///   reading disabled to avoid latency issues.
/// </remarks>
public sealed class HidrawReader : IDisposable
{
  /// <summary>The native file descriptor used for ioctl and libc operations.</summary>
  public Int32 FileDescriptor { get; }

  private Boolean _Disposed;

  private HidrawReader(Int32 fileDescriptor) =>
    FileDescriptor = fileDescriptor;

  /// <summary>
  ///   Opens a hidraw device for reading.
  /// </summary>
  /// <param name="devicePath">The path to a given hidraw device.</param>
  /// <returns>
  ///   A <see cref="HidrawReader"/> instance if the path was not null or empty and
  ///   the device was successfully opened; otherwise <see langword="null"/>.
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
      Int32 nativeErrorNumber = Libc.GetLastError();

      Console.Error.WriteLine(
        $"Failed to open {devicePath}: " +
        $"{Libc.StrError(nativeErrorNumber)} " +
        $"(nativeErrorNumber={nativeErrorNumber})"
      );

      return null;
    }

    HidrawReader reader = new(fileDescriptor);

    if (!reader.VerifyDevice())
    {
      Console.Error.WriteLine("[warning] Could not verify hidraw device identity");
    }

    return reader;
  }

  /// <summary>
  ///   Verifies that the opened hidraw device is the expected SCUF controller
  ///   by checking its vendor and product IDs using the HIDIOCGRAWINFO ioctl.
  /// </summary>
  /// <returns>
  ///   <see langword="true"/> if the device matches the expected controller IDs; otherwise <see langword="false"/>.
  /// </returns>
  private unsafe Boolean VerifyDevice()
  {
    const UInt16 ScufVendorId = 0x1b1c;
    const UInt16 ScufProductId = 0x3a08;
    HidrawDevInfo devInfo;
    Int32 result = Libc.Ioctl(FileDescriptor, HidrawIoctl.HIDIOCGRAWINFO, &devInfo);

    return result >= 0 && (UInt16)devInfo.Vendor == ScufVendorId && (UInt16)devInfo.Product == ScufProductId;
  }

  /// <summary>
  ///   Reads a raw HID report from the opened hidraw device. This method is non-blocking.
  ///   If no report is available, it returns 0 immediately.
  /// </summary>
  /// <param name="reportBuffer">
  ///   Buffer to receive the HID report; 64 bytes is appropriate for the expected reports.
  /// </param>
  /// <returns>
  ///   The number of bytes read, 0 if no report was available, or -1 on error.
  /// </returns>
  public unsafe Int32 ReadReport(Span<Byte> reportBuffer)
  {
    if (_Disposed)
    {
      return -1;
    }

    fixed (Byte* ptr = reportBuffer)
    {
      IntPtr bytesRead = Libc.Read(FileDescriptor, ptr, (UIntPtr)reportBuffer.Length);

      if (bytesRead >= 0)
      {
        return (Int32)bytesRead;
      }

      Int32 nativeErrorNumber = Libc.GetLastError();
      return nativeErrorNumber == Libc.EAGAIN ? 0 : -1;
    }
  }

  public void Dispose()
  {
    ReleaseResources();
    GC.SuppressFinalize(this);
  }

  private void ReleaseResources()
  {
    if (_Disposed)
    {
      return;
    }

    _Disposed = true;

    Libc.Close(FileDescriptor);
  }

  ~HidrawReader()
  {
    ReleaseResources();
  }
}