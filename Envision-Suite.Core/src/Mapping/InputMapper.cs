using System.Collections.Immutable;

namespace EnvisionSuite.Core.Mapping;

public sealed class InputMapper
{
  private readonly ImmutableArray<InputMapping> _Mappings =
  [
    new ButtonMapping(PhysicalButton.ButtonA, VirtualButton.ButtonA),
    new ButtonMapping(PhysicalButton.ButtonB, VirtualButton.ButtonB),
    new ButtonMapping(PhysicalButton.ButtonX, VirtualButton.ButtonX),
    new ButtonMapping(PhysicalButton.ButtonY, VirtualButton.ButtonY),
    new ButtonMapping(PhysicalButton.BumperLeft, VirtualButton.BumperLeft),
    new ButtonMapping(PhysicalButton.BumperRight, VirtualButton.BumperRight),
    new ButtonMapping(PhysicalButton.ButtonStart, VirtualButton.ButtonStart),
    new ButtonMapping(PhysicalButton.ButtonSelect, VirtualButton.ButtonSelect),
    new ButtonMapping(PhysicalButton.ButtonGuide, VirtualButton.ButtonGuide),
    new ButtonMapping(PhysicalButton.LeftStickClick, VirtualButton.LeftStickClick),
    new ButtonMapping(PhysicalButton.RightStickClick, VirtualButton.RightStickClick),
    new ButtonMapping(PhysicalButton.Paddle1, VirtualButton.LeftStickClick),
    new ButtonMapping(PhysicalButton.Paddle2, VirtualButton.RightStickClick),
    new ButtonMapping(PhysicalButton.Paddle3, VirtualButton.ButtonX),
    new ButtonMapping(PhysicalButton.Paddle4, VirtualButton.ButtonA),
    new ButtonMapping(PhysicalButton.ButtonLeftSAX, VirtualButton.None),
    new ButtonMapping(PhysicalButton.ButtonRightSAX, VirtualButton.None),
    new AxisMapping(PhysicalAxis.LeftStickX, VirtualAxis.LeftStickX, false),
    new AxisMapping(PhysicalAxis.LeftStickY, VirtualAxis.LeftStickY, false),
    new AxisMapping(PhysicalAxis.RightStickX, VirtualAxis.RightStickX, false),
    new AxisMapping(PhysicalAxis.RightStickY, VirtualAxis.RightStickY, false),
    new AxisMapping(PhysicalAxis.LeftTrigger, VirtualAxis.LeftTrigger, false),
    new AxisMapping(PhysicalAxis.RightTrigger, VirtualAxis.RightTrigger, false),
    new AxisMapping(PhysicalAxis.DpadX, VirtualAxis.DpadX, false),
    new AxisMapping(PhysicalAxis.DpadY, VirtualAxis.DpadY, false)
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

  private static Int32 GetPhysicalAxisValue(InputState inputState, PhysicalAxis axis) =>
    axis switch
    {
      PhysicalAxis.LeftStickX => inputState.LeftStickX,
      PhysicalAxis.LeftStickY => inputState.LeftStickY,
      PhysicalAxis.RightStickX => inputState.RightStickX,
      PhysicalAxis.RightStickY => inputState.RightStickY,
      PhysicalAxis.LeftTrigger => inputState.LeftTrigger,
      PhysicalAxis.RightTrigger => inputState.RightTrigger,
      PhysicalAxis.DpadX => inputState.DpadX,
      PhysicalAxis.DpadY => inputState.DpadY,
      _ => 0
    };

  public Boolean ResolveButton(InputState inputState, VirtualButton target)
  {
    if (target == VirtualButton.None)
    {
      return false;
    }

    foreach (InputMapping mapping in _Mappings)
    {
      if (mapping is ButtonMapping buttonMapping && buttonMapping.Target == target && IsPhysicalButtonPressed(inputState, buttonMapping.Source))
      {
        return true;
      }

      if (mapping is AxisToButtonMapping axisMapping && axisMapping.Target == target)
      {
        Int32 value = GetPhysicalAxisValue(inputState, axisMapping.Source);

        if (IsAxisActivated(value, axisMapping.Direction, axisMapping.Threshold))
        {
          return true;
        }
      }
    }

    return false;
  }

  public Int32 ResolveAxis(InputState inputState, VirtualAxis target)
  {
    if (target == VirtualAxis.None)
    {
      return 0;
    }

    Int32 resolvedValue = 0;

    foreach (InputMapping mapping in _Mappings)
    {
      Int32? contribution = mapping switch
      {
        AxisMapping axisMapping when axisMapping.Target == target =>
          ResolveAxisMappingValue(inputState, axisMapping),
        ButtonToAxisMapping buttonMapping when buttonMapping.Target == target &&
          IsPhysicalButtonPressed(inputState, buttonMapping.Source) => buttonMapping.PressedValue,
        _ => null
      };

      if (contribution is null)
      {
        continue;
      }

      resolvedValue = CombineAxisValues(target, resolvedValue, contribution.Value);
    }

    return resolvedValue;
  }

  private static Int32 ResolveAxisMappingValue(InputState inputState, AxisMapping mapping)
  {
    Int32 value = GetPhysicalAxisValue(inputState, mapping.Source);

    if (!mapping.Invert)
    {
      return value;
    }

    return mapping.Source switch
    {
      PhysicalAxis.LeftTrigger or PhysicalAxis.RightTrigger => 1023 - value,
      PhysicalAxis.DpadX or PhysicalAxis.DpadY => -value,
      _ => InvertAxisValue(value)
    };
  }

  private static Boolean IsAxisActivated(Int32 value, AxisDirection direction, Int32 threshold) =>
    direction switch
    {
      AxisDirection.Positive => value >= threshold,
      AxisDirection.Negative => value <= -threshold,
      _ => false
    };

  private static Int32 CombineAxisValues(VirtualAxis target, Int32 currentValue, Int32 additionalValue) =>
    target switch
    {
      VirtualAxis.LeftTrigger or VirtualAxis.RightTrigger => Math.Max(currentValue, additionalValue),
      VirtualAxis.DpadX or VirtualAxis.DpadY => Math.Clamp(currentValue + additionalValue, -1, 1),
      _ => Math.Abs((Int64)additionalValue) >= Math.Abs((Int64)currentValue)
          ? additionalValue : currentValue
    };

  private static Int32 InvertAxisValue(Int32 value) =>
    Math.Clamp(-value, Int16.MinValue, Int16.MaxValue);
}