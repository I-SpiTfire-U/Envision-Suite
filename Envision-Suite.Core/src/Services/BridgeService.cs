using EnvisionSuite.Core.Input;
using EnvisionSuite.Core.Interop.LinuxInput;
using EnvisionSuite.Core.Mapping;
using EnvisionSuite.Core.Output;

namespace EnvisionSuite.Core.Services;

public sealed class BridgeService(EvdevReader evdevReader, HidrawReader? hidrawReader, VirtualGamepad virtualGamepad)
{
  private const Int32 MaximumReportsPerPoll = 128;

  private readonly EvdevReader _EvdevReader = evdevReader;
  private readonly InputFilter _InputFilter = new();
  private readonly InputState _FilteredState = new();
  private readonly HidrawReader? _HidrawReader = hidrawReader;
  private readonly InputPoller _InputPoller = new(evdevReader, hidrawReader);
  private readonly InputState _RawInputState = new();
  private readonly VirtualGamepad _VirtualGamepad = virtualGamepad;

  public void Run(CancellationToken cancellationToken)
  {
    Console.WriteLine("Bridge service started. Press Ctrl+C to exit.");

    Span<InputEvent> eventBuffer = stackalloc InputEvent[64];
    Span<Byte> hidrawBuffer = stackalloc Byte[64];

    while (!cancellationToken.IsCancellationRequested)
    {
      PollResult pollResult = _InputPoller.Poll(4);

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

      ProcessHidrawInput(pollResult, hidrawBuffer);
      ProcessEvdevInput(pollResult, eventBuffer);

      if (_RawInputState.IsDirty)
      {
        _InputFilter.Apply(_RawInputState, _FilteredState);
        _VirtualGamepad.EmitControllerState(_FilteredState);
        _RawInputState.ClearDirty();
      }
    }

    Console.WriteLine("Bridge service stopped.");
  }

  private void ProcessHidrawInput(PollResult pollResult, Span<Byte> hidrawBuffer)
  {
    if (!pollResult.HasFlag(PollResult.HidrawReady) || _HidrawReader is null)
    {
      return;
    }

    for (Int32 i = 0; i < MaximumReportsPerPoll; i++)
    {
      Int32 reportLength = _HidrawReader.ReadReport(hidrawBuffer);

      if (reportLength == 0)
      {
        break;
      }

      if (reportLength < 0)
      {
        Console.Error.WriteLine("[error] Failed to read HID report");
        break;
      }

      EnvisionHidMapping.ProcessReport(hidrawBuffer[..reportLength], _RawInputState);
    }
  }

  private void ProcessEvdevInput(PollResult pollResult, Span<InputEvent> eventBuffer)
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
      if (ShouldIgnoreDuplicateFaceButton(inputEvent) || ShouldIgnorePaddleDpadEvent(inputEvent))
      {
        continue;
      }

      EnvisionMapping.ProcessEvdevEvent(in inputEvent, _RawInputState);
    }
  }

  private Boolean ShouldIgnoreDuplicateFaceButton(InputEvent inputEvent) =>
    _HidrawReader is not null &&
    inputEvent.Type == EventTypes.EV_KEY &&
    (inputEvent.Code == ButtonCodes.BTN_SOUTH || inputEvent.Code == ButtonCodes.BTN_EAST);

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
}