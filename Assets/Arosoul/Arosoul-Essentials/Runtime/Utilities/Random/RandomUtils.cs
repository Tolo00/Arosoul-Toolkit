using UnityEngine;

namespace Arosoul.Essentials 
{
    public static class RandomUtils {

    #region Position
        /// <summary>
        /// Returns a random point inside a circle centered at (0,0).
        /// </summary>
        public static Vector2 PointInCircle(float radius) {
            return Random.insideUnitCircle * radius;
        }

        /// <summary>
        /// Returns a random point inside a circle centered at a given position.
        /// </summary>
        public static Vector2 PointInCircle(Vector2 center, float radius) {
            return center + PointInCircle(radius);
        }

        /// <summary>
        /// Returns a random point inside a 3D sphere centered at (0,0,0).
        /// </summary>
        public static Vector3 PointInSphere(float radius) {
            return Random.insideUnitSphere * radius;
        }

        /// <summary>
        /// Returns a random point inside a 3D sphere centered at a given position.
        /// </summary>
        public static Vector3 PointInSphere(Vector3 center, float radius) {
            return center + PointInSphere(radius);
        }

        /// <summary>
        /// Returns a random point inside a rectangle (2D).
        /// </summary>
        public static Vector2 PointInsideRect(Rect rect) {
            float x = Random.Range(rect.xMin, rect.xMax);
            float y = Random.Range(rect.yMin, rect.yMax);
            return new Vector2(x, y);
        }

        /// <summary>
        /// Returns a random point inside a volume (3D).
        /// </summary>
        public static Vector3 PointInsideBounds(Bounds bound) {
            float x = Random.Range(bound.min.x, bound.max.x);
            float y = Random.Range(bound.min.y, bound.max.y);
            float z = Random.Range(bound.min.z, bound.max.z);
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Returns a random point inside a volume (3D).
        /// </summary>
        public static Vector3 PointInsideBounds(Bounds bound, Vector3 offset) {
            float x = Random.Range(bound.min.x, bound.max.x);
            float y = Random.Range(bound.min.y, bound.max.y);
            float z = Random.Range(bound.min.z, bound.max.z);
            return new Vector3(x + offset.x, y + offset.y, z + offset.z);
        }
    #endregion

    #region Rotation
        /// <summary>
        /// Returns a random 2D rotation.
        /// </summary>
        public static Quaternion Rotation2D() {
            float angle = Random.Range(0f, 360f);
            return Quaternion.Euler(0f, 0f, angle);
        }

        /// <summary>
        /// Returns a random 3D rotation.
        /// </summary>
        public static Quaternion Rotation3D() {
            return Random.rotation;
        }
    #endregion

    #region Color
        /// <summary>
        /// Returns a random RGB color (full saturation & brightness).
        /// </summary>
        public static Color RandomColor() {
            return Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f);
        }

        /// <summary>
        /// Returns a random pastel color.
        /// </summary>
        public static Color RandomPastelColor() {
            return Random.ColorHSV(0f, 1f, 0.4f, 0.6f, 0.8f, 1f);
        }

        /// <summary>
        /// Returns a random grayscale color.
        /// </summary>
        public static Color RandomGray() {
            float value = Random.Range(0f, 1f);
            return new Color(value, value, value);
        }
    #endregion

    #region Numbers
        /// <summary>
        /// Returns -1 or +1 randomly.
        /// </summary>
        public static int Sign() {
            return Random.value < 0.5f ? -1 : 1;
        }

        /// <summary>
        /// Returns a random bool with equal chance.
        /// </summary>
        public static bool Bool() {
            return Random.value < 0.5f;
        }

        /// <summary>
        /// Returns true with the given probability (0–1 range).
        /// </summary>
        public static bool Chance(float probability) {
            return Random.value < Mathf.Clamp01(probability);
        }


    #endregion
    }
}