using System;

namespace Arosoul.Essentials
{
    public static class ArrayExtensions {
        private static readonly Random random = new Random();

        /// <summary>
        /// Randomly rearranges the elements of the array in place.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="array">The array to shuffle.</param>
        public static void Shuffle<T>(this T[] array) {
            for (int i = array.Length - 1; i > 0; i--) {
                int j = random.Next(0, i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }

        /// <summary>
        /// Determines whether the array contains no elements.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="array">The array to check.</param>
        /// <returns><c>true</c> if the array is empty; otherwise, <c>false</c>.</returns>
        public static bool IsEmpty<T>(this T[] array) => array.Length == 0;

        /// <summary>
        /// Determines whether the array is <c>null</c> or contains no elements.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="array">The array to check.</param>
        /// <returns><c>true</c> if the array is <c>null</c> or empty; otherwise, <c>false</c>.</returns>
        public static bool IsNullOrEmpty<T>(this T[] array) => array == null || array.Length == 0;

        /// <summary>
        /// Returns a random element from the array.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="array">The array to select a random element from.</param>
        /// <returns>A randomly selected element from the array.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the array is empty.</exception>
        public static T GetRandom<T>(this T[] array) => array[random.Next(array.Length)];

        /// <summary>
        /// Swaps the elements at the specified indices in the array.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="array">The array whose elements will be swapped.</param>
        /// <param name="i">The index of the first element.</param>
        /// <param name="j">The index of the second element.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if either index is outside the bounds of the array.
        /// </exception>s
        public static void Swap<T>(this T[] array, int i, int j) {
            if (i < 0 || j < 0 || i >= array.Length || j >= array.Length) throw new ArgumentOutOfRangeException();
            (array[i], array[j]) = (array[j], array[i]);
        }

        /// <summary>
        /// Joins the elements of the array into a single string, separated by the specified delimiter.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="array">The array whose elements will be joined.</param>
        /// <param name="separator">The string to use as a separator. Default is ", ".</param>
        /// <returns>A string containing the concatenated elements of the array.</returns>
        public static string JoinToString<T>(this T[] array, string separator = ", ") => string.Join(separator, array);
    }
}