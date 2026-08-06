using EnvisionSuite.Core.Interop.LinuxInput;

namespace EnvisionSuite.Core.Mapping;

public static class EnvisionMapping
{
  /// <summary>
  ///   Processes a single evdev input event and updates the input state.
  /// </summary>
  /// <param name="inputEvent">The input event to process (passed by readonly reference to avoid copying).</param>
  /// <param name="inputState">The input state to update.</param>
  /// <remarks>
  ///   Handles EV_ABS (axis), EV_KEY (button), and EV_SYN (sync) events.
  ///   The sync event is ignored as state is processed continuously.
  ///   Uses 'in' parameter for the 24-byte struct to avoid defensive copies while preventing mutation.
  /// </remarks>
  public static void ProcessEvdevEvent(in InputEvent inputEvent, InputState inputState)
  {
    switch (inputEvent.Type)
    {
      case EventTypes.EV_ABS:
        ProcessAbsoluteAxis(inputEvent.Code, inputEvent.Value, inputState);
        break;

      case EventTypes.EV_KEY:
        ProcessButton(inputEvent.Code, inputEvent.Value != 0, inputState);
        break;

      case EventTypes.EV_SYN:
        break;
    }
  }

  private static void ProcessAbsoluteAxis(UInt16 axisCode, Int32 axisValue, InputState inputState)
  {
    switch (axisCode)
    {
      case AbsCodes.ABS_X:
        inputState.LeftStickX = (Int16)axisValue;
        break;

      case AbsCodes.ABS_Y:
        inputState.LeftStickY = (Int16)axisValue;
        break;

      case AbsCodes.ABS_Z: // Right stick X - Envision reports this on ABS_Z (non-standard!)
        inputState.RightStickX = (Int16)axisValue;
        break;

      case AbsCodes.ABS_RX: // Left trigger - Envision reports this on ABS_RX (non-standard!)
        inputState.LeftTrigger = (Int16)axisValue;
        break;

      case AbsCodes.ABS_RZ: // Right stick Y - Envision reports this on ABS_RZ (non-standard!)
        inputState.RightStickY = (Int16)axisValue;
        break;

      case AbsCodes.ABS_RY: // Right trigger - Envision reports this on ABS_RY (non-standard!)
        inputState.RightTrigger = (Int16)axisValue;
        break;

      case AbsCodes.ABS_HAT0X:
        inputState.DpadX = (SByte)axisValue;
        break;

      case AbsCodes.ABS_HAT0Y:
        inputState.DpadY = (SByte)axisValue;
        break;

      default:
        return;
    }
    inputState.MarkDirty();
  }

  private static void ProcessButton(UInt16 buttonCode, Boolean buttonIsPressed, InputState inputState)
  {
    switch (buttonCode)
    {
      case ButtonCodes.BTN_SOUTH: // A
        inputState.ButtonA = buttonIsPressed;
        break;

      case ButtonCodes.BTN_EAST: // B
        inputState.ButtonB = buttonIsPressed;
        break;

      case ButtonCodes.BTN_NORTH: // Y on 3a08 wireless receiver
        inputState.ButtonY = buttonIsPressed;
        break;

      case ButtonCodes.BTN_C: // X on 3a08 wireless receiver
        inputState.ButtonX = buttonIsPressed;
        break;

      case ButtonCodes.BTN_WEST: // LB (normally X button position)
        inputState.BumperLeft = buttonIsPressed;
        break;

      case ButtonCodes.BTN_Z: // RB (non-standard!)
        inputState.BumperRight = buttonIsPressed;
        break;

      case ButtonCodes.BTN_TL2: // Left stick click (L3)
        inputState.LeftStickClick = buttonIsPressed;
        break;

      case ButtonCodes.BTN_TR2: // Right stick click (R3)
        inputState.RightStickClick = buttonIsPressed;
        break;

      case ButtonCodes.BTN_TL: // Select on V2
        inputState.ButtonSelect = buttonIsPressed;
        break;

      case ButtonCodes.BTN_TR: // Start on V2
        inputState.ButtonStart = buttonIsPressed;
        break;

      case ButtonCodes.BTN_MODE:
        inputState.ButtonGuide = buttonIsPressed;
        break;

      #region  Legacy standard mappings (not used on V2)
      case ButtonCodes.BTN_THUMBL:
      case ButtonCodes.BTN_THUMBR:
        return;
      #endregion

      case ButtonCodes.BTN_TRIGGER_HAPPY1:
        inputState.Paddle1 = buttonIsPressed;
        break;

      case ButtonCodes.BTN_TRIGGER_HAPPY2:
        inputState.Paddle2 = buttonIsPressed;
        break;

      case ButtonCodes.BTN_TRIGGER_HAPPY3:
        inputState.Paddle3 = buttonIsPressed;
        break;

      case ButtonCodes.BTN_TRIGGER_HAPPY4:
        inputState.Paddle4 = buttonIsPressed;
        break;

      case ButtonCodes.BTN_TRIGGER_HAPPY5:
        inputState.ButtonLeftSAX = buttonIsPressed;
        break;

      case ButtonCodes.BTN_TRIGGER_HAPPY6:
        inputState.ButtonRightSAX = buttonIsPressed;
        break;

      case 0x13f: // Unknown Scuf button (code 319) Currently Ignored.
      default:
        return;
    }

    inputState.MarkDirty();
  }
}