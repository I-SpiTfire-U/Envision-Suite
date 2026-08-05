using System.Runtime.InteropServices;


/// <summary>
///   Structure used by poll(2) to describe a file descriptor to monitor.
/// </summary>
/// <remarks>
///   This maps to the kernel's <c>struct pollfileDescriptor</c> defined in <c>poll.h</c>.
///   This struct remains mutable because the kernel writes to <see cref="ReturnedEvents" />.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct PollfileDescriptor
{
  public Int32 FileDescriptor;
  public Int16 Events;
  public Int16 ReturnedEvents;
}