using EnvisionSuite.Core.Interop;
using EnvisionSuite.Core.Interop.LinuxInput;
using EnvisionSuite.Core.Interop.Uinput;
using EnvisionSuite.Core.Mapping;

namespace EnvisionSuite.Core.Output;

public sealed class VirtualGamepad : IDisposable
{
  private const String UinputPath = "/dev/uinput";

  #region Xbox Elite 2 Controller identifiers
  private const UInt16 XboxVendorId = 0x045e;
  private const UInt16 XboxProductId = 0x0b12;
  #endregion

  private const Int16 StickMinimum = Int16.MinValue;
  private const Int16 StickMaximum = Int16.MaxValue;

  private const Int32 TriggerMinimum = 0;
  private const Int32 TriggerMaximum = 1023;

  private const Int32 DpadMinimum = -1;
  private const Int32 DpadMaximum = 1;
  private const Int32 MaximumEventsPerFrame = 26; // 8 axes + 15 buttons + 1 sync = 24 events

  private readonly InputEvent[] _InputEventBuffer = new InputEvent[MaximumEventsPerFrame];
  private readonly Int32 _FileDescriptor;
  private readonly InputState _PreviousInputState = new();
  private Int32 _ConsecutiveWriteErrorCount;
  private Int32 _QueuedEventCount;
  private Boolean _Disposed;

  private VirtualGamepad(Int32 fileDescriptor) =>
    _FileDescriptor = fileDescriptor;

  public void Dispose()
  {
    if (!_Disposed)
    {
      _Disposed = true;

      unsafe
      {
        Libc.Ioctl(_FileDescriptor, UinputIoctl.UI_DEV_DESTROY, null);
      }
      Libc.Close(_FileDescriptor);
    }
  }

  public static VirtualGamepad? Create()
  {
    Int32 fileDescriptor = Libc.Open(UinputPath, Libc.O_WRONLY | Libc.O_NONBLOCK);
    if (fileDescriptor < 0)
    {
      Int32 errno = Libc.GetLastError();
      Console.Error.WriteLine($"""
      Failed to open {UinputPath}: {Libc.StrError(errno)} (errno={errno})
      Make sure uinput module is loaded: sudo modprobe uinput
      Check permissions on /dev/uinput
      """);
      return null;
    }

    VirtualGamepad gamepad = new(fileDescriptor);
    if (gamepad.TrySetupVirtualDevice())
    {
      return gamepad;
    }

    gamepad.Dispose();
    return null;
  }

  private Boolean TrySetupVirtualDevice()
  {
    if (!EnableEventType(EventTypes.EV_KEY) || !EnableEventType(EventTypes.EV_ABS) || !EnableEventType(EventTypes.EV_SYN))
    {
      Console.Error.WriteLine("Failed to enable event types.");
      return false;
    }

    ReadOnlySpan<UInt16> buttons =
    [
      ButtonCodes.BTN_SOUTH,          // A
      ButtonCodes.BTN_EAST,           // B
      ButtonCodes.BTN_NORTH,          // Y
      ButtonCodes.BTN_WEST,           // X
      ButtonCodes.BTN_TL,             // LB
      ButtonCodes.BTN_TR,             // RB
      ButtonCodes.BTN_SELECT,         // Back/Select
      ButtonCodes.BTN_START,          // Start
      ButtonCodes.BTN_MODE,           // Guide
      ButtonCodes.BTN_THUMBL,         // L3
      ButtonCodes.BTN_THUMBR,         // R3
      ButtonCodes.BTN_TRIGGER_HAPPY1, // Paddle Top-Left
      ButtonCodes.BTN_TRIGGER_HAPPY2, // Paddle Top-Right
      ButtonCodes.BTN_TRIGGER_HAPPY3, // Paddle Bottom-Left
      ButtonCodes.BTN_TRIGGER_HAPPY4, // Paddle Bottom-Right
      ButtonCodes.BTN_TRIGGER_HAPPY5, // SAX Left
      ButtonCodes.BTN_TRIGGER_HAPPY6  // SAX Right
    ];

    foreach (UInt16 buttonCode in buttons)
    {
      if (!EnableKey(buttonCode))
      {
        Console.Error.WriteLine($"Failed to enable button 0x{buttonCode:x4}.");
        return false;
      }
    }

    /*
      Enable and configure axes
      Sticks: ±32768 with fuzz=16, flat=128 for hardware noise filtering
      Triggers: 0-1023 with no fuzz/flat for maximum responsiveness
      D-pad: -1 to 1 (3-state)
    */
    if (!EnableAndConfigureAxis(AbsCodes.ABS_X, StickMinimum, StickMaximum, 16, 128) ||
        !EnableAndConfigureAxis(AbsCodes.ABS_Y, StickMinimum, StickMaximum, 16, 128) ||
        !EnableAndConfigureAxis(AbsCodes.ABS_RX, StickMinimum, StickMaximum, 16, 128) ||
        !EnableAndConfigureAxis(AbsCodes.ABS_RY, StickMinimum, StickMaximum, 16, 128) ||
        !EnableAndConfigureAxis(AbsCodes.ABS_Z, TriggerMinimum, TriggerMaximum, 0, 0) ||
        !EnableAndConfigureAxis(AbsCodes.ABS_RZ, TriggerMinimum, TriggerMaximum, 0, 0) ||
        !EnableAndConfigureAxis(AbsCodes.ABS_HAT0X, DpadMinimum, DpadMaximum, 0, 0) ||
        !EnableAndConfigureAxis(AbsCodes.ABS_HAT0Y, DpadMinimum, DpadMaximum, 0, 0))
    {
      Console.Error.WriteLine("Failed to configure axes.");
      return false;
    }

    if (!SetupDeviceIdentification())
    {
      Console.Error.WriteLine("Failed to setup device info.");
      return false;
    }

    if (!CreateVirtualDevice())
    {
      Console.Error.WriteLine("Failed to create uinput device.");
      return false;
    }

    return true;
  }

