using UnityEngine;

namespace Arosoul.Essentials {
    public static class RigidbodyExtensions {
        /// <summary>
        /// Changes the direction of the Rigidbody's velocity while maintaining its speed.
        /// </summary>
        /// <param name="rigidbody">The Rigidbody to change direction.</param>
        /// <param name="direction">The new direction for the Rigidbody.</param>
        /// <returns>The modified Rigidbody for method chaining.</returns>
        public static Rigidbody ChangeDirection(this Rigidbody rigidbody, Vector3 direction) {
            if (direction.sqrMagnitude == 0f) return rigidbody;
            direction.Normalize();
            
#if UNITY_6000_0_OR_NEWER
            rigidbody.linearVelocity = direction * rigidbody.linearVelocity.magnitude;
#else
            rigidbody.velocity = direction * rigidbody.velocity.magnitude;
#endif
            return rigidbody;
        }

        /// <summary>
        /// Stops the Rigidbody by setting its linear and angular velocities to zero.
        /// </summary>
        /// <param name="rigidbody">The Rigidbody to stop.</param>
        /// <returns>The modified Rigidbody for method chaining.</returns>
        public static Rigidbody Stop(this Rigidbody rigidbody) {
#if UNITY_6000_0_OR_NEWER
            rigidbody.linearVelocity = Vector3.zero;
#else
            rigidbody.velocity = Vector3.zero;
#endif
            rigidbody.angularVelocity = Vector3.zero;
            return rigidbody;
        }

        /// <summary>
        /// Applies a spring-damper rotational force to the Rigidbody, smoothly driving it toward a target
        /// world-space rotation. A spring torque proportional to the angular difference accelerates the
        /// body toward the target rotation, while a damping torque opposing angular velocity prevents
        /// overshooting and oscillation.
        /// </summary>
        /// <param name="rigidbody">The Rigidbody to apply rotational forces to.</param>
        /// <param name="targetRotation">The desired world-space rotation to rotate toward.</param>
        /// <param name="springStrength">How strongly the body is pulled toward the target rotation. Higher values result in faster, more aggressive correction.</param>
        /// <param name="damping">How strongly angular velocity is resisted. Higher values reduce overshoot and oscillation.</param>
        public static void RotateSpring(this Rigidbody rigidbody, Quaternion targetRotation, float springStrength, float damping)
        {
            // Compute shortest rotation from current to target
            Quaternion rotationError = targetRotation * Quaternion.Inverse(rigidbody.rotation);

            rotationError.ToAngleAxis(out float angleInDegrees, out Vector3 rotationAxis);

            // Convert to shortest path
            if (angleInDegrees > 180f)
                angleInDegrees -= 360f;

            if (rotationAxis.sqrMagnitude < 0.0001f)
                return;

            Vector3 springTorque = rotationAxis.normalized * (angleInDegrees * Mathf.Deg2Rad * springStrength);
            Vector3 dampingTorque = -rigidbody.angularVelocity * damping;

            rigidbody.AddTorque(springTorque + dampingTorque, ForceMode.Acceleration);
        }

        /// <summary>
        /// Applies a spring-damper rotational force to the Rigidbody, smoothly driving it to face a
        /// given world-space direction. The direction is converted into a target rotation, then a spring
        /// torque proportional to the angular difference accelerates the body toward it, while damping
        /// reduces angular velocity to prevent overshooting and oscillation.
        /// </summary>
        /// <param name="rigidbody">The Rigidbody to apply rotational forces to.</param>
        /// <param name="direction">The desired world-space direction to face.</param>
        /// <param name="springStrength">How strongly the body is pulled toward the target rotation. Higher values result in faster, more aggressive correction.</param>
        /// <param name="damping">How strongly angular velocity is resisted. Higher values reduce overshoot and oscillation.</param>
        public static void RotateSpring(this Rigidbody rigidbody, Vector3 direction, float springStrength, float damping)
        {
            if (direction.sqrMagnitude < 0.0001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
            rigidbody.RotateSpring(targetRotation, springStrength, damping);
        }

        /// <summary>
        /// Applies a spring-damper force to the Rigidbody, smoothly driving it towards a target
        /// world-space position. A spring force proportional to the displacement accelerates the
        /// body toward the target, while a damping force opposing velocity prevents overshooting
        /// and oscillation.
        /// </summary>
        /// <param name="rigidbody">The Rigidbody to apply forces to.</param>
        /// <param name="targetPosition">The desired world-space position to move toward.</param>
        /// <param name="springStrength">How strongly the body is pulled toward the target position. Higher values result in faster, more aggressive correction.</param>
        /// <param name="damping">How strongly velocity is resisted. Higher values reduce overshoot and oscillation.</param>
        public static void MoveSpring(this Rigidbody rigidbody, Vector3 targetPosition, float springStrength, float damping)
        {
            Vector3 displacement = targetPosition - rigidbody.position;
            Vector3 springForce = displacement * springStrength;
            Vector3 dampingForce = -rigidbody.linearVelocity * damping;
            rigidbody.AddForce(springForce + dampingForce, ForceMode.Acceleration);
        }

        /// <summary>
        /// Set the position and rotation of a rigidbody.
        /// </summary>
        public static void SetPositionAndRotation(this Rigidbody rigidbody, Vector3 position, Quaternion rotation) 
        {
            rigidbody.MovePosition(position);
            rigidbody.MoveRotation(rotation);
        }

        /// <summary>
        /// Smoothly move position and rotation of a rigidbody. 
        /// Only works if interpolation is enabled and it's not kinematic.
        /// </summary>
        public static void MovePositionAndRotation(this Rigidbody rigidbody, Vector3 position, Quaternion rotation) 
        {
            rigidbody.MovePosition(position);
            rigidbody.MoveRotation(rotation);
        }
    }
}