using UnityEngine;

namespace Arosoul.Commands.Tests
{
    /// <summary>
    /// Instance commands used by the play mode tests.
    /// </summary>
    [CommandPrefix("target")]
    public class TestTarget : MonoBehaviour
    {
        [Command("health", MonoTargetType.All)]
        public float Health { get; set; } = 100f;

        [Command]
        public float Damage(float amount)
        {
            Health -= amount;
            return Health;
        }

        [Command(MonoTargetType.All)]
        public string Describe() => $"{name} {Health}";
    }
}
