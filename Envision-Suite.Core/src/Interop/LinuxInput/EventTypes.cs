namespace EnvisionSuite.Core.Interop.LinuxInput;

/// <summary>
///   Event type constants for Linux input events.
/// </summary>
public static class EventTypes
{
  /// <summary>Synchronization event - marks the end of a set of related events.</summary>
  public const UInt16 EV_SYN = 0x00;

  /// <summary>Key/button event - press, release, or repeat.</summary>
  public const UInt16 EV_KEY = 0x01;

  /// <summary>Absolute axis event - joystick position, trigger value, etc.</summary>
  public const UInt16 EV_ABS = 0x03;

  /// <summary>Force feedback event (rumble, vibration).</summary>
  public const UInt16 EV_FF = 0x15;
}