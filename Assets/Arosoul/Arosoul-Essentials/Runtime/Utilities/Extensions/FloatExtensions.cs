using UnityEngine;

namespace Arosoul.Essentials
{
    public static class FloatExtensions
    {
        /// <summary>
        /// Clamps the float value between min and max
        /// </summary>
        public static float Clamp(this float value, float min, float max)
        {
            return Mathf.Clamp(value, min, max);
        }

        /// <summary>
        /// Clamps the float value between 0 and max
        /// </summary>
        public static float Clamp(this float value, float max)
        {
            return Mathf.Clamp(value, 0f, max);
        }

        /// <summary>
        /// Clamps the float value between 0 and 1
        /// </summary>
        public static float Clamp01(this float value)
        {
            return Mathf.Clamp01(value);
        }

        /// <summary>
        /// Maps a value from one range to another
        /// </summary>
        public static float Map(this float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return (value - fromMin) * (toMax - toMin) / (fromMax - fromMin) + toMin;
        }

        /// <summary>
        /// Maps a value from one range to 0-1
        /// </summary>
        public static float MapTo01(this float value, float min, float max)
        {
            return value.Map(min, max, 0f, 1f);
        }

        /// <summary>
        /// Returns true if the value is between min and max (inclusive)
        /// </summary>
        public static bool IsBetween(this float value, float min, float max)
        {
            return value >= min && value <= max;
        }

        /// <summary>
        /// Returns the absolute value
        /// </summary>
        public static float Abs(this float value)
        {
            return Mathf.Abs(value);
        }

        /// <summary>
        /// Returns the sign of the value (-1, 0, or 1)
        /// </summary>
        public static float Sign(this float value)
        {
            return Mathf.Sign(value);
        }

        /// <summary>
        /// Wraps the value within a range
        /// </summary>
        public static float Wrap(this float value, float min, float max)
        {
            float range = max - min;
            if (value < min)
                return max - Mathf.Repeat(min - value, range);
            return min + Mathf.Repeat(value - min, range);
        }

        /// <summary>
        /// Wraps the value between 0 and max
        /// </summary>
        public static float Wrap(this float value, float max)
        {
            return value.Wrap(0f, max);
        }

        /// <summary>
        /// Returns the square of the value
        /// </summary>
        public static float Square(this float value)
        {
            return value * value;
        }

        /// <summary>
        /// Returns the square root of the value
        /// </summary>
        public static float Sqrt(this float value)
        {
            return Mathf.Sqrt(value);
        }

        /// <summary>
        /// Returns the value raised to the specified power
        /// </summary>
        public static float Pow(this float value, float exponent)
        {
            return Mathf.Pow(value, exponent);
        }

        /// <summary>
        /// Returns the larger of this value and the other value
        /// </summary>
        public static float Max(this float value, float other)
        {
            return Mathf.Max(value, other);
        }

        /// <summary>
        /// Returns the smaller of this value and the other value
        /// </summary>
        public static float Min(this float value, float other)
        {
            return Mathf.Min(value, other);
        }

        /// <summary>
        /// Repeats the value within a range (wraps at 0 and length)
        /// </summary>
        public static float Repeat(this float value, float length)
        {
            return Mathf.Repeat(value, length);
        }

        /// <summary>
        /// Ping-pongs the value between 0 and length
        /// </summary>
        public static float PingPong(this float value, float length)
        {
            return Mathf.PingPong(value, length);
        }

        /// <summary>
        /// Returns a random float between 0 and this value
        /// </summary>
        public static float Random(this float max)
        {
            return UnityEngine.Random.Range(0f, max);
        }

        /// <summary>
        /// Returns a random float between min and this value
        /// </summary>
        public static float Random(this float max, float min)
        {
            return UnityEngine.Random.Range(min, max);
        }

        /// <summary>
        /// Returns the percentage of this value relative to a total
        /// </summary>
        public static float PercentOf(this float value, float total)
        {
            if (Mathf.Approximately(total, 0f)) return 0f;
            return (value / total) * 100f;
        }

        /// <summary>
        /// Calculates a percentage of this value
        /// </summary>
        public static float Percent(this float value, float percentage)
        {
            return value * (percentage / 100f);
        }

        /// <summary>
        /// Rounds the value to the nearest integer
        /// </summary>
        public static float Round(this float value)
        {
            return Mathf.Round(value);
        }

        /// <summary>
        /// Rounds the value to the specified number of decimal places
        /// </summary>
        public static float Round(this float value, int decimalPlaces)
        {
            float multiplier = Mathf.Pow(10f, decimalPlaces);
            return Mathf.Round(value * multiplier) / multiplier;
        }

        /// <summary>
        /// Rounds the value to the nearest multiple of the specified value
        /// </summary>
        public static float RoundToNearest(this float value, float multiple)
        {
            if (Mathf.Approximately(multiple, 0f)) return value;
            return Mathf.Round(value / multiple) * multiple;
        }

        /// <summary>
        /// Floors the value to the largest integer less than or equal to it
        /// </summary>
        public static float Floor(this float value)
        {
            return Mathf.Floor(value);
        }

        /// <summary>
        /// Ceils the value to the smallest integer greater than or equal to it
        /// </summary>
        public static float Ceil(this float value)
        {
            return Mathf.Ceil(value);
        }

        /// <summary>
        /// Converts to int using Floor
        /// </summary>
        public static int ToInt(this float value)
        {
            return Mathf.FloorToInt(value);
        }

        /// <summary>
        /// Converts to int using Round
        /// </summary>
        public static int ToIntRounded(this float value)
        {
            return Mathf.RoundToInt(value);
        }

        /// <summary>
        /// Converts to int using Ceil
        /// </summary>
        public static int ToIntCeiled(this float value)
        {
            return Mathf.CeilToInt(value);
        }

