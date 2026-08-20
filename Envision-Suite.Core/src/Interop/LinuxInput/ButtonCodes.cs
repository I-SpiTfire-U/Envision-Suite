namespace EnvisionSuite.Core.Interop.LinuxInput;

/// <summary>
///   These are the standard Linux kernel button codes used by evdev for the Envision Pro V2 gamepad.
/// </summary>
/// <remarks>
///   The SCUF Envision Pro V2 uses non-standard button codes. See <see cref="Mapping.EnvisionMapping" /> for the actual mappings.
/// </remarks>
public static class ButtonCodes
{
  /// <summary>Miscellaneous buttons code.</summary>
  public const UInt16 BTN_MISC = 0x100;

  /// <summary>Base gamepad buttons code.</summary>
  public const UInt16 BTN_GAMEPAD = 0x130;

  /// <summary>A / Cross (standard).</summary>
  public const UInt16 BTN_SOUTH = 0x130;

  /// <summary>B / Circle (standard).</summary>
  public const UInt16 BTN_EAST = 0x131;

  /// <summary>Y / Triangle (standard).</summary>
  public const UInt16 BTN_NORTH = 0x133;

  /// <summary>X / Square (standard). Used by Envision Pro V2 for LB.</summary>
  public const UInt16 BTN_WEST = 0x134;

  /// <summary>Used by Envision Pro V2 for X button.</summary>
  public const UInt16 BTN_C = 0x132;

  public const UInt16 BTN_A = BTN_SOUTH;
  public const UInt16 BTN_B = BTN_EAST;
  public const UInt16 BTN_X = BTN_NORTH;
  public const UInt16 BTN_Y = BTN_WEST;

  /// <summary>Used by SCUF V2 for RB.</summary>
  public const UInt16 BTN_Z = 0x135;
  
  /// <summary>LB / L1 (standard). Used by Envision Pro V2 for Select.</summary>
  public const UInt16 BTN_TL = 0x136;

  /// <summary>RB / R1 (standard). Used by Envision Pro V2 for Start.</summary>
  public const UInt16 BTN_TR = 0x137;

  /// <summary>LT / L2 (standard). Used by Envision Pro V2 for L3.</summary>
  public const UInt16 BTN_TL2 = 0x138;

  /// <summary>RT / R2 (standard). Used by Envision Pro V2 for R3.</summary>
  public const UInt16 BTN_TR2 = 0x139;

  public const UInt16 BTN_SELECT = 0x13a;
  public const UInt16 BTN_START = 0x13b;
  public const UInt16 BTN_MODE = 0x13c;

  public const UInt16 BTN_THUMBL = 0x13d;
  public const UInt16 BTN_THUMBR = 0x13e;

  /// <summary>Paddle 1 (Elite controllers).</summary>
  public const UInt16 BTN_TRIGGER_HAPPY1 = 0x2c0;

  /// <summary>Paddle 2 (Elite controllers).</summary>
  public const UInt16 BTN_TRIGGER_HAPPY2 = 0x2c1;

  /// <summary>Paddle 3 (Elite controllers).</summary>
  public const UInt16 BTN_TRIGGER_HAPPY3 = 0x2c2;

  /// <summary>Paddle 4 (Elite controllers).</summary>
  public const UInt16 BTN_TRIGGER_HAPPY4 = 0x2c3;

  /// <summary>Additional button 5 (Left SAX Button).</summary>
  public const UInt16 BTN_TRIGGER_HAPPY5 = 0x2c4;

  /// <summary>Additional button 6 (Right SAX Button).</summary>
  public const UInt16 BTN_TRIGGER_HAPPY6 = 0x2c5;
}