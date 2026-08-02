using System.Runtime.InteropServices;
using EnvisionSuite.Core.Interop.LinuxInput;

namespace EnvisionSuite.Core.Interop.Uinput;

/// <summary>
///     Setup structure for creating a uinput virtual device.
///     Contains device identification and force feedback configuration.
/// </summary>
/// <remarks>
///     This maps to the kernel's <c>struct uinput_setup</c> defined in <c>linux/uinput.h</c>.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct UinputSetup
{
  public InputId DeviceId;
  public fixed Byte DeviceName[80];
  public UInt32 MaximumForceFeedbackEffects;
}