using System.Runtime.InteropServices;
using EnvisionSuite.Core.Interop.LinuxInput;

namespace EnvisionSuite.Core.Interop.Uinput;

/// <summary>
///   Setup structure for creating a uinput virtual device.
/// </summary>
/// <remarks>
///   Maps to the kernel's <c>struct uinput_setup</c> defined in <c>linux/uinput.h</c>.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct UinputSetup
{
  public const Int32 MaximumNameSize = 80;

  public InputId DeviceId;
  public fixed Byte DeviceName[MaximumNameSize];
  public UInt32 MaximumForceFeedbackEffects;
}