using EnvisionSuite.Core.Input;
using EnvisionSuite.Core.Interop.LinuxInput;
using EnvisionSuite.Core.Mapping;
using EnvisionSuite.Core.Output;

namespace EnvisionSuite.Core.Services;

/// <summary>
///     Orchestrates the main input bridging loop.
///     Reads input from the physical Scuf controller, maps it to Xbox format,
///     applies filtering, and emits to the virtual Xbox controller.
/// </summary>
/// <remarks>
///     <para>
///         <b>Data flow pipeline:</b>
///     </para>
///     <code>
/// Physical Scuf Controller
///         ↓
/// EvdevReader (reads evdev events) / HidrawReader (reads HID reports)
///         ↓
/// InputPoller (multiplexes with poll(2), 4ms timeout)
///         ↓
/// EnvisionMapping (translates Scuf's non-standard mappings to standard Xbox)
///         ↓
/// InputFilter (applies deadzone and jitter filtering)
///         ↓
/// VirtualGamepad (emits to virtual Xbox Elite 2 via uinput)
///         ↓
/// Games see standard Xbox controller
/// </code>
///     <para>
///         The service runs a tight loop polling at ~250Hz (4ms timeout) for responsive input.
///         This provides sub-frame latency at 60fps while being CPU-efficient.
///     </para>
/// </remarks>
/// <remarks>
///     Creates a new bridge service.
/// </remarks>
/// <param name="evdevReader">The evdev reader for the physical controller.</param>
/// <param name="hidrawReader">Optional hidraw reader (currently unused for V2 hardware).</param>
/// <param name="virtualGamepad">The virtual Xbox controller to emit to.</param>
public sealed class BridgeService(EvdevReader evdevReader, HidrawReader? hidrawReader, VirtualGamepad virtualGamepad)
{
  private readonly EvdevReader _Evdev = evdevReader;
  private readonly InputFilter _Filter = new();
  private readonly InputState _FilteredState = new();
  private readonly HidrawReader? _Hidraw = hidrawReader;
  private readonly InputPoller _Poller = new(evdevReader, hidrawReader);
  private readonly InputState _RawInputState = new();
  private readonly VirtualGamepad _VirtualGamepad = virtualGamepad;

  /// <summary>
  ///     Runs the main bridging loop until cancellation is requested.
  /// </summary>
  /// <param name="cancellationToken">Token to signal shutdown (e.g., from Ctrl+C).</param>
  /// <remarks>
  ///     The loop polls for input at ~250Hz (4ms timeout) and processes events as they arrive.
  ///     On each iteration:
  ///     1. Poll for available data on evdev/hidraw
  ///     2. Read and process any evdev events through EnvisionMapping
  ///     3. If state changed, apply filtering and emit to virtual gamepad
  /// </remarks>
  public void Run(CancellationToken cancellationToken)
  {
    Console.WriteLine("Bridge service started. Press Ctrl+C to exit.");

    Span<InputEvent> eventBuffer = stackalloc InputEvent[64];
    Span<Byte> hidrawBuffer = stackalloc Byte[64];

    while (!cancellationToken.IsCancellationRequested)
    {
      PollResult pollResult = _Poller.Poll(4);

      if (pollResult.HasFlag(PollResult.Error))
      {
        Console.Error.WriteLine("Poll error on evdev device");
        break;
      }

      if (pollResult.HasFlag(PollResult.HidrawReady) && _Hidraw is not null)
      {
        // Drain queued reports and keep the newest state.
        for (Int32 i = 0; i < 128; i++)
        {
          Int32 reportLength = _Hidraw.ReadReport(hidrawBuffer);

          if (reportLength == 0)
          {
            break;
          }

          if (reportLength < 0)
          {
            Console.Error.WriteLine("Failed to read HID report");
            break;
          }

          EnvisionHidMapping.ProcessReport(hidrawBuffer[..reportLength], _RawInputState);
        }
      }

      if (pollResult.HasFlag(PollResult.EvdevReady))
      {
        Int32 eventCount = _Evdev.ReadEvents(eventBuffer);

        for (Int32 i = 0; i < eventCount; i++)
        {
          InputEvent inputEvent = eventBuffer[i];

          // A and B are now read authoritatively from HID.
          // Ignoring their evdev copies prevents SAX buttons
          // from also producing unwanted A/B presses.
          if (_Hidraw is not null && inputEvent.Type == EventTypes.EV_KEY && (inputEvent.Code == ButtonCodes.BTN_SOUTH || inputEvent.Code == ButtonCodes.BTN_EAST))
          {
            continue;
          }

          Boolean paddlePressed = _RawInputState.Paddle1 || _RawInputState.Paddle2 || _RawInputState.Paddle3 || _RawInputState.Paddle4;

          if (paddlePressed && inputEvent.Type == EventTypes.EV_ABS && (inputEvent.Code == AbsCodes.ABS_HAT0X || inputEvent.Code == AbsCodes.ABS_HAT0Y))
          {
            continue;
          }

          EnvisionMapping.ProcessEvdevEvent(in inputEvent, _RawInputState);
        }
      }

      if (_RawInputState.IsDirty)
      {
        _Filter.Apply(_RawInputState, _FilteredState);
        _VirtualGamepad.EmitControllerState(_FilteredState);
        _RawInputState.ClearDirty();
      }
    }

    Console.WriteLine("Bridge service stopped.");
  }
}