  private Boolean EnableEventType(UInt16 type) =>
    Libc.Ioctl(_FileDescriptor, UinputIoctl.UI_SET_EVBIT, type) >= 0;

  private Boolean EnableKey(UInt16 code) =>
    Libc.Ioctl(_FileDescriptor, UinputIoctl.UI_SET_KEYBIT, code) >= 0;

  private unsafe Boolean EnableAndConfigureAxis(UInt16 axisCode, Int32 minimumAxisValue, Int32 maximumAxisValue, Int32 noiseFilterThreshold, Int32 flatZone)
  {
    if (Libc.Ioctl(_FileDescriptor, UinputIoctl.UI_SET_ABSBIT, axisCode) < 0)
    {
      return false;
    }

    UinputAbsSetup absSetup = new(axisCode, new InputAbsInfo(minimumAxisValue, maximumAxisValue, noiseFilterThreshold, flatZone));
    return Libc.Ioctl(_FileDescriptor, UinputIoctl.UI_ABS_SETUP, &absSetup) >= 0;
  }

  private unsafe Boolean SetupDeviceIdentification()
  {
    UinputSetup setup = new()
    {
      DeviceId = new InputId(BusType.BUS_USB, XboxVendorId, XboxProductId, 1),
      MaximumForceFeedbackEffects = 0
    };

    ReadOnlySpan<Byte> name = "Xbox Elite 2 Virtual Controller"u8;
    for (Int32 i = 0; i < Math.Min(name.Length, 79); i++)
    {
      setup.DeviceName[i] = name[i];
    }

    return Libc.Ioctl(_FileDescriptor, UinputIoctl.UI_DEV_SETUP, &setup) >= 0;
  }

  private unsafe Boolean CreateVirtualDevice() =>
    Libc.Ioctl(_FileDescriptor, UinputIoctl.UI_DEV_CREATE, null) >= 0;

