using EnvisionSuite.Core.Interop;
using EnvisionSuite.Core.Interop.LinuxInput;

namespace EnvisionSuite.Core.Input;

/// <summary>
///   Reads input events from a Linux evdev device.
///   Evdev (event device) is the Linux kernel's interface for input devices,
///   providing structured events for buttons, axes, and other input types.
///   This class opens the device in non-blocking mode and can optionally grab it
///   exclusively to prevent other applications from receiving its events.
/// </summary>
public sealed class EvdevReader : IDisposable
{
  private Boolean _Disposed;
  private Boolean _Grabbed;

  private EvdevReader(Int32 fileDescriptor)
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
    ReleaseGrab();
    Libc.Close(FileDescriptor);
  }

  /// <summary>
  ///   Opens an evdev device for reading.
  /// </summary>
  /// <param name="devicePath">
  ///   Path to the evdev device (e.g., "/dev/input/event5").
  /// </param>
  /// <param name="grabExclusive">
  ///   If true, grabs the device exclusively using EVIOCGRAB ioctl.
  ///   This prevents other applications (including games) from seeing
  ///   the device's raw input, which is essential when bridging to a
  ///   virtual controller to avoid double-input.
  /// </param>
  /// <returns>
  ///   An <see cref="EvdevReader" /> instance, or null if the device could not be opened.
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
  ///   While grabbed, no other process can receive events from this device.
  /// </summary>
  /// <returns>True if the grab succeeded, false otherwise.</returns>
  private Boolean TryGrab()
  {
    Int32 result = Libc.Ioctl(FileDescriptor, EvdevIoctl.EVIOCGRAB, 1);
    if (result < 0)
    {
      Int32 nativeErrorNumber = Libc.GetLastError();
      Console.Error.WriteLine($"Failed to grab device exclusively: {Libc.StrError(nativeErrorNumber)} (nativeErrorNumber={nativeErrorNumber})");

      return false;
    }

    _Grabbed = true;
    return true;
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
  ///   Reads pending input events from the device.
  ///   This method is non-blocking - if no events are available, it returns 0 immediately.
  /// </summary>
  /// <param name="buffer">
  ///   Buffer to receive the events. Should be large enough to hold multiple events
  ///   (typically 64 is sufficient for a single poll cycle).
  /// </param>
  /// <returns>
  ///   The number of events read, 0 if no events were available, or -1 on error.
  /// </returns>
  public unsafe Int32 ReadEvents(Span<InputEvent> buffer)
  {
    if (_Disposed)
    {
      return -1;
    }

    fixed (InputEvent* ptr = buffer)
    {
      IntPtr bytesRead = Libc.Read(FileDescriptor, ptr, (UIntPtr)(buffer.Length * InputEvent.Size));

      if (bytesRead < 0)
      {
        Int32 nativeErrorNumber = Libc.GetLastError();
        return nativeErrorNumber == Libc.EAGAIN ? 0 : -1;
      }
      return (Int32)(bytesRead / InputEvent.Size);
    }
  }
}