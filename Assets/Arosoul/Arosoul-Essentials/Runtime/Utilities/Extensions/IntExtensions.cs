using UnityEngine;

namespace Arosoul.Essentials
{
    public static class IntExtensions
    {
        /// <summary>
        /// Clamps the integer value between min and max
        /// </summary>
        public static int Clamp(this int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// Clamps the integer value between 0 and max
        /// </summary>
        public static int Clamp(this int value, int max)
        {
            return value.Clamp(0, max);
        }

        /// <summary>
        /// Maps a value from one range to another
        /// </summary>
        public static int Map(this int value, int fromMin, int fromMax, int toMin, int toMax)
        {
            return (value - fromMin) * (toMax - toMin) / (fromMax - fromMin) + toMin;
        }

        /// <summary>
        /// Maps a value from one range to another, returning a float for more precision
        /// </summary>
        public static float MapToFloat(this int value, int fromMin, int fromMax, float toMin, float toMax)
        {
            return (value - fromMin) * (toMax - toMin) / (fromMax - fromMin) + toMin;
        }

        /// <summary>
        /// Returns true if the value is between min and max (inclusive)
        /// </summary>
        public static bool IsBetween(this int value, int min, int max)
        {
            return value >= min && value <= max;
        }

        /// <summary>
        /// Returns the absolute value
        /// </summary>
        public static int Abs(this int value)
        {
            return Mathf.Abs(value);
        }

        /// <summary>
        /// Returns the sign of the value (-1, 0, or 1)
        /// </summary>
        public static int Sign(this int value)
        {
            if (value > 0) return 1;
            if (value < 0) return -1;
            return 0;
        }

        /// <summary>
        /// Wraps the value within a range (useful for circular arrays, angles, etc.)
        /// </summary>
        public static int Wrap(this int value, int min, int max)
        {
            int range = max - min + 1;
            if (value < min)
                return max - (min - value - 1) % range;
            return min + (value - min) % range;
        }

        /// <summary>
        /// Wraps the value between 0 and max
        /// </summary>
        public static int Wrap(this int value, int max)
        {
            return value.Wrap(0, max);
        }

        /// <summary>
        /// Returns true if the value is even
        /// </summary>
        public static bool IsEven(this int value)
        {
            return value % 2 == 0;
        }

        /// <summary>
        /// Returns true if the value is odd
        /// </summary>
        public static bool IsOdd(this int value)
        {
            return value % 2 != 0;
        }

        /// <summary>
        /// Returns the square of the value
        /// </summary>
        public static int Square(this int value)
        {
            return value * value;
        }

        /// <summary>
        /// Returns the value raised to the specified power
        /// </summary>
        public static int Pow(this int value, int exponent)
        {
            return (int)Mathf.Pow(value, exponent);
        }

        /// <summary>
        /// Returns the larger of this value and the other value
        /// </summary>
        public static int Max(this int value, int other)
        {
            return Mathf.Max(value, other);
        }

        /// <summary>
        /// Returns the smaller of this value and the other value
        /// </summary>
        public static int Min(this int value, int other)
        {
            return Mathf.Min(value, other);
        }

        /// <summary>
        /// Converts the integer to a normalized float value between 0 and 1
        /// </summary>
        public static float Normalize(this int value, int min, int max)
        {
            return Mathf.InverseLerp(min, max, value);
        }

        /// <summary>
        /// Repeats the value within a range (like Mathf.Repeat but for integers)
        /// </summary>
        public static int Repeat(this int value, int length)
        {
            return (int)Mathf.Repeat(value, length);
        }

        /// <summary>
        /// Ping-pongs the value between 0 and length
        /// </summary>
        public static int PingPong(this int value, int length)
        {
            return (int)Mathf.PingPong(value, length);
        }

        /// <summary>
        /// Returns a random integer between 0 and this value (exclusive)
        /// </summary>
        public static int Random(this int max)
        {
            return UnityEngine.Random.Range(0, max);
        }

        /// <summary>
        /// Returns a random integer between min and this value (exclusive)
        /// </summary>
        public static int Random(this int max, int min)
        {
            return UnityEngine.Random.Range(min, max);
        }

        /// <summary>
        /// Returns the percentage of this value relative to a total
        /// </summary>
        public static float PercentOf(this int value, int total)
        {
            if (total == 0) return 0f;
            return (value / (float)total) * 100f;
        }

        /// <summary>
        /// Calculates a percentage of this value
        /// </summary>
        public static int Percent(this int value, float percentage)
        {
            return Mathf.RoundToInt(value * (percentage / 100f));
        }

        /// <summary>
        /// Returns the value with the specified number of digits
        /// </summary>
        public static int ToDigits(this int value, int digits)
        {
            return (int)(value / Mathf.Pow(10, digits - 1));
        }

        /// <summary>
        /// Rounds to the nearest multiple of the specified value
        /// </summary>
        public static int RoundToNearest(this int value, int multiple)
        {
            if (multiple == 0) return value;
            return Mathf.RoundToInt(value / (float)multiple) * multiple;
        }

        /// <summary>
        /// Returns true if the value is a power of two
        /// </summary>
        public static bool IsPowerOfTwo(this int value)
        {
            return value > 0 && (value & (value - 1)) == 0;
        }

        /// <summary>
        /// Returns the next power of two
        /// </summary>
        public static int NextPowerOfTwo(this int value)
        {
            return Mathf.NextPowerOfTwo(value);
        }

        /// <summary>
        /// Returns the closest power of two
        /// </summary>
        public static int ClosestPowerOfTwo(this int value)
        {
            return Mathf.ClosestPowerOfTwo(value);
        }
    }
}