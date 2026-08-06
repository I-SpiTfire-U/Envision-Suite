using System.Collections.Immutable;

namespace EnvisionSuite.Core.Mapping;

public sealed class InputMapper
{
  private readonly ImmutableArray<ButtonMapping> _ButtonMappings =
  [
    new(PhysicalButton.ButtonA, VirtualButton.ButtonA),
    new(PhysicalButton.ButtonB, VirtualButton.ButtonB),
    new(PhysicalButton.ButtonX, VirtualButton.ButtonX),
    new(PhysicalButton.ButtonY, VirtualButton.ButtonY),
    new(PhysicalButton.BumperLeft, VirtualButton.BumperLeft),
    new(PhysicalButton.BumperRight, VirtualButton.BumperRight),
    new(PhysicalButton.ButtonStart, VirtualButton.ButtonStart),
    new(PhysicalButton.ButtonSelect, VirtualButton.ButtonSelect),
    new(PhysicalButton.ButtonGuide, VirtualButton.ButtonGuide),
    new(PhysicalButton.LeftStickClick, VirtualButton.LeftStickClick),
    new(PhysicalButton.RightStickClick, VirtualButton.RightStickClick),
    new(PhysicalButton.Paddle1, VirtualButton.None),
    new(PhysicalButton.Paddle2, VirtualButton.None),
    new(PhysicalButton.Paddle3, VirtualButton.None),
    new(PhysicalButton.Paddle4, VirtualButton.ButtonA),
    new(PhysicalButton.ButtonLeftSAX, VirtualButton.None),
    new(PhysicalButton.ButtonRightSAX, VirtualButton.None),
  ];

  private static Boolean IsPhysicalButtonPressed(InputState inputState, PhysicalButton button) =>
    button switch
    {
      PhysicalButton.ButtonA => inputState.ButtonA,
      PhysicalButton.ButtonB => inputState.ButtonB,
      PhysicalButton.ButtonX => inputState.ButtonX,
      PhysicalButton.ButtonY => inputState.ButtonY,

      PhysicalButton.BumperLeft => inputState.BumperLeft,
      PhysicalButton.BumperRight => inputState.BumperRight,

      PhysicalButton.ButtonStart => inputState.ButtonStart,
      PhysicalButton.ButtonSelect => inputState.ButtonSelect,
      PhysicalButton.ButtonGuide => inputState.ButtonGuide,

      PhysicalButton.LeftStickClick => inputState.LeftStickClick,
      PhysicalButton.RightStickClick => inputState.RightStickClick,

      PhysicalButton.Paddle1 => inputState.Paddle1,
      PhysicalButton.Paddle2 => inputState.Paddle2,
      PhysicalButton.Paddle3 => inputState.Paddle3,
      PhysicalButton.Paddle4 => inputState.Paddle4,

      PhysicalButton.ButtonLeftSAX => inputState.ButtonLeftSAX,
      PhysicalButton.ButtonRightSAX => inputState.ButtonRightSAX,
      _ => false
    };

  public Boolean ResolveButton(InputState inputState, VirtualButton target)
  {
    if (target == VirtualButton.None)
    {
      return false;
    }

    foreach (ButtonMapping mapping in _ButtonMappings)
    {
      if (mapping.Target != target)
      {
        continue;
      }

      if (IsPhysicalButtonPressed(inputState, mapping.Source))
      {
        return true;
      }
    }

    return false;
  }
}