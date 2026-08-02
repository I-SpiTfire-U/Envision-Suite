namespace EnvisionSuite.Core.Mapping;

public static class EnvisionHidMapping
{
  private const Byte InputReportId = 0x06;
  private const Int32 InputReportLength = 16;

  private const Int32 FaceButtonsIndex = 12;
  private const Int32 RearButtonsIndex = 14;
  private const Int32 RightSaxIndex = 15;

  private const Byte ButtonAMask = 0x01;
  private const Byte ButtonBMask = 0x02;

  private const Byte Paddle1Mask = 0x20;
  private const Byte Paddle2Mask = 0x10;
  private const Byte Paddle3Mask = 0x40;
  private const Byte Paddle4Mask = 0x08;
  private const Byte LeftSaxMask = 0x80;
  private const Byte RightSaxMask = 0x01;

  public static Boolean ProcessReport(ReadOnlySpan<Byte> report, InputState state)
  {
    if (report.Length < InputReportLength || report[0] != InputReportId)
    {
      return false;
    }

    Byte faceButtons = report[FaceButtonsIndex];
    Byte rearButtons = report[RearButtonsIndex];
    Byte rightSaxButtons = report[RightSaxIndex];

    Boolean paddle1 = (rearButtons & Paddle1Mask) != 0;
    Boolean paddle2 = (rearButtons & Paddle2Mask) != 0;
    Boolean paddle3 = (rearButtons & Paddle3Mask) != 0;
    Boolean paddle4 = (rearButtons & Paddle4Mask) != 0;
    Boolean sideLeft = (rearButtons & LeftSaxMask) != 0;
    Boolean sideRight = (rightSaxButtons & RightSaxMask) != 0;

    // The side buttons also repeat their configured A/B actions.
    // Mask those duplicates so each side button remains independent.
    Boolean buttonA = (faceButtons & ButtonAMask) != 0 && !sideLeft;
    Boolean buttonB = (faceButtons & ButtonBMask) != 0 && !sideRight;

    Boolean changed = state.ButtonA != buttonA
      || state.ButtonB != buttonB
      || state.Paddle1 != paddle1
      || state.Paddle2 != paddle2
      || state.Paddle3 != paddle3
      || state.Paddle4 != paddle4
      || state.ButtonLeftSAX != sideLeft
      || state.ButtonRightSAX != sideRight;

    if (!changed)
    {
      return false;
    }

    state.Paddle1 = paddle1;
    state.Paddle2 = paddle2;
    state.Paddle3 = paddle3;
    state.Paddle4 = paddle4;
    state.ButtonLeftSAX = sideLeft;
    state.ButtonRightSAX = sideRight;
    state.ButtonA = buttonA;
    state.ButtonB = buttonB;

    state.MarkDirty();

    return true;
  }
}