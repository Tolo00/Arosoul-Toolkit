using System.Collections.Generic;

namespace Arosoul.Transitions.Editor
{
    /// <summary>
    /// Allocates stable integer ids. Existing valid ids are never changed.
    /// </summary>
    public static class StableIdAllocator
    {
        /// <summary>
        /// Takes the current ids (0 means unassigned) and returns ids where unassigned and duplicated entries
        /// received new unique ids. The first entry that uses an id keeps it.
        /// </summary>
        public static int[] Allocate(IReadOnlyList<int> current)
        {
            var result = new int[current.Count];
            var used = new HashSet<int>();
            int max = 0;

            for (int i = 0; i < current.Count; i++)
            {
                int id = current[i];
                if (id > max) max = id;
                if (id > 0 && used.Add(id))
                    result[i] = id;
            }

            int next = max + 1;
            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] != 0) continue;
                while (used.Contains(next)) next++;
                result[i] = next;
                used.Add(next);
                next++;
            }
            return result;
        }
    }
}
