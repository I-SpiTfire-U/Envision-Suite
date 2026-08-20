namespace EnvisionSuite.Core.Mapping;

public sealed record ButtonToAxisMapping(PhysicalButton Source, VirtualAxis Target, Int32 PressedValue) : InputMapping;