using EnvisionSuite.Core.Interop;
using EnvisionSuite.Core.Interop.LinuxInput;

namespace EnvisionSuite.Core.Input;

/// <summary>
///   Reads input events from a Linux evdev device.
/// </summary>
/// <remarks>
///   Evdev is the Linux kernel's interface for input devices.
///   This class opens a device in non-blocking mode and can optionally grab it
///   exclusively to prevent other applications from receiving its events.
/// </remarks>
public sealed class EvdevReader : IDisposable
{
  /// <summary>The native file descriptor used for ioctl and libc operations.</summary>
  public Int32 FileDescriptor { get; }

  private Boolean _Grabbed;
  private Boolean _Disposed;

  private EvdevReader(Int32 fileDescriptor) =>
    FileDescriptor = fileDescriptor;

  /// <summary>
  ///   Opens an evdev device for reading.
  /// </summary>
  /// <param name="devicePath">The path to a given evdev device.</param>
  /// <param name="grabExclusive">Grabs the device exclusively using EVIOCGRAB ioctl when true.</param>
  /// <remarks>
  ///   Exclusively grabbing prevents other applications from receiving a device's raw
  ///   input. This is essential to avoid double-input when bridging to a virtual controller.
  /// </remarks>
  /// <returns>
  ///   An <see cref="EvdevReader"/> instance if the device was successfully opened and, when
  ///   requested, grabbed exclusively; otherwise <see langword="null"/>.
  /// </returns>
  public static EvdevReader? Open(String devicePath, Boolean grabExclusive = true)
  {
    Int32 fileDescriptor = Libc.Open(devicePath, Libc.O_RDONLY | Libc.O_NONBLOCK);
    if (fileDescriptor < 0)
    {
      Int32 nativeErrorNumber = Libc.GetLastError();
      Console.Error.WriteLine($"Failed to open {devicePath}: {Libc.StrError(nativeErrorNumber)} (nativeErrorNumber={nativeErrorNumber})");

      return null;
    }

    EvdevReader reader = new(fileDescriptor);
    if (!grabExclusive || reader.TryGrab())
    {
      return reader;
    }

    reader.Dispose();
    return null;
  }

  /// <summary>
  ///   Grabs the device exclusively using the EVIOCGRAB ioctl.
  /// </summary>
  /// <returns>
  ///   <see langword="true"/> if the grab succeeded; otherwise <see langword="false"/>.
  /// </returns>
  private Boolean TryGrab()
  {
    Int32 result = Libc.Ioctl(FileDescriptor, EvdevIoctl.EVIOCGRAB, 1);
    if (result >= 0)
    {
      _Grabbed = true;
      return true;
    }

    Int32 nativeErrorNumber = Libc.GetLastError();
    Console.Error.WriteLine($"[error] Failed to grab device exclusively: {Libc.StrError(nativeErrorNumber)} (nativeErrorNumber={nativeErrorNumber})");
    return false;
  }

  private void ReleaseGrab()
  {
    if (!_Grabbed)
    {
      return;
    }

    Libc.Ioctl(FileDescriptor, EvdevIoctl.EVIOCGRAB, 0);
    _Grabbed = false;
  }

  /// <summary>
  ///   Reads pending input events from the device. This method is non-blocking.
  ///   If no events are available, it returns 0 immediately.
  /// </summary>
  /// <param name="eventBuffer">
  ///   A buffer to receive events. Should be large enough to hold multiple events
  ///   (typically 64 is sufficient for a single poll cycle).
  /// </param>
  /// <returns>
  ///   The number of events read, 0 if no events were available, or -1 on error.
  /// </returns>
  public unsafe Int32 ReadEvents(Span<InputEvent> eventBuffer)
  {
    if (_Disposed)
    {
      return -1;
    }

    fixed (InputEvent* ptr = eventBuffer)
    {
      IntPtr bytesRead = Libc.Read(FileDescriptor, ptr, (UIntPtr)(eventBuffer.Length * InputEvent.Size));

      if (bytesRead >= 0)
      {
        return (Int32)(bytesRead / InputEvent.Size);
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

    ReleaseGrab();
    Libc.Close(FileDescriptor);
  }

  ~EvdevReader()
  {
    ReleaseResources();
  }
}