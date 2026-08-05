using System.Collections.Immutable;

namespace EnvisionSuite.Core.Discovery;

public sealed record DiscoveredDevices
{
  public required String EvdevDevicePath { get; init; }
  public String? HidrawDevicePath { get; init; }
  public required ImmutableArray<String> AdditionalEvdevPaths { get; init; }
}
