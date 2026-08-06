namespace EnvisionSuite.Core.Mapping;

public sealed record AxisMapping(PhysicalAxis Source, VirtualAxis Target, Boolean Invert) : InputMapping;