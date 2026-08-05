using EnvisionSuite.Core.Interop;

namespace EnvisionSuite.Core.Input;

/// <summary>
///   Multiplexes input from evdev and hidraw devices using poll(2).
///   This allows the main loop to efficiently wait for input from multiple
///   sources without busy-waiting or using multiple threads.
/// </summary>
/// <remarks>
///   <para>
///     The poll timeout determines the maximum latency between input and response.
///     Lower timeouts provide more responsive input but use more CPU.
///   </para>
///   <para>
///     <b>Ownership:</b> This class does NOT own the file descriptors - they are owned
///     by the <see cref="EvdevReader" /> and <see cref="HidrawReader" /> instances passed
///     to the constructor. Do not dispose of this class's readers separately.
///   </para>
///   <para>
///     <b>Thread safety:</b> This class is not thread-safe. Use from a single thread only.
///   </para>
/// </remarks>
/// <remarks>
///   Creates a new input poller for the given devices.
/// </remarks>
/// <param name="evdevReader">The evdev reader (required).</param>
/// <param name="hidrawReader">The hidraw reader (optional, may be null).</param>
public sealed class InputPoller(EvdevReader evdevReader, HidrawReader? hidrawReader)
{
  private readonly Int32 _EvdevFileDescriptor = evdevReader.FileDescriptor;
  private readonly Boolean _HasHidraw = hidrawReader is not null;
  private readonly Int32 _HidrawFileDescriptor = hidrawReader?.FileDescriptor ?? -1;

  /// <summary>
  ///   Waits for input to be available on any of the polled devices.
  ///   Uses the poll(2) system call to efficiently multiplex multiple file descriptors.
  /// </summary>
  /// <param name="timeoutMs">
  ///   Maximum time to wait in milliseconds. Lower values provide more responsive
  ///   input (4ms ≈ 250Hz polling is recommended for gaming). A value of -1 would
  ///   wait indefinitely, but this is not recommended as it prevents clean shutdown.
  /// </param>
  /// <returns>
  ///   A <see cref="PollResult" /> indicating which devices have data available,
  ///   or if an error/timeout occurred.
  /// </returns>
  public unsafe PollResult Poll(Int32 timeoutMs)
  {
    Int32 fileDescriptorCount = _HasHidraw ? 2 : 1;
    PollfileDescriptor* fileDescriptors = stackalloc PollfileDescriptor[2];

    fileDescriptors[0] = new PollfileDescriptor
    {
      FileDescriptor = _EvdevFileDescriptor,
      Events = Libc.POLLIN,
      ReturnedEvents = 0
    };

    if (_HasHidraw)
    {
      fileDescriptors[1] = new PollfileDescriptor
      {
        FileDescriptor = _HidrawFileDescriptor,
        Events = Libc.POLLIN,
        ReturnedEvents = 0
      };
    }

    Int32 result;
    do
    {
      result = Libc.Poll(fileDescriptors, (UIntPtr)fileDescriptorCount, timeoutMs);
    }
    while (result < 0 && Libc.GetLastError() == Libc.EINTR);

    if (result < 0)
    {
      return PollResult.Error;
    }

    if (result == 0)
    {
      return PollResult.Timeout;
    }

    PollResult pollResult = ParseEvdevEvents(fileDescriptors[0].ReturnedEvents);

    if (_HasHidraw)
    {
      pollResult |= ParseHidrawEvents(fileDescriptors[1].ReturnedEvents);
    }

    return pollResult;
  }

  private static PollResult ParseEvdevEvents(Int16 returnedEvents)
  {
    PollResult pollResult = PollResult.None;

    if ((returnedEvents & Libc.POLLIN) != 0)
    {
      pollResult |= PollResult.EvdevReady;
    }

    if ((returnedEvents & (Libc.POLLHUP | Libc.POLLNVAL)) != 0)
    {
      pollResult |= PollResult.EvdevDisconnected;
    }
    else if ((returnedEvents & Libc.POLLERR) != 0)
    {
      pollResult |= PollResult.Error;
    }

    return pollResult;
  }

  private static PollResult ParseHidrawEvents(Int16 returnedEvents)
  {
    PollResult pollResult = PollResult.None;

    if ((returnedEvents & Libc.POLLIN) != 0)
    {
      pollResult |= PollResult.HidrawReady;
    }

    if ((returnedEvents & (Libc.POLLHUP | Libc.POLLNVAL)) != 0)
    {
      pollResult |= PollResult.HidrawDisconnected;
    }
    else if ((returnedEvents & Libc.POLLERR) != 0)
    {
      pollResult |= PollResult.Error;
    }

    return pollResult;
  }
}