namespace EnvisionSuite.Core.Interop.LinuxInput;

/// <summary>
///     ioctl request codes for evdev devices.
/// </summary>
public static class EvdevIoctl
{
  /// <summary>
  ///     EVIOCGRAB - Grab or release exclusive access to the device.
  ///     When grabbed, no other process receives events from this device.
  ///     <c>_IOW('E', 0x90, int)</c> = 0x40044590
  /// </summary>
  /// <remarks>
  ///     Pass 1 to grab, 0 to release.
  /// </remarks>
  public const UIntPtr EVIOCGRAB = 0x40044590;
}