namespace EnvisionSuite.Core.Interop.LinuxInput;

/// <summary>
///     Button/key codes for gamepad buttons.
///     These are the standard Linux kernel button codes used by evdev.
/// </summary>
/// <remarks>
///     Note: The Scuf Envision Pro V2 uses non-standard button codes.
///     See <see cref="Mapping.EnvisionMapping" /> for the actual mappings.
/// </remarks>
public static class ButtonCodes
{
  /// <summary>Base code for miscellaneous buttons.</summary>
  public const UInt16 BTN_MISC = 0x100;

  /// <summary>Base code for gamepad buttons.</summary>
  public const UInt16 BTN_GAMEPAD = 0x130;

  #region Face_Buttons
  /// <summary>South button (A on Xbox, Cross on PlayStation).</summary>
  public const UInt16 BTN_SOUTH = 0x130;

  /// <summary>East button (B on Xbox, Circle on PlayStation).</summary>
  public const UInt16 BTN_EAST = 0x131;

  /// <summary>North button (Y on Xbox, Triangle on PlayStation).</summary>
  public const UInt16 BTN_NORTH = 0x133;

  /// <summary>West button (X on Xbox, Square on PlayStation). Used by Scuf V2 for LB.</summary>
  public const UInt16 BTN_WEST = 0x134;

  /// <summary>C button (used by Scuf V2 for X button).</summary>
  public const UInt16 BTN_C = 0x132;
  #endregion

  #region Face_Button_Aliases
  /// <summary>Alias for BTN_SOUTH.</summary>
  public const UInt16 BTN_A = BTN_SOUTH;

  /// <summary>Alias for BTN_EAST.</summary>
  public const UInt16 BTN_B = BTN_EAST;

  /// <summary>Alias for BTN_NORTH (confusingly named in kernel).</summary>
  public const UInt16 BTN_X = BTN_NORTH;

  /// <summary>Alias for BTN_WEST (confusingly named in kernel).</summary>
  public const UInt16 BTN_Y = BTN_WEST;
  #endregion

  #region Triggers_And_Bumpers
  /// <summary>Z button (used by Scuf V2 for RB).</summary>
  public const UInt16 BTN_Z = 0x135;
  
  /// <summary>LB / L1 (standard). Used by Scuf V2 for Select.</summary>
  public const UInt16 BTN_TL = 0x136;

  /// <summary>RB / R1 (standard). Used by Scuf V2 for Start.</summary>
  public const UInt16 BTN_TR = 0x137;

  /// <summary>LT / L2 (standard). Used by Scuf V2 for L3.</summary>
  public const UInt16 BTN_TL2 = 0x138;

  /// <summary>RT / R2 (standard). Used by Scuf V2 for R3.</summary>
  public const UInt16 BTN_TR2 = 0x139;
  #endregion

  #region Menu_Buttons
  /// <summary>Back / Share / Select button (Standard).</summary>
  public const UInt16 BTN_SELECT = 0x13a;

  /// <summary>Start / Options button (Standard).</summary>
  public const UInt16 BTN_START = 0x13b;

  /// <summary>Guide / Home (Standard).</summary>
  public const UInt16 BTN_MODE = 0x13c;
  #endregion

  #region Thumb_Stick_Clicks
  /// <summary>Left stick click / L3 (standard).</summary>
  public const UInt16 BTN_THUMBL = 0x13d;

  /// <summary>Right stick click / R3 (standard).</summary>
  public const UInt16 BTN_THUMBR = 0x13e;
  #endregion

  #region Back_Paddles
  /// <summary>Paddle 1 (for Elite controllers).</summary>
  public const UInt16 BTN_TRIGGER_HAPPY1 = 0x2c0;

  /// <summary>Paddle 2 (for Elite controllers).</summary>
  public const UInt16 BTN_TRIGGER_HAPPY2 = 0x2c1;

  /// <summary>Paddle 3 (for Elite controllers).</summary>
  public const UInt16 BTN_TRIGGER_HAPPY3 = 0x2c2;

  /// <summary>Paddle 4 (for Elite controllers).</summary>
  public const UInt16 BTN_TRIGGER_HAPPY4 = 0x2c3;
  #endregion

  #region SAX_Buttons
  /// <summary>Additional controller button 5 (Left SAX Button).</summary>
  public const UInt16 BTN_TRIGGER_HAPPY5 = 0x2c4;

  /// <summary>Additional controller button 6 (Right SAX Button).</summary>
  public const UInt16 BTN_TRIGGER_HAPPY6 = 0x2c5;
  #endregion
}