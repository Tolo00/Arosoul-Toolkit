using UnityEngine;

namespace Arosoul.Essentials 
{
    public static class QuaternionExtensions {
        public static Quaternion ShortestRotation(this Quaternion current, Quaternion target) {
            return target * Quaternion.Inverse(current);
        }
    }
}
