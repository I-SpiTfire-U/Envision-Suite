using System.Collections.Immutable;

namespace EnvisionSuite.Core.Mapping;

public sealed class InputMapper
{
  private readonly ImmutableArray<InputMapping> _InputMappings =
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

  private static Boolean IsPhysicalButtonPressed(InputState inputState, PhysicalButton targetButton) =>
    targetButton switch
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

  private static Int32 GetPhysicalAxisValue(InputState inputState, PhysicalAxis targetAxis) =>
    targetAxis switch
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

  private static Boolean IsAxisActivated(Int32 axisValue, AxisDirection axisDirection, Int32 threshold) =>
    axisDirection switch
    {
      AxisDirection.Positive => axisValue >= threshold,
      AxisDirection.Negative => axisValue <= -threshold,
      _ => false
    };

  public Boolean ResolveButton(InputState inputState, VirtualButton targetButton)
  {
    if (targetButton == VirtualButton.None)
    {
      return false;
    }

    foreach (InputMapping mapping in _InputMappings)
    {
      if (mapping is ButtonMapping buttonMapping && buttonMapping.Target == targetButton && IsPhysicalButtonPressed(inputState, buttonMapping.Source))
      {
        return true;
      }

      if (mapping is not AxisToButtonMapping axisMapping || axisMapping.Target != targetButton)
      {
        continue;
      }

      Int32 value = GetPhysicalAxisValue(inputState, axisMapping.Source);
      if (IsAxisActivated(value, axisMapping.Direction, axisMapping.Threshold))
      {
        return true;
      }
    }

    return false;
  }

  private static Int32 InvertAxisValue(Int32 axisValue) =>
    Math.Clamp(-axisValue, Int16.MinValue, Int16.MaxValue);

  private static Int32 ResolveAxisMappingValue(InputState inputState, AxisMapping axisMapping)
  {
    Int32 axisValue = GetPhysicalAxisValue(inputState, axisMapping.Source);

    if (!axisMapping.Invert)
    {
      return axisValue;
    }

    return axisMapping.Source switch
    {
      PhysicalAxis.LeftTrigger or PhysicalAxis.RightTrigger => 1023 - axisValue,
      PhysicalAxis.DpadX or PhysicalAxis.DpadY => -axisValue,
      _ => InvertAxisValue(axisValue)
    };
  }

  private static Int32 CombineAxisValues(VirtualAxis targetAxis, Int32 currentValue, Int32 additionalValue) =>
    targetAxis switch
    {
      VirtualAxis.LeftTrigger or VirtualAxis.RightTrigger => Math.Max(currentValue, additionalValue),
      VirtualAxis.DpadX or VirtualAxis.DpadY => Math.Clamp(currentValue + additionalValue, -1, 1),
      _ => Math.Abs((Int64)additionalValue) >= Math.Abs((Int64)currentValue)
          ? additionalValue : currentValue
    };

  public Int32 ResolveAxis(InputState inputState, VirtualAxis targetAxis)
  {
    if (targetAxis == VirtualAxis.None)
    {
      return 0;
    }

    Int32 resolvedValue = 0;

    foreach (InputMapping mapping in _InputMappings)
    {
      Int32? contribution = mapping switch
      {
        AxisMapping axisMapping when axisMapping.Target == targetAxis =>
          ResolveAxisMappingValue(inputState, axisMapping),
        ButtonToAxisMapping buttonMapping when buttonMapping.Target == targetAxis &&
          IsPhysicalButtonPressed(inputState, buttonMapping.Source) => buttonMapping.PressedValue,
        _ => null
      };

      if (contribution is null)
      {
        continue;
      }

      resolvedValue = CombineAxisValues(targetAxis, resolvedValue, contribution.Value);
    }

    return resolvedValue;
  }
}