using EnvisionSuite.Core.Interop;

namespace EnvisionSuite.Core.Input;

/// <summary>
///     Flags indicating which input sources have data available after polling.
/// </summary>
[Flags]
public enum PollResult
{
  None = 0,
  EvdevReady = 1,
  HidrawReady = 2,
  Error = 4,
  Timeout = 8
}

/// <summary>
///     Multiplexes input from evdev and hidraw devices using poll(2).
///     This allows the main loop to efficiently wait for input from multiple
///     sources without busy-waiting or using multiple threads.
/// </summary>
/// <remarks>
///     <para>
///         The poll timeout determines the maximum latency between input and response.
///         Lower timeouts provide more responsive input but use more CPU.
///     </para>
///     <para>
///         <b>Ownership:</b> This class does NOT own the file descriptors - they are owned
///         by the <see cref="EvdevReader" /> and <see cref="HidrawReader" /> instances passed
///         to the constructor. Do not dispose of this class's readers separately.
///     </para>
///     <para>
///         <b>Thread safety:</b> This class is not thread-safe. Use from a single thread only.
///     </para>
/// </remarks>
/// <remarks>
///     Creates a new input poller for the given devices.
/// </remarks>
/// <param name="evdev">The evdev reader (required).</param>
/// <param name="hidraw">The hidraw reader (optional, may be null).</param>
public sealed class InputPoller(EvdevReader evdev, HidrawReader? hidraw)
{
  private readonly Int32 _EvdevfileDescriptor = evdev.FileDescriptor;
  private readonly Boolean _HasHidraw = hidraw is not null;
  private readonly Int32 _HidrawfileDescriptor = hidraw?.FileDescriptor ?? -1;

  /// <summary>
  ///     Waits for input to be available on any of the polled devices.
  ///     Uses the poll(2) system call to efficiently multiplex multiple file descriptors.
  /// </summary>
  /// <param name="timeoutMs">
  ///     Maximum time to wait in milliseconds. Lower values provide more responsive
  ///     input (4ms ≈ 250Hz polling is recommended for gaming). A value of -1 would
  ///     wait indefinitely, but this is not recommended as it prevents clean shutdown.
  /// </param>
  /// <returns>
  ///     A <see cref="PollResult" /> indicating which devices have data available,
  ///     or if an error/timeout occurred.
  /// </returns>
  public unsafe PollResult Poll(Int32 timeoutMs)
  {
    Int32 fileDescriptorCount = _HasHidraw ? 2 : 1;
    PollfileDescriptor* fileDescriptors = stackalloc PollfileDescriptor[2];

    fileDescriptors[0] = new PollfileDescriptor
    {
      FileDescriptor = _EvdevfileDescriptor,
      Events = Libc.POLLIN,
      PreviousEvents = 0
    };

    if (_HasHidraw)
    {
      fileDescriptors[1] = new PollfileDescriptor
      {
        FileDescriptor = _HidrawfileDescriptor,
        Events = Libc.POLLIN,
        PreviousEvents = 0
      };
    }

    Int32 result;
    do
    {
      result = Libc.Poll(fileDescriptors, (UIntPtr)fileDescriptorCount, timeoutMs);
    } while (result < 0 && Libc.GetLastError() == Libc.EINTR);

    if (result < 0)
    {
      return PollResult.Error;
    }

    if (result == 0)
    {
      return PollResult.Timeout;
    }

    var pollResult = PollResult.None;

    if ((fileDescriptors[0].PreviousEvents & (Libc.POLLIN | Libc.POLLERR | Libc.POLLHUP)) != 0)
    {
      if ((fileDescriptors[0].PreviousEvents & Libc.POLLIN) != 0)
      {
        pollResult |= PollResult.EvdevReady;
      }

      if ((fileDescriptors[0].PreviousEvents & (Libc.POLLERR | Libc.POLLHUP)) != 0)
      {
        pollResult |= PollResult.Error;
      }
    }

    if (_HasHidraw && (fileDescriptors[1].PreviousEvents & (Libc.POLLIN | Libc.POLLERR | Libc.POLLHUP)) != 0)
    {
      if ((fileDescriptors[1].PreviousEvents & Libc.POLLIN) != 0)
      {
        pollResult |= PollResult.HidrawReady;
      }
    }

    // Don't set error for hidraw issues, it's optional
    return pollResult;
  }
}