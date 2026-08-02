namespace EnvisionSuite.Core.Output;

public enum ExitCode
{
  Success = 0,
  Failure = 1,
  ControllerNotFound = 2,
  InputDeviceUnavailable = 3,
  VirtualGamepadUnavailable = 4,
  Interrupted = 130
}