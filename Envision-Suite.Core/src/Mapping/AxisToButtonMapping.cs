namespace EnvisionSuite.Core.Mapping;

public sealed record AxisToButtonMapping(PhysicalAxis Source, VirtualButton Target, AxisDirection Direction, Int32 Threshold) : InputMapping;