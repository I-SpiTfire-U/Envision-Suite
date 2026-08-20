using System.Runtime.InteropServices;
using EnvisionSuite.Core.Interop.LinuxInput;

namespace EnvisionSuite.Core.Interop.Uinput;

/// <summary>
///   Absolute axis setup structure for uinput.
///   Configures the range and characteristics of an axis on the virtual device.
/// </summary>
/// <remarks>
///   <para>
///     This maps to the kernel's <c>struct uinput_abs_setup</c> defined in <c>linux/uinput.h</c>.
///   </para>
///   <para>
///     The kernel structure has a 16-bit code followed by 2 bytes padding (for alignment),
///     then struct input_absinfo (24 bytes).
///     Expected size: 2 (code) + 2 (padding) + 24 (absinfo) = 28 bytes.
///   </para>
///   <para>
///     The ioctl UI_ABS_SETUP (0x401c5504) encodes size 0x1c = 28 bytes, confirming padding.
///   </para>
///   <para>
///     This is a readonly struct because it is only set once and passed to ioctl.
///   </para>
/// </remarks>
/// <remarks>
///   Creates a new absolute axis setup structure.
/// </remarks>
/// <param name="code">Axis code (ABS_X, ABS_Y, ABS_Z, etc.).</param>
/// <param name="absInfo">Axis parameters (min, max, fuzz, flat).</param>
[StructLayout(LayoutKind.Sequential)]
public readonly struct UinputAbsSetup
{
  public readonly UInt16 AxisCode;
  public readonly InputAbsInfo AxisParameters;
  public const Int32 StructureSize = 28;

  public UinputAbsSetup(UInt16 code, InputAbsInfo absInfo)
  {
    AxisCode = code;
    AxisParameters = absInfo;
  }

  public static void ValidateSize()
  {
    Int32 actualSize = Marshal.SizeOf<UinputAbsSetup>();
    Int32 axisCodeOffset = Marshal.OffsetOf<UinputAbsSetup>(nameof(AxisCode)).ToInt32();
    Int32 axisParametersOffset = Marshal.OffsetOf<UinputAbsSetup>(nameof(AxisParameters)).ToInt32();

    Boolean hasValidLayout = actualSize == StructureSize && axisCodeOffset == 0 && axisParametersOffset == 4;

    if (hasValidLayout)
    {
      return;
    }

    throw new InvalidOperationException($"""
    UinputAbsSetup layout mismatch.
    Expected size {StructureSize}, AxisCode offset 0, and
    AxisParameters offset 4. Actual size: {actualSize},
    AxisCode offset: {axisCodeOffset},
    AxisParameters offset: {axisParametersOffset}.
    """);
  }
}