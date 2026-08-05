namespace EnvisionSuite.Core.Mapping;

/// <summary>
///   Applies deadzone and jitter filtering to controller inputs.
///   This improves the user experience by eliminating stick drift, trigger noise,
///   and small unintentional movements.
/// </summary>
/// <remarks>
///   <para>
///     <b>Deadzone filtering</b>: Values within the deadzone are reported as zero/center.
///     This prevents stick drift when the controller is at rest. The deadzone is applied
///     as a radial (circular) deadzone for sticks, which feels more natural than
///     independent axis deadzones.
///   </para>
///   <para>
///     <b>Jitter filtering</b>: Small changes from the previous value are ignored.
///     This prevents noisy input from unstable analog hardware.
///   </para>
///   <para>
///     Default values are tuned for the Scuf Envision Pro V2:
///     - Stick deadzone: 3500 (~10.7% of range) - aggressive to eliminate drift
///     - Trigger deadzone: 10 (~1% of range) - minimal for responsive triggers
///     - Stick jitter threshold: 300 - filters small fluctuations
///     - Trigger jitter threshold: 20 (~2% of range) - low for responsive triggers
///   </para>
///   <para>
///     <b>Thread safety:</b> This class is NOT thread-safe. It maintains mutable state
///     (previous axis values) for jitter filtering. Use from a single thread only.
///   </para>
/// </remarks>
/// <remarks>
///   Creates a new input filter with configurable thresholds.
/// </remarks>
/// <param name="stickDeadzone">
///   Stick deadzone radius. Values within this distance from center (0) are reported as 0.
///   Default: 3500 (~10.7% of the -32768 to 32767 range).
/// </param>
/// <param name="triggerDeadzone">
///   Trigger deadzone. Values below this are reported as 0.
///   Default: 10 (~1% of the 0-1023 range).
/// </param>
/// <param name="jitterThreshold">
///   Minimum change required for stick values to update.
///   Changes smaller than this are ignored to filter noise.
///   Default: 300.
/// </param>
/// <param name="triggerJitterThreshold">
///   Minimum change required for trigger values to update.
///   Lower than stick threshold for more responsive trigger input.
///   Default: 20 (~2% of range).
/// </param>
public sealed class InputFilter(Int32 stickDeadzone = 3500, Int32 triggerDeadzone = 10, Int32 jitterThreshold = 300, Int32 triggerJitterThreshold = 20)
{
  private readonly Int32 _StickDeadzone = stickDeadzone;
  private readonly Int32 _TriggerDeadzone = triggerDeadzone;
  private readonly Int32 _JitterThreshold = jitterThreshold;
  private readonly Int32 _TriggerJitterThreshold = triggerJitterThreshold;

  private Int16 _PreviousLeftStickX;
  private Int16 _PreviousLeftStickY;
  private Int16 _PreviousRightStickX;
  private Int16 _PreviousRightStickY;
  private Int16 _PreviousLeftTrigger;
  private Int16 _PreviousRightTrigger;

  /// <summary>
  ///   Applies deadzone and jitter filtering to the input state.
  /// </summary>
  /// <param name="state">The raw input state from the controller.</param>
  /// <param name="output">The filtered output state to write to.</param>
  /// <remarks>
  ///   Button states are copied directly without filtering.
  ///   Analog values (sticks, triggers) are filtered through deadzone and jitter thresholds.
  ///   The output state is always marked as dirty after this call.
  /// </remarks>
  public void Apply(InputState state, InputState output)
  {
    output.ButtonA = state.ButtonA;
    output.ButtonB = state.ButtonB;
    output.ButtonX = state.ButtonX;
    output.ButtonY = state.ButtonY;
    output.BumperLeft = state.BumperLeft;
    output.BumperRight = state.BumperRight;
    output.ButtonStart = state.ButtonStart;
    output.ButtonSelect = state.ButtonSelect;
    output.ButtonGuide = state.ButtonGuide;
    output.ThumbLeft = state.ThumbLeft;
    output.ThumbRight = state.ThumbRight;
    output.Paddle1 = state.Paddle1;
    output.Paddle2 = state.Paddle2;
    output.Paddle3 = state.Paddle3;
    output.Paddle4 = state.Paddle4;
    output.ButtonLeftSAX = state.ButtonLeftSAX;
    output.ButtonRightSAX = state.ButtonRightSAX;
    output.DpadX = state.DpadX;
    output.DpadY = state.DpadY;

    FilterStickRadial(state.LeftStickX, state.LeftStickY, ref _PreviousLeftStickX, ref _PreviousLeftStickY, out Int16 leftX, out Int16 leftY);
    output.LeftStickX = leftX;
    output.LeftStickY = leftY;

    FilterStickRadial(state.RightStickX, state.RightStickY, ref _PreviousRightStickX, ref _PreviousRightStickY, out Int16 rightX, out Int16 rightY);
    output.RightStickX = rightX;
    output.RightStickY = rightY;

    output.LeftTrigger = FilterTrigger(state.LeftTrigger, ref _PreviousLeftTrigger);
    output.RightTrigger = FilterTrigger(state.RightTrigger, ref _PreviousRightTrigger);
    output.MarkDirty();
  }

  /// <summary>
  ///   Filters a stick using radial (circular) deadzone and jitter thresholds.
  /// </summary>
  /// <param name="x">The raw X axis value.</param>
  /// <param name="y">The raw Y axis value.</param>
  /// <param name="prevX">Reference to the previous filtered X value.</param>
  /// <param name="prevY">Reference to the previous filtered Y value.</param>
  /// <param name="outX">The filtered X axis value.</param>
  /// <param name="outY">The filtered Y axis value.</param>
  /// <remarks>
  ///   Radial deadzone calculates the distance from center using sqrt(x^2 + y^2).
  ///   This creates a circular deadzone that feels more natural than square deadzones.
  /// </remarks>
  private void FilterStickRadial(Int16 x, Int16 y, ref Int16 prevX, ref Int16 prevY, out Int16 outX, out Int16 outY)
  {
    Double magnitude = Math.Sqrt((Double)x * x + (Double)y * y);

    if (magnitude < _StickDeadzone)
    {
      prevX = 0;
      prevY = 0;
      outX = 0;
      outY = 0;
      return;
    }

    // Apply jitter filter for X
    if (Math.Abs(x - prevX) < _JitterThreshold)
    {
      outX = prevX;
    }
    else
    {
      prevX = x;
      outX = x;
    }

    // Apply jitter filter for Y
    if (Math.Abs(y - prevY) < _JitterThreshold)
    {
      outY = prevY;
    }
    else
    {
      prevY = y;
      outY = y;
    }
  }

  /// <summary>
  ///   Filters a trigger value through deadzone and jitter thresholds.
  /// </summary>
  /// <param name="value">The raw trigger value (0-1023).</param>
  /// <param name="previousValue">Reference to the previous filtered value (updated if value passes).</param>
  /// <returns>The filtered trigger value (0 if in deadzone, previous if jittering, otherwise the value).</returns>
  private Int16 FilterTrigger(Int16 value, ref Int16 previousValue)
  {
    if (value < _TriggerDeadzone)
    {
      previousValue = 0;
      return 0;
    }

    if (Math.Abs(value - previousValue) < _TriggerJitterThreshold)
    {
      return previousValue;
    }

    previousValue = value;
    return value;
  }

  public void Reset()
  {
    _PreviousLeftStickX = 0;
    _PreviousLeftStickY = 0;
    _PreviousRightStickX = 0;
    _PreviousRightStickY = 0;
    _PreviousLeftTrigger = 0;
    _PreviousRightTrigger = 0;
  }
}