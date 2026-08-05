namespace EnvisionSuite.Core.Interop.LinuxInput;

/// <summary>
///   Absolute axis codes for gamepad axes.
///   These are the standard Linux kernel axis codes used by evdev.
/// </summary>
/// <remarks>
///   Note: The Scuf Envision Pro V2 uses non-standard axis assignments.
///   See <see cref="Mapping.EnvisionMapping" /> for the actual mappings.
/// </remarks>
public static class AbsCodes
{
  /// <summary>Left stick X axis (standard).</summary>
  public const UInt16 ABS_X = 0x00;

  /// <summary>Left stick Y axis (standard).</summary>
  public const UInt16 ABS_Y = 0x01;

  /// <summary>Z axis - typically left trigger, but Scuf V2 uses for right stick X.</summary>
  public const UInt16 ABS_Z = 0x02;

  /// <summary>Right stick X (standard), but Scuf V2 uses for left trigger.</summary>
  public const UInt16 ABS_RX = 0x03;

  /// <summary>Right stick Y (standard), but Scuf V2 uses for right trigger.</summary>
  public const UInt16 ABS_RY = 0x04;

  /// <summary>Right trigger (standard), but Scuf V2 uses for right stick Y.</summary>
  public const UInt16 ABS_RZ = 0x05;

  /// <summary>D-pad X axis (-1 = left, 0 = center, 1 = right).</summary>
  public const UInt16 ABS_HAT0X = 0x10;

  /// <summary>D-pad Y axis (-1 = up, 0 = center, 1 = down).</summary>
  public const UInt16 ABS_HAT0Y = 0x11;
}
