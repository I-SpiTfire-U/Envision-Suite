using System.Runtime.InteropServices;

namespace EnvisionSuite.Core.Interop.LinuxInput;

/// <summary>
///     Linux input event structure representing a single input event from evdev.
///     Each event contains a timestamp, type, code, and value.
/// </summary>
/// <remarks>
///     This maps to the kernel's <c>struct input_event</c> defined in <c>linux/input.h</c>.
///     The structure is 24 bytes on x64 Linux due to 64-bit time_t.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct InputEvent
{
  /// <summary>Seconds component of the event timestamp.</summary>
  public Int64 TvSec;

  /// <summary>Microseconds component of the event timestamp.</summary>
  public Int64 TvUsec;

  /// <summary>Event type (EV_KEY, EV_ABS, EV_SYN, etc.).</summary>
  public UInt16 Type;

  /// <summary>Event code (specific to the event type).</summary>
  public UInt16 Code;

  /// <summary>Event value (key state, axis position, etc.).</summary>
  public Int32 Value;

  /// <summary>Size of this structure in bytes (24 on x64 Linux).</summary>
  public const Int32 Size = 24;

  /// <summary>
  ///     Validates that the managed struct size matches the expected kernel struct size.
  ///     Call this at startup to detect potential marshalling issues.
  /// </summary>
  /// <exception cref="InvalidOperationException">Thrown if size mismatch is detected.</exception>
  public static void ValidateSize()
  {
    var actualSize = Marshal.SizeOf<InputEvent>();
    if (actualSize != Size)
    {
      throw new InvalidOperationException(
          $"InputEvent structure size mismatch: expected {Size} bytes, got {actualSize} bytes. " +
          "This indicates a marshalling issue with the kernel's struct input_event.");
    }
  }
}