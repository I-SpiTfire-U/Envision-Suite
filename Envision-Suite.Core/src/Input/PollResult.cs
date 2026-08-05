namespace EnvisionSuite.Core.Input;

[Flags]
public enum PollResult
{
  None = 0,
  EvdevReady = 1 << 0,
  HidrawReady = 1 << 1,
  Timeout = 1 << 2,
  EvdevDisconnected = 1 << 3,
  HidrawDisconnected = 1 << 4,
  Error = 1 << 5
}