using EnvisionSuite.Core.Interop;
using EnvisionSuite.Core.Interop.LinuxInput;
using EnvisionSuite.Core.Interop.Uinput;
using EnvisionSuite.Core.Mapping;

namespace EnvisionSuite.Core.Output;

public sealed class VirtualGamepad : IDisposable
{
  private const Int32 MaximumQueuedEvents = 20; // 8 axes + 11 buttons + 1 sync = 20 events
  private readonly InputEvent[] _InputEventBuffer = new InputEvent[MaximumQueuedEvents];
  private readonly Int32 _FileDescriptor;
  private Int32 _ConsecutiveWriteErrorCount;
  private Int32 _QueuedEventCount;
  private Boolean _Disposed;

  private VirtualGamepad(Int32 fileDescriptor) =>
    _FileDescriptor = fileDescriptor;

  public static VirtualGamepad? Create()
  {
    const String UinputPath = "/dev/uinput";
    Int32 fileDescriptor = Libc.Open(UinputPath, Libc.O_WRONLY | Libc.O_NONBLOCK);

    if (fileDescriptor < 0)
    {
      Int32 nativeErrorNumber = Libc.GetLastError();
      Console.Error.WriteLine($"""
      Failed to open {UinputPath}: {Libc.StrError(nativeErrorNumber)} (nativeErrorNumber={nativeErrorNumber})
      Make sure uinput module is loaded: sudo modprobe uinput
      Check permissions on /dev/uinput
      """);
      return null;
    }

    VirtualGamepad gamepad = new(fileDescriptor);
    
    if (gamepad.TryConfigureVirtualDevice())
    {
      return gamepad;
    }

    gamepad.Dispose();
    return null;
  }

  /// <summary>
  ///   Attempts to enable a specified event type.
  /// </summary>
  /// <returns>
  ///   <see langword="true"/> if the event is enabled successfully; otherwise, <see langword="false"/>.
  /// </returns>
  private Boolean TryEnableEventType(UInt16 eventType) =>
    Libc.Ioctl(_FileDescriptor, UinputIoctl.UI_SET_EVBIT, eventType) >= 0;

  /// <summary>
  ///   Attempts to enable a specified key or button.
  /// </summary>
  /// <returns>
  ///   <see langword="true"/> if the key is enabled successfully; otherwise, <see langword="false"/>.
  /// </returns>
  private Boolean TryEnableKey(UInt16 buttonCode) =>
    Libc.Ioctl(_FileDescriptor, UinputIoctl.UI_SET_KEYBIT, buttonCode) >= 0;

  /// <summary>
  ///   Attempts to create a new virtual device.
  /// </summary>
  /// <returns>
  ///   <see langword="true"/> if the virtual device is created successfully; otherwise, <see langword="false"/>.
  /// </returns>
  private unsafe Boolean TryCreateVirtualDevice() =>
    Libc.Ioctl(_FileDescriptor, UinputIoctl.UI_DEV_CREATE, null) >= 0;

  /// <summary>
  ///   Attempts to enable and configure a specified axis.
  /// </summary>
  /// <param name="axisCode">The absolute axis code to configure.</param>
  /// <param name="minimumAxisValue">The minimum possible axis value.</param>
  /// <param name="maximumAxisValue">The maximum possible axis value.</param>
  /// <param name="noiseFilterThreshold">The amount of axis noise to filter out.</param>
  /// <param name="flatZone">The flat zone around the axis center.</param>
  /// <returns>
  ///   <see langword="true"/> if the axis is enabled and configured successfully; otherwise, <see langword="false"/>.
  /// </returns>
  private unsafe Boolean TryEnableAndConfigureAxis(UInt16 axisCode, Int32 minimumAxisValue, Int32 maximumAxisValue, Int32 noiseFilterThreshold, Int32 flatZone)
  {
    if (Libc.Ioctl(_FileDescriptor, UinputIoctl.UI_SET_ABSBIT, axisCode) < 0)
    {
      return false;
    }

    UinputAbsSetup absSetup = new(axisCode, new InputAbsInfo(minimumAxisValue, maximumAxisValue, noiseFilterThreshold, flatZone));
    return Libc.Ioctl(_FileDescriptor, UinputIoctl.UI_ABS_SETUP, &absSetup) >= 0;
  }

