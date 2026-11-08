using System;
using System.Collections.Generic;

namespace Arosoul.Essentials
{
    public static class ListExtensions {
        private static readonly Random random = new Random();

        /// <summary>
        /// Randomly rearranges the elements of the list in place.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to shuffle.</param>
        public static void Shuffle<T>(this List<T> list) {
            for (int i = list.Count - 1; i > 0; i--) {
                int j = random.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>
        /// Determines whether the list contains no elements.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to check.</param>
        /// <returns><c>true</c> if the list is empty; otherwise, <c>false</c>.</returns>
        public static bool IsEmpty<T>(this List<T> list) => list.Count == 0;

        /// <summary>
        /// Determines whether the list is <c>null</c> or contains no elements.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to check.</param>
        /// <returns><c>true</c> if the list is <c>null</c> or empty; otherwise, <c>false</c>.</returns>
        public static bool IsNullOrEmpty<T>(this List<T> list) => list == null || list.Count == 0;

        /// <summary>
        /// Returns a random element from the list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to select a random element from.</param>
        /// <returns>A randomly selected element from the list.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the list is empty.</exception>
        public static T GetRandom<T>(this List<T> list) => list[random.Next(list.Count)];

        /// <summary>
        /// Swaps the elements at the specified indices in the list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list whose elements will be swapped.</param>
        /// <param name="i">The index of the first element.</param>
        /// <param name="j">The index of the second element.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if either index is outside the bounds of the list.
        /// </exception>
        public static void Swap<T>(this List<T> list, int i, int j) {
            if (i < 0 || j < 0 || i >= list.Count || j >= list.Count) throw new ArgumentOutOfRangeException();
            (list[i], list[j]) = (list[j], list[i]);
        }

        /// <summary>
        /// Removes duplicate elements from the list, preserving the original order.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to remove duplicates from.</param>
        public static void RemoveDuplicates<T>(this List<T> list) {
            var seen = new HashSet<T>();
            int i = 0;
            while (i < list.Count)
            {
                if (!seen.Add(list[i]))
                    list.RemoveAt(i);
                else
                    i++;
            }
        }

        /// <summary>
        /// Removes all <c>null</c> elements from the list.
        /// </summary>
        /// <typeparam name="T">The reference type of elements in the list.</typeparam>
        /// <param name="list">The list to remove null values from.</param>
        public static void RemoveNulls<T>(this List<T> list) where T : class {
            list.RemoveAll(item => item == null);
        }

        /// <summary>
        /// Filters a collection based on a predicate and returns a new list
        /// containing the elements that match the specified condition.
        /// </summary>
        /// <param name="source">The collection to filter.</param>
        /// <param name="predicate">The condition that each element is tested against.</param>
        /// <returns>A new list containing elements that satisfy the predicate.</returns>
        public static IList<T> Filter<T>(this IList<T> source, Predicate<T> predicate) {
            List<T> list = new List<T>();
            foreach (T item in source) {
                if (predicate(item)) {
                    list.Add(item);
                }
            }
            return list;
        }

        /// <summary>
        /// Joins the elements of the list into a single string, separated by the specified delimiter.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list whose elements will be joined.</param>
        /// <param name="separator">The string to use as a separator. Default is ", ".</param>
        /// <returns>A string containing the concatenated elements of the list.</returns>
        public static string JoinToString<T>(this List<T> list, string separator = ", ") => string.Join(separator, list);
    }
}