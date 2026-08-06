namespace EnvisionSuite.Core.Mapping;

public sealed class InputState
{
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
    targetState.LeftStickClick = LeftStickClick;
    targetState.RightStickClick = RightStickClick;
    targetState.Paddle1 = Paddle1;
    targetState.Paddle2 = Paddle2;
    targetState.Paddle3 = Paddle3;
    targetState.Paddle4 = Paddle4;
    targetState.ButtonLeftSAX = ButtonLeftSAX;
    targetState.ButtonRightSAX = ButtonRightSAX;
  }

  public Int16 LeftStickX { get; set; }
  public Int16 LeftStickY { get; set; }
  public Int16 RightStickX { get; set; }
  public Int16 RightStickY { get; set; }

  public Int16 LeftTrigger { get; set; }
  public Int16 RightTrigger { get; set; }

  public SByte DpadX { get; set; }
  public SByte DpadY { get; set; }

  public Boolean ButtonA { get; set; }
  public Boolean ButtonB { get; set; }
  public Boolean ButtonX { get; set; }
  public Boolean ButtonY { get; set; }

  public Boolean BumperLeft { get; set; }
  public Boolean BumperRight { get; set; }

  public Boolean ButtonStart { get; set; }
  public Boolean ButtonSelect { get; set; }
  public Boolean ButtonGuide { get; set; }

  public Boolean LeftStickClick { get; set; }
  public Boolean RightStickClick { get; set; }

  public Boolean Paddle1 { get; set; }
  public Boolean Paddle2 { get; set; }
  public Boolean Paddle3 { get; set; }
  public Boolean Paddle4 { get; set; }

  public Boolean ButtonLeftSAX { get; set; }
  public Boolean ButtonRightSAX { get; set; }

  public Boolean IsDirty { get; private set; }

  public void MarkDirty() =>
    IsDirty = true;

  public void ClearDirty() =>
    IsDirty = false;
}