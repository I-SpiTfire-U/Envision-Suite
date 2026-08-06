namespace EnvisionSuite.Core.Mapping;

public sealed record ButtonMapping(PhysicalButton Source, VirtualButton Target) : InputMapping;