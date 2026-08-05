namespace EnvisionSuite.Core.Interop.LinuxInput;

/// <summary>
///   Synchronization event codes.
/// </summary>
public static class SynCodes
{
  /// <summary>
  ///   Marks the end of a related set of events.
  ///   All events between SYN_REPORT events should be processed together.
  /// </summary>
  public const UInt16 SYN_REPORT = 0x00;
}