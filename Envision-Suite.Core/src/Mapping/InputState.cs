namespace EnvisionSuite.Core.Mapping;

public sealed class InputState
{
  /// <summary>
  ///     Creates a shallow copy of this state.
  ///     Useful for comparing previous and current states.
  /// </summary>
  /// <returns>A new <see cref="InputState" /> with the same values (dirty flag is NOT copied).</returns>
  /// <remarks>
  ///     Uses MemberwiseClone for maintainability - automatically includes any new fields.
  ///     The dirty flag is reset to false in the clone.
  /// </remarks>
  public InputState Clone()
  {
    InputState clone = (InputState)MemberwiseClone();
    clone.IsDirty = false;
    return clone;
  }

  public void CopyTo(InputState targetState)
  {
    targetState.LeftStickX = LeftStickX;
    targetState.LeftStickY = LeftStickY;
    targetState.RightStickX = RightStickX;
    targetState.RightStickY = RightStickY;
    targetState.LeftTrigger = LeftTrigger;
    targetState.RightTrigger = RightTrigger;
    targetState.DpadX = DpadX;
    targetState.DpadY = DpadY;
    targetState.ButtonA = ButtonA;
    targetState.ButtonB = ButtonB;
    targetState.ButtonX = ButtonX;
    targetState.ButtonY = ButtonY;
    targetState.BumperLeft = BumperLeft;
    targetState.BumperRight = BumperRight;
    targetState.ButtonStart = ButtonStart;
    targetState.ButtonSelect = ButtonSelect;
    targetState.ButtonGuide = ButtonGuide;
    targetState.ThumbLeft = ThumbLeft;
    targetState.ThumbRight = ThumbRight;
    targetState.Paddle1 = Paddle1;
    targetState.Paddle2 = Paddle2;
    targetState.Paddle3 = Paddle3;
    targetState.Paddle4 = Paddle4;
    targetState.ButtonLeftSAX = ButtonLeftSAX;
    targetState.ButtonRightSAX = ButtonRightSAX;
  }

  /// <summary>
  ///     Left stick X-axis position.
  ///     Range: -32768 (full left) to 32767 (full right), 0 is center.
  /// </summary>
  public Int16 LeftStickX { get; set; }

  /// <summary>
  ///     Left stick Y-axis position.
  ///     Range: -32768 (full up) to 32767 (full down), 0 is center.
  /// </summary>
  public Int16 LeftStickY { get; set; }

  /// <summary>
  ///     Right stick X-axis position.
  ///     Range: -32768 (full left) to 32767 (full right), 0 is center.
  /// </summary>
  public Int16 RightStickX { get; set; }

  /// <summary>
  ///     Right stick Y-axis position.
  ///     Range: -32768 (full up) to 32767 (full down), 0 is center.
  /// </summary>
  public Int16 RightStickY { get; set; }

  /// <summary>
  ///     Left trigger (LT/L2) position.
  ///     Range: 0 (released) to 1023 (fully pressed).
  /// </summary>
  public Int16 LeftTrigger { get; set; }

  /// <summary>
  ///     Right trigger (RT/R2) position.
  ///     Range: 0 (released) to 1023 (fully pressed).
  /// </summary>
  public Int16 RightTrigger { get; set; }

  /// <summary>
  ///     D-pad X-axis.
  ///     Values: -1 (left), 0 (center), 1 (right).
  /// </summary>
  public SByte DpadX { get; set; }

  /// <summary>
  ///     D-pad Y-axis.
  ///     Values: -1 (up), 0 (center), 1 (down).
  /// </summary>
  public SByte DpadY { get; set; }

  /// <summary>A button (Xbox) / Cross (PlayStation) state.</summary>
  public Boolean ButtonA { get; set; }

  /// <summary>B button (Xbox) / Circle (PlayStation) state.</summary>
  public Boolean ButtonB { get; set; }

  /// <summary>X button (Xbox) / Square (PlayStation) state.</summary>
  public Boolean ButtonX { get; set; }

  /// <summary>Y button (Xbox) / Triangle (PlayStation) state.</summary>
  public Boolean ButtonY { get; set; }

  /// <summary>Left bumper (LB/L1) state.</summary>
  public Boolean BumperLeft { get; set; }

  /// <summary>Right bumper (RB/R1) state.</summary>
  public Boolean BumperRight { get; set; }

  /// <summary>Start / Options button state.</summary>
  public Boolean ButtonStart { get; set; }

  /// <summary>Back / Select / Share button state.</summary>
  public Boolean ButtonSelect { get; set; }

  /// <summary>Guide / Home / Xbox button state.</summary>
  public Boolean ButtonGuide { get; set; }

  /// <summary>Left stick click (L3/LS) state.</summary>
  public Boolean ThumbLeft { get; set; }

  /// <summary>Right stick click (R3/RS) state.</summary>
  public Boolean ThumbRight { get; set; }

  /// <summary>Paddle 1 (P1) state.</summary>
  public Boolean Paddle1 { get; set; }

  /// <summary>Paddle 2 (P2) state.</summary>
  public Boolean Paddle2 { get; set; }

  /// <summary>Paddle 3 (P3) state.</summary>
  public Boolean Paddle3 { get; set; }

  /// <summary>Paddle 4 (P4) state. Note: Scuf V2 only has 3 paddles.</summary>
  public Boolean Paddle4 { get; set; }

  /// <summary>Left SAX side-button state.</summary>
  public Boolean ButtonLeftSAX { get; set; }

  /// <summary>Right SAX side-button state.</summary>
  public Boolean ButtonRightSAX { get; set; }

  /// <summary>
  ///     Gets whether the state has been modified since the last <see cref="ClearDirty" /> call.
  ///     Used to determine if the virtual gamepad needs to emit new events.
  /// </summary>
  public Boolean IsDirty { get; private set; }

  public void MarkDirty() =>
    IsDirty = true;

  public void ClearDirty() =>
    IsDirty = false;
}