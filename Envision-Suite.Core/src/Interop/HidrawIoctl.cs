namespace EnvisionSuite.Core.Interop;

/// <summary>
///     ioctl request codes for hidraw devices.
///     These are used to query device information and control hidraw behavior.
/// </summary>
/// <remarks>
///     The ioctl codes are computed using the Linux _IOR/_IOW macros with 'H' as the type.
/// </remarks>
public static class HidrawIoctl
{
  /// <summary>
  ///     HIDIOCGRAWINFO - Get raw device info (vendor/product IDs).
  ///     <c>_IOR('H', 0x03, struct hidraw_devinfo)</c> = 0x80084803
  /// </summary>
  public const UIntPtr HIDIOCGRAWINFO = 0x80084803;

  /// <summary>
  ///     HIDIOCGRAWNAME(256) - Get raw device name string (up to 256 bytes).
  ///     <c>_IOC(_IOC_READ, 'H', 0x04, 256)</c> = 0x81004804
  /// </summary>
  public const UIntPtr HIDIOCGRAWNAME_256 = 0x81004804;
}