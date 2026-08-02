using System.Runtime.InteropServices;

namespace EnvisionSuite.Core.Interop.LinuxInput;

/// <summary>
///   Absolute axis information structure.
///   Describes the range and characteristics of an absolute axis (joystick, trigger, etc.).
/// </summary>
/// <remarks>
///   This maps to the kernel's <c>struct input_absinfo</c> defined in <c>linux/input.h</c>.
///   This is a readonly struct because it is only set during axis configuration.
/// </remarks>
/// <remarks>
///   Creates a new absolute axis information structure.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public readonly struct InputAbsInfo
{
  public readonly Int32 CurrentAxisValue;
  public readonly Int32 MinimumAxisValue;
  public readonly Int32 MaximumAxisValue;

  /// <summary>Values within this range of the previous value are ignored. Used for hardware noise filtering.</summary>
  public readonly Int32 FuzzValue;

  /// <summary>Values within this range of center are reported as center. Used for hardware deadzone.</summary>
  public readonly Int32 FlatZone;

  /// <summary>Resolution in units per millimeter (optional).</summary>
  public readonly Int32 Resolution;

  public InputAbsInfo(Int32 minimumAxisValue, Int32 maximumAxisValue, Int32 fuzzValue = 0, Int32 flatZone = 0, Int32 currentAxisValue = 0, Int32 resolution = 0)
  {
    CurrentAxisValue = currentAxisValue;
    MinimumAxisValue = minimumAxisValue;
    MaximumAxisValue = maximumAxisValue;
    FuzzValue = fuzzValue;
    FlatZone = flatZone;
    Resolution = resolution;
  }
}