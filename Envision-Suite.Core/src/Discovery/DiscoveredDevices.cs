namespace EnvisionSuite.Core.Discovery;

public sealed class DiscoveredDevices
{
  public required String EvdevDevicePath { get; init; }
  public required String HidrawDevicePath { get; init; }
  public required IReadOnlyList<String> AdditionalEvdevPaths { get; init; }
}