        /// <summary>
        /// Returns true if the value is approximately equal to another value
        /// </summary>
        public static bool Approximately(this float value, float other)
        {
            return Mathf.Approximately(value, other);
        }

        /// <summary>
        /// Returns true if the value is approximately zero
        /// </summary>
        public static bool ApproximatelyZero(this float value)
        {
            return Mathf.Approximately(value, 0f);
        }

        /// <summary>
        /// Linearly interpolates to a target value
        /// </summary>
        public static float Lerp(this float value, float target, float t)
        {
            return Mathf.Lerp(value, target, t);
        }

        /// <summary>
        /// Linearly interpolates to a target value without clamping t
        /// </summary>
        public static float LerpUnclamped(this float value, float target, float t)
        {
            return Mathf.LerpUnclamped(value, target, t);
        }

        /// <summary>
        /// Inverse lerp - returns where value is between a and b (0-1)
        /// </summary>
        public static float InverseLerp(this float value, float a, float b)
        {
            return Mathf.InverseLerp(a, b, value);
        }

        /// <summary>
        /// Smoothly interpolates using sine
        /// </summary>
        public static float SmoothStep(this float value, float from, float to)
        {
            return Mathf.SmoothStep(from, to, value);
        }

        /// <summary>
        /// Moves current toward target with a maximum change of maxDelta
        /// </summary>
        public static float MoveTowards(this float value, float target, float maxDelta)
        {
            return Mathf.MoveTowards(value, target, maxDelta);
        }

        /// <summary>
        /// Returns the sine of the value (in radians)
        /// </summary>
        public static float Sin(this float value)
        {
            return Mathf.Sin(value);
        }

        /// <summary>
        /// Returns the cosine of the value (in radians)
        /// </summary>
        public static float Cos(this float value)
        {
            return Mathf.Cos(value);
        }

        /// <summary>
        /// Returns the tangent of the value (in radians)
        /// </summary>
        public static float Tan(this float value)
        {
            return Mathf.Tan(value);
        }

        /// <summary>
        /// Converts degrees to radians
        /// </summary>
        public static float ToRadians(this float degrees)
        {
            return degrees * Mathf.Deg2Rad;
        }

        /// <summary>
        /// Converts radians to degrees
        /// </summary>
        public static float ToDegrees(this float radians)
        {
            return radians * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Normalizes an angle to be between 0 and 360
        /// </summary>
        public static float NormalizeAngle(this float angle)
        {
            angle = angle % 360f;
            if (angle < 0f)
                angle += 360f;
            return angle;
        }

        /// <summary>
        /// Normalizes an angle to be between -180 and 180
        /// </summary>
        public static float NormalizeAngleSigned(this float angle)
        {
            angle = angle.NormalizeAngle();
            if (angle > 180f)
                angle -= 360f;
            return angle;
        }

        /// <summary>
        /// Returns the shortest angle between two angles
        /// </summary>
        public static float DeltaAngle(this float current, float target)
        {
            return Mathf.DeltaAngle(current, target);
        }

        /// <summary>
        /// Smoothly damps toward a target value
        /// </summary>
        public static float SmoothDamp(this float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed = Mathf.Infinity, float deltaTime = -1f)
        {
            if (deltaTime < 0f)
                deltaTime = Time.deltaTime;
            return Mathf.SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
        }

        /// <summary>
        /// Returns an exponential easing curve
        /// </summary>
        public static float EaseInExpo(this float t)
        {
            return t == 0f ? 0f : Mathf.Pow(2f, 10f * (t - 1f));
        }

        /// <summary>
        /// Returns an exponential easing out curve
        /// </summary>
        public static float EaseOutExpo(this float t)
        {
            return t == 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
        }

        /// <summary>
        /// Returns a quadratic easing in curve
        /// </summary>
        public static float EaseInQuad(this float t)
        {
            return t * t;
        }

        /// <summary>
        /// Returns a quadratic easing out curve
        /// </summary>
        public static float EaseOutQuad(this float t)
        {
            return t * (2f - t);
        }

        /// <summary>
        /// Returns a cubic easing in curve
        /// </summary>
        public static float EaseInCubic(this float t)
        {
            return t * t * t;
        }

        /// <summary>
        /// Returns a cubic easing out curve
        /// </summary>
        public static float EaseOutCubic(this float t)
        {
            return (--t) * t * t + 1f;
        }

        /// <summary>
        /// Returns true if the value is positive
        /// </summary>
        public static bool IsPositive(this float value)
        {
            return value > 0f;
        }

        /// <summary>
        /// Returns true if the value is negative
        /// </summary>
        public static bool IsNegative(this float value)
        {
            return value < 0f;
        }

        /// <summary>
        /// Returns the value with inverted sign
        /// </summary>
        public static float Negate(this float value)
        {
            return -value;
        }

        /// <summary>
        /// Snaps the value to a grid
        /// </summary>
        public static float SnapToGrid(this float value, float gridSize)
        {
            return Mathf.Round(value / gridSize) * gridSize;
        }

        /// <summary>
        /// Returns the fractional part of the value
        /// </summary>
        public static float Frac(this float value)
        {
            return value - Mathf.Floor(value);
        }

        /// <summary>
        /// Oscillates between 0 and 1 using sine wave
        /// </summary>
        public static float Oscillate(this float time, float frequency = 1f)
        {
            return (Mathf.Sin(time * frequency * Mathf.PI * 2f) + 1f) * 0.5f;
        }

        /// <summary>
        /// Remaps from 0-1 to a bell curve centered at 0.5
        /// </summary>
        public static float Bell(this float t)
        {
            return Mathf.Sin(t * Mathf.PI);
        }
    }
}