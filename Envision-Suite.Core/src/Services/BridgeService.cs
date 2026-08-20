using EnvisionSuite.Core.Input;
using EnvisionSuite.Core.Interop.LinuxInput;
using EnvisionSuite.Core.Mapping;
using EnvisionSuite.Core.Output;

namespace EnvisionSuite.Core.Services;

public sealed class BridgeService(EvdevReader evdevReader, HidrawReader? hidrawReader, VirtualGamepad virtualGamepad)
{
  private readonly EvdevReader _EvdevReader = evdevReader;
  private readonly HidrawReader? _HidrawReader = hidrawReader;
  private readonly VirtualGamepad _VirtualGamepad = virtualGamepad;
  private readonly InputState _CurrentInputState = new();
  private readonly InputState _PreviousInputState = new();
  private readonly InputState _RawInputState = new();

  /// <summary>
  ///   Processes the hidraw buffer after the hidraw is confirmed ready.
  /// </summary>
  /// <param name="pollResult">The poll used to check the hidraw status.</param>
  /// <param name="hidrawBuffer">The hidraw buffer to process.</param>
  private void ProcessHidrawBuffer(PollResult pollResult, Span<Byte> hidrawBuffer)
  {
    const Int32 MaximumReportsPerPoll = 128;

    if (!pollResult.HasFlag(PollResult.HidrawReady) || _HidrawReader is null)
    {
      return;
    }

    for (Int32 i = 0; i < MaximumReportsPerPoll; i++)
    {
      Int32 reportLength = _HidrawReader.ReadReport(hidrawBuffer);

      if (reportLength > 0)
      {
        EnvisionHidMapping.ProcessReport(hidrawBuffer[..reportLength], _RawInputState);
      }

      if (reportLength < 0)
      {
        Console.Error.WriteLine("[error] Failed to read HID report");
      }
    }
  }

  /// <summary>
  ///   Determines whether a duplicate face button input should be ignored.
  /// </summary>
  /// <param name="inputEvent">The input event to check.</param>
  /// <returns>
  ///   <see langword="true"/> if the duplicate input should be ignored; otherwise, <see langword="false"/>.
  /// </returns>
  private Boolean ShouldIgnoreDuplicateFaceButtonEvent(InputEvent inputEvent) =>
    _HidrawReader is not null &&
    inputEvent.Type == EventTypes.EV_KEY &&
    (inputEvent.Code == ButtonCodes.BTN_SOUTH || inputEvent.Code == ButtonCodes.BTN_EAST);

  /// <summary>
  ///   Determines whether a paddle dpad event should be ignored.
  /// </summary>
  /// <param name="inputEvent">The input event to check.</param>
  /// <returns>
  ///   <see langword="true"/> if the dpad input should be ignored; otherwise, <see langword="false"/>.
  /// </returns>
  private Boolean ShouldIgnorePaddleDpadEvent(InputEvent inputEvent)
  {
    Boolean anyPaddlePressed =
      _RawInputState.Paddle1 ||
      _RawInputState.Paddle2 ||
      _RawInputState.Paddle3 ||
      _RawInputState.Paddle4;

    Boolean isDpadEvent =
      inputEvent.Type == EventTypes.EV_ABS &&
      (inputEvent.Code == AbsCodes.ABS_HAT0X || inputEvent.Code == AbsCodes.ABS_HAT0Y);

    return anyPaddlePressed && isDpadEvent;
  }

  /// <summary>
  ///   Processes the event buffer after the evdev is confirmed ready.
  /// </summary>
  /// <param name="pollResult">The poll used to check the evdev status.</param>
  /// <param name="eventBuffer">The event buffer to process.</param>
  private void ProcessEvdevBuffer(PollResult pollResult, Span<InputEvent> eventBuffer)
  {
    if (!pollResult.HasFlag(PollResult.EvdevReady))
    {
      return;
    }

    Int32 eventCount = _EvdevReader.ReadEvents(eventBuffer);
    if (eventCount < 0)
    {
      Console.Error.WriteLine("[error] Failed to read evdev events");
      return;
    }

    for (Int32 i = 0; i < eventCount; i++)
    {
      InputEvent inputEvent = eventBuffer[i];
      if (!ShouldIgnoreDuplicateFaceButtonEvent(inputEvent) && !ShouldIgnorePaddleDpadEvent(inputEvent))
      {
        EnvisionMapping.ProcessEvdevEvent(in inputEvent, _RawInputState);
      }
    }
  }

  /// <summary>
  ///   Starts and continues to run the controller bridge until a cancellation is requested.
  /// </summary>
  /// <param name="cancellationToken">The provided cancellation token.</param>
  public void RunBridge(CancellationToken cancellationToken)
  {
    InputPoller inputPoller = new(_EvdevReader, _HidrawReader);
    InputMapper inputMapper = new();
    InputFilter inputFilter = new();
    Span<InputEvent> eventBuffer = stackalloc InputEvent[64];
    Span<Byte> hidrawBuffer = stackalloc Byte[64];

    while (!cancellationToken.IsCancellationRequested)
    {
      PollResult pollResult = inputPoller.Poll(4);

      if (pollResult.HasFlag(PollResult.EvdevDisconnected))
      {
        Console.WriteLine("Controller disconnected");
        break;
      }

      if (pollResult.HasFlag(PollResult.Error))
      {
        Console.Error.WriteLine("[error] Poll error on input device");
        break;
      }

      ProcessHidrawBuffer(pollResult, hidrawBuffer);
      ProcessEvdevBuffer(pollResult, eventBuffer);

      if (_RawInputState.IsDirty)
      {
        inputFilter.Apply(_RawInputState, _CurrentInputState);
        _VirtualGamepad.EmitControllerStateChanges(_CurrentInputState, _PreviousInputState, inputMapper);

        _CurrentInputState.CopyTo(_PreviousInputState);
        _RawInputState.ClearDirty();
      }
    }

    Console.WriteLine("Bridge service stopped.");
  }
}