  /// <summary>
  ///   Attempts to enable and configure all of the controller axes.
  /// </summary>
  /// <remarks>
  ///   Sticks use the full Int16 range with fuzz 16 and flat 128; triggers use 0–1023
  ///   with no filtering; D-pad axes use the three-state range -1–1.
  /// </remarks>
  /// <returns>
  ///   <see langword="true"/> if all axes are configured successfully; otherwise, <see langword="false"/>.
  /// </returns>
  private Boolean TryEnableAndConfigureAxes()
  {
    const Int16 StickMinimum = Int16.MinValue;
    const Int16 StickMaximum = Int16.MaxValue;
    const Int16 TriggerMinimum = 0;
    const Int16 TriggerMaximum = 1023;
    const SByte DpadMinimum = -1;
    const SByte DpadMaximum = 1;

    return TryEnableAndConfigureAxis(AbsCodes.ABS_X, StickMinimum, StickMaximum, 16, 128)
        && TryEnableAndConfigureAxis(AbsCodes.ABS_Y, StickMinimum, StickMaximum, 16, 128)
        && TryEnableAndConfigureAxis(AbsCodes.ABS_RX, StickMinimum, StickMaximum, 16, 128)
        && TryEnableAndConfigureAxis(AbsCodes.ABS_RY, StickMinimum, StickMaximum, 16, 128)
        && TryEnableAndConfigureAxis(AbsCodes.ABS_Z, TriggerMinimum, TriggerMaximum, 0, 0)
        && TryEnableAndConfigureAxis(AbsCodes.ABS_RZ, TriggerMinimum, TriggerMaximum, 0, 0)
        && TryEnableAndConfigureAxis(AbsCodes.ABS_HAT0X, DpadMinimum, DpadMaximum, 0, 0)
        && TryEnableAndConfigureAxis(AbsCodes.ABS_HAT0Y, DpadMinimum, DpadMaximum, 0, 0);
  }

  /// <summary>
  ///   Attempts to configure the device identification.
  /// </summary>
  /// <returns>
  ///   <see langword="true"/> if the device identification is configured successfully; otherwise, <see langword="false"/>.
  /// </returns>
  private unsafe Boolean TryConfigureDeviceIdentification()
  {
    const UInt16 XboxVendorId = 0x045e;
    const UInt16 XboxProductId = 0x0b12;
    const UInt16 DeviceVersion = 1;
    ReadOnlySpan<Byte> deviceName = "Xbox Elite 2 Virtual Controller"u8;
    Int32 deviceNameLength = Math.Min(deviceName.Length, UinputSetup.MaximumNameSize - 1);

    UinputSetup setup = new()
    {
      DeviceId = new InputId(BusType.BUS_USB, XboxVendorId, XboxProductId, DeviceVersion),
      MaximumForceFeedbackEffects = 0
    };

    for (Int32 i = 0; i < deviceNameLength; i++)
    {
      setup.DeviceName[i] = deviceName[i];
    }

    return Libc.Ioctl(_FileDescriptor, UinputIoctl.UI_DEV_SETUP, &setup) >= 0;
  }

  /// <summary>
  ///   Attempts to configure and create the virtual device with the required event types, buttons, axes, and identification.
  /// </summary>
  /// <returns>
  ///   <see langword="true"/> if the virtual device is configured and created successfully; otherwise, <see langword="false"/>.
  /// </returns>
  private Boolean TryConfigureVirtualDevice()
  {
    if (!TryEnableEventType(EventTypes.EV_KEY) || !TryEnableEventType(EventTypes.EV_ABS) || !TryEnableEventType(EventTypes.EV_SYN))
    {
      Console.Error.WriteLine("Failed to enable event types.");
      return false;
    }

    ReadOnlySpan<UInt16> buttons =
    [
      ButtonCodes.BTN_SOUTH,
      ButtonCodes.BTN_EAST,
      ButtonCodes.BTN_NORTH,
      ButtonCodes.BTN_WEST,
      ButtonCodes.BTN_TL,
      ButtonCodes.BTN_TR,
      ButtonCodes.BTN_SELECT,
      ButtonCodes.BTN_START,
      ButtonCodes.BTN_MODE,
      ButtonCodes.BTN_THUMBL,
      ButtonCodes.BTN_THUMBR,
    ];

    foreach (UInt16 buttonCode in buttons)
    {
      if (!TryEnableKey(buttonCode))
      {
        Console.Error.WriteLine($"[error] Failed to enable button 0x{buttonCode:x4}.");
        return false;
      }
    }

    if (!TryEnableAndConfigureAxes())
    {
      Console.Error.WriteLine("[error] Failed to enable and configure axes.");
      return false;
    }

    if (!TryConfigureDeviceIdentification())
    {
      Console.Error.WriteLine("[error] Failed to configure device identification.");
      return false;
    }

    if (!TryCreateVirtualDevice())
    {
      Console.Error.WriteLine("[error] Failed to create uinput virtual device.");
      return false;
    }

    return true;
  }

  private void QueueInputEvent(UInt16 eventType, UInt16 eventCode, Int32 eventValue)
  {
    if (_QueuedEventCount >= MaximumQueuedEvents)
    {
      return;
    }

    _InputEventBuffer[_QueuedEventCount++] = new InputEvent
    {
      TvSec = 0,
      TvUsec = 0,
      Type = eventType,
      Code = eventCode,
      Value = eventValue
    };
  }

  /// <summary>
  ///   Queues an axis event if the current axis value differs from the previous value.
  /// </summary>
  /// <param name="axisCode">The absolute axis code to queue.</param>
  /// <param name="targetAxis">The virtual axis whose value is compared.</param>
  /// <param name="currentInputState">The current controller input state.</param>
  /// <param name="previousInputState">The previous controller input state.</param>
  /// <param name="inputMapper">The mapper used to resolve virtual axis values.</param>
  private void QueueAxisIfValueChanged(UInt16 axisCode, VirtualAxis targetAxis, InputState currentInputState, InputState previousInputState, InputMapper inputMapper)
  {
    Int32 value = inputMapper.ResolveAxis(currentInputState, targetAxis);
    Int32 previousValue = inputMapper.ResolveAxis(previousInputState, targetAxis);

    if (value != previousValue)
    {
      QueueInputEvent(EventTypes.EV_ABS, axisCode, value);

      #if DEBUG
      Console.WriteLine($"[Virtual] {targetAxis} = {value}");
      #endif
    }
  }

  /// <summary>
  ///   Queues a button event if the current button value differs from the previous value.
  /// </summary>
  /// <param name="buttonCode">The button code to queue.</param>
  /// <param name="targetButton">The virtual button whose value is compared.</param>
  /// <param name="currentInputState">The current controller input state.</param>
  /// <param name="previousInputState">The previous controller input state.</param>
  /// <param name="inputMapper">The mapper used to resolve virtual button values.</param>
  private void QueueButtonIfStateChanged(UInt16 buttonCode, VirtualButton targetButton, InputState currentInputState, InputState previousInputState, InputMapper inputMapper)
  {
    Boolean isPressed = inputMapper.ResolveButton(currentInputState, targetButton);
    Boolean wasPressed = inputMapper.ResolveButton(previousInputState, targetButton);

    if (isPressed != wasPressed)
    {
      QueueInputEvent(EventTypes.EV_KEY, buttonCode, (Int16)(isPressed ? 1 : 0));

      #if DEBUG
      Console.WriteLine($"[Virtual] {targetButton} = {isPressed}");
      #endif
    }
  }

  /// <summary>
  ///   Attempts to flush all queued input events to the virtual device.
  /// </summary>
  private unsafe void FlushQueuedEvents()
  {
    if (_QueuedEventCount == 0)
    {
      return;
    }

    fixed (InputEvent* pointer = _InputEventBuffer)
    {
      UIntPtr bytesToWrite = (UIntPtr)(_QueuedEventCount * InputEvent.Size);
      IntPtr bytesWritten = Libc.Write(_FileDescriptor, pointer, bytesToWrite);

      _QueuedEventCount = 0;

      if (bytesWritten >= 0)
      {
        _ConsecutiveWriteErrorCount = 0;
        return;
      }

      _ConsecutiveWriteErrorCount++;

      if (_ConsecutiveWriteErrorCount == 1 || _ConsecutiveWriteErrorCount % 1000 == 0)
      {
        Int32 nativeErrorNumber = Libc.GetLastError();
        Console.Error.WriteLine($"[warning] Failed to write to uinput device: {Libc.StrError(nativeErrorNumber)} (nativeErrorNumber={nativeErrorNumber}, consecutive errors={_ConsecutiveWriteErrorCount}).");
      }
    }
  }

  /// <summary>
  ///   Emits mapped virtual controller events for inputs that changed between the current and previous states.
  /// </summary>
  /// <param name="currentInputState">The current controller input state.</param>
  /// <param name="previousInputState">The previous controller input state.</param>
  /// <param name="inputMapper">The mapper used to resolve virtual controller inputs.</param>
  public void EmitControllerStateChanges(InputState currentInputState, InputState previousInputState, InputMapper inputMapper)
  {
    QueueAxisIfValueChanged(AbsCodes.ABS_X, VirtualAxis.LeftStickX, currentInputState, previousInputState, inputMapper);
    QueueAxisIfValueChanged(AbsCodes.ABS_Y, VirtualAxis.LeftStickY, currentInputState, previousInputState, inputMapper);
    QueueAxisIfValueChanged(AbsCodes.ABS_RX, VirtualAxis.RightStickX, currentInputState, previousInputState, inputMapper);
    QueueAxisIfValueChanged(AbsCodes.ABS_RY, VirtualAxis.RightStickY, currentInputState, previousInputState, inputMapper);
    QueueAxisIfValueChanged(AbsCodes.ABS_Z, VirtualAxis.LeftTrigger, currentInputState, previousInputState, inputMapper);
    QueueAxisIfValueChanged(AbsCodes.ABS_RZ, VirtualAxis.RightTrigger, currentInputState, previousInputState, inputMapper);
    QueueAxisIfValueChanged(AbsCodes.ABS_HAT0X, VirtualAxis.DpadX, currentInputState, previousInputState, inputMapper);
    QueueAxisIfValueChanged(AbsCodes.ABS_HAT0Y, VirtualAxis.DpadY, currentInputState, previousInputState, inputMapper);
    QueueButtonIfStateChanged(ButtonCodes.BTN_SOUTH, VirtualButton.ButtonA, currentInputState, previousInputState, inputMapper);
    QueueButtonIfStateChanged(ButtonCodes.BTN_EAST, VirtualButton.ButtonB, currentInputState, previousInputState, inputMapper);
    QueueButtonIfStateChanged(ButtonCodes.BTN_NORTH, VirtualButton.ButtonX, currentInputState, previousInputState, inputMapper);
    QueueButtonIfStateChanged(ButtonCodes.BTN_WEST, VirtualButton.ButtonY, currentInputState, previousInputState, inputMapper);
    QueueButtonIfStateChanged(ButtonCodes.BTN_TL, VirtualButton.BumperLeft, currentInputState, previousInputState, inputMapper);
    QueueButtonIfStateChanged(ButtonCodes.BTN_TR, VirtualButton.BumperRight, currentInputState, previousInputState, inputMapper);
    QueueButtonIfStateChanged(ButtonCodes.BTN_SELECT, VirtualButton.ButtonSelect, currentInputState, previousInputState, inputMapper);
    QueueButtonIfStateChanged(ButtonCodes.BTN_START, VirtualButton.ButtonStart, currentInputState, previousInputState, inputMapper);
    QueueButtonIfStateChanged(ButtonCodes.BTN_MODE, VirtualButton.ButtonGuide, currentInputState, previousInputState, inputMapper);
    QueueButtonIfStateChanged(ButtonCodes.BTN_THUMBL, VirtualButton.LeftStickClick, currentInputState, previousInputState, inputMapper);
    QueueButtonIfStateChanged(ButtonCodes.BTN_THUMBR, VirtualButton.RightStickClick, currentInputState, previousInputState, inputMapper);

    QueueInputEvent(EventTypes.EV_SYN, SynCodes.SYN_REPORT, 0);
    FlushQueuedEvents();
  }

  public void Dispose()
  {
    ReleaseResources();
    GC.SuppressFinalize(this);
  }

  private void ReleaseResources()
  {
    if (_Disposed)
    {
      return;
    }

    _Disposed = true;

    unsafe
    {
      Libc.Ioctl(_FileDescriptor, UinputIoctl.UI_DEV_DESTROY, null);
    }
    
    Libc.Close(_FileDescriptor);
  }

  ~VirtualGamepad()
  {
    ReleaseResources();
  }
}