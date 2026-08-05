namespace EnvisionSuite.Core.Interop.Uinput;

/// <summary>
///   ioctl request codes for the uinput virtual device interface.
///   These are used to configure and create virtual input devices.
/// </summary>
/// <remarks>
///   The ioctl codes are computed using the Linux _IOW/_IO macros with 'U' as the type.
///   The typical sequence to create a device is:
///   1. Open /dev/uinput
///   2. UI_SET_EVBIT for each event type (EV_KEY, EV_ABS, etc.)
///   3. UI_SET_KEYBIT for each button
///   4. UI_SET_ABSBIT + UI_ABS_SETUP for each axis
///   5. UI_DEV_SETUP to configure device identity
///   6. UI_DEV_CREATE to create the device
///   7. Write input_event structures to emit events
///   8. UI_DEV_DESTROY when done
/// </remarks>
public static class UinputIoctl
{
  /// <summary>
  ///   UI_SET_EVBIT - Enable an event type on the virtual device.
  ///   <c>_IOW('U', 100, int)</c> = 0x40045564
  /// </summary>
  /// <remarks>
  ///   Pass the event type (EV_KEY, EV_ABS, EV_SYN, etc.) as the argument.
  /// </remarks>
  public const UIntPtr UI_SET_EVBIT = 0x40045564;

  /// <summary>
  ///   UI_SET_KEYBIT - Enable a key/button code on the virtual device.
  ///   <c>_IOW('U', 101, int)</c> = 0x40045565
  /// </summary>
  /// <remarks>
  ///   Pass the button code (BTN_SOUTH, BTN_EAST, etc.) as the argument.
  ///   Requires UI_SET_EVBIT(EV_KEY) first.
  /// </remarks>
  public const UIntPtr UI_SET_KEYBIT = 0x40045565;

  /// <summary>
  ///   UI_SET_ABSBIT - Enable an absolute axis on the virtual device.
  ///   <c>_IOW('U', 103, int)</c> = 0x40045567
  /// </summary>
  /// <remarks>
  ///   Pass the axis code (ABS_X, ABS_Y, etc.) as the argument.
  ///   Requires UI_SET_EVBIT(EV_ABS) first.
  ///   Should be followed by UI_ABS_SETUP to configure axis parameters.
  /// </remarks>
  public const UIntPtr UI_SET_ABSBIT = 0x40045567;

  /// <summary>
  ///   UI_DEV_SETUP - Configure device identification (name, vendor/product IDs).
  ///   <c>_IOW('U', 3, struct uinput_setup)</c> = 0x405c5503
  /// </summary>
  /// <remarks>
  ///   Pass a pointer to UinputSetup structure.
  /// </remarks>
  public const UIntPtr UI_DEV_SETUP = 0x405c5503;

  /// <summary>
  ///   UI_ABS_SETUP - Configure absolute axis parameters (min, max, fuzz, flat).
  ///   <c>_IOW('U', 4, struct uinput_abs_setup)</c> = 0x401c5504
  /// </summary>
  /// <remarks>
  ///   Pass a pointer to UinputAbsSetup structure.
  ///   Must be called after UI_SET_ABSBIT for the axis.
  /// </remarks>
  public const UIntPtr UI_ABS_SETUP = 0x401c5504;

  /// <summary>
  ///   UI_DEV_CREATE - Create the virtual device.
  ///   <c>_IO('U', 1)</c> = 0x5501
  /// </summary>
  /// <remarks>
  ///   After this call, the device appears in /dev/input/ and applications can use it.
  /// </remarks>
  public const UIntPtr UI_DEV_CREATE = 0x5501;

  /// <summary>
  ///   UI_DEV_DESTROY - Destroy the virtual device.
  ///   <c>_IO('U', 2)</c> = 0x5502
  /// </summary>
  /// <remarks>
  ///   Should be called before closing the uinput file descriptor.
  /// </remarks>
  public const UIntPtr UI_DEV_DESTROY = 0x5502;
}