  public void EmitControllerState(InputState filteredControllerState)
  {
    _QueuedEventCount = 0;

    QueueAxisIfValueChanged(AbsCodes.ABS_X, filteredControllerState.LeftStickX, _PreviousInputState.LeftStickX);
    QueueAxisIfValueChanged(AbsCodes.ABS_Y, filteredControllerState.LeftStickY, _PreviousInputState.LeftStickY);
    QueueAxisIfValueChanged(AbsCodes.ABS_RX, filteredControllerState.RightStickX, _PreviousInputState.RightStickX);
    QueueAxisIfValueChanged(AbsCodes.ABS_RY, filteredControllerState.RightStickY, _PreviousInputState.RightStickY);

    QueueAxisIfValueChanged(AbsCodes.ABS_Z, filteredControllerState.LeftTrigger, _PreviousInputState.LeftTrigger);
    QueueAxisIfValueChanged(AbsCodes.ABS_RZ, filteredControllerState.RightTrigger, _PreviousInputState.RightTrigger);
    
    QueueAxisIfValueChanged(AbsCodes.ABS_HAT0X, filteredControllerState.DpadX, _PreviousInputState.DpadX);
    QueueAxisIfValueChanged(AbsCodes.ABS_HAT0Y, filteredControllerState.DpadY, _PreviousInputState.DpadY);

    QueueButtonIfStateChanged(ButtonCodes.BTN_SOUTH, filteredControllerState.ButtonA, _PreviousInputState.ButtonA);
    QueueButtonIfStateChanged(ButtonCodes.BTN_EAST, filteredControllerState.ButtonB, _PreviousInputState.ButtonB);
    QueueButtonIfStateChanged(ButtonCodes.BTN_NORTH, filteredControllerState.ButtonY, _PreviousInputState.ButtonY);
    QueueButtonIfStateChanged(ButtonCodes.BTN_WEST, filteredControllerState.ButtonX, _PreviousInputState.ButtonX);

    QueueButtonIfStateChanged(ButtonCodes.BTN_TL, filteredControllerState.BumperLeft, _PreviousInputState.BumperLeft);
    QueueButtonIfStateChanged(ButtonCodes.BTN_TR, filteredControllerState.BumperRight, _PreviousInputState.BumperRight);

    QueueButtonIfStateChanged(ButtonCodes.BTN_SELECT, filteredControllerState.ButtonSelect, _PreviousInputState.ButtonSelect);
    QueueButtonIfStateChanged(ButtonCodes.BTN_START, filteredControllerState.ButtonStart, _PreviousInputState.ButtonStart);
    QueueButtonIfStateChanged(ButtonCodes.BTN_MODE, filteredControllerState.ButtonGuide, _PreviousInputState.ButtonGuide);

    QueueButtonIfStateChanged(ButtonCodes.BTN_THUMBL, filteredControllerState.ThumbLeft, _PreviousInputState.ThumbLeft);
    QueueButtonIfStateChanged(ButtonCodes.BTN_THUMBR, filteredControllerState.ThumbRight, _PreviousInputState.ThumbRight);

    QueueButtonIfStateChanged(ButtonCodes.BTN_TRIGGER_HAPPY1, filteredControllerState.Paddle1, _PreviousInputState.Paddle1);
    QueueButtonIfStateChanged(ButtonCodes.BTN_TRIGGER_HAPPY2, filteredControllerState.Paddle2, _PreviousInputState.Paddle2);
    QueueButtonIfStateChanged(ButtonCodes.BTN_TRIGGER_HAPPY3, filteredControllerState.Paddle3, _PreviousInputState.Paddle3);
    QueueButtonIfStateChanged(ButtonCodes.BTN_TRIGGER_HAPPY4, filteredControllerState.Paddle4, _PreviousInputState.Paddle4);
    QueueButtonIfStateChanged(ButtonCodes.BTN_TRIGGER_HAPPY5, filteredControllerState.ButtonLeftSAX, _PreviousInputState.ButtonLeftSAX);
    QueueButtonIfStateChanged(ButtonCodes.BTN_TRIGGER_HAPPY6, filteredControllerState.ButtonRightSAX, _PreviousInputState.ButtonRightSAX);

    QueueInputEvent(EventTypes.EV_SYN, SynCodes.SYN_REPORT, 0);
    FlushQueuedEvents();

    filteredControllerState.CopyTo(_PreviousInputState);
  }

  private void QueueAxisIfValueChanged(UInt16 code, Int32 value, Int32 previousValue)
  {
    if (value != previousValue)
    {
      QueueInputEvent(EventTypes.EV_ABS, code, value);
    }
  }

  private void QueueButtonIfStateChanged(UInt16 code, Boolean pressed, Boolean wasPressed)
  {
    if (pressed != wasPressed)
    {
      QueueInputEvent(EventTypes.EV_KEY, code, (Int16)(pressed ? 1 : 0));
    }
  }

  private void QueueInputEvent(UInt16 type, UInt16 code, Int32 value)
  {
    if (_QueuedEventCount >= MaximumEventsPerFrame)
    {
      return;
    }

    _InputEventBuffer[_QueuedEventCount++] = new InputEvent
    {
      TvSec = 0,
      TvUsec = 0,
      Type = type,
      Code = code,
      Value = value
    };
  }

  private unsafe void FlushQueuedEvents()
  {
    if (_QueuedEventCount == 0)
    {
      return;
    }

    fixed (InputEvent* ptr = _InputEventBuffer)
    {
      UIntPtr bytesToWrite = (UIntPtr)(_QueuedEventCount * InputEvent.Size);
      IntPtr bytesWritten = Libc.Write(_FileDescriptor, ptr, bytesToWrite);

      if (bytesWritten >= 0)
      {
        _ConsecutiveWriteErrorCount = 0;
        return;
      }

      _ConsecutiveWriteErrorCount++;
      if (_ConsecutiveWriteErrorCount == 1 || _ConsecutiveWriteErrorCount % 1000 == 0)
      {
        Int32 errno = Libc.GetLastError();
        Console.Error.WriteLine($"[warning] Failed to write to uinput device: {Libc.StrError(errno)} (errno={errno}, consecutive errors={_ConsecutiveWriteErrorCount}).");
      }
    }
  }
}