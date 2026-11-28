using UnityEngine;

namespace Arosoul.Commands.Samples
{
    /// <summary>
    /// Example commands. Add this component to a GameObject, press play, open the console with ` and try:
    /// <code>
    /// sample.player.health
    /// sample.player.health = 50
    /// sample.player.heal 25
    /// sample.player.damage 30
    /// sample.player.teleport (0, 2, 0)
    /// sample.player.tint = orange
    /// sample.player.mode = god-mode
    /// sample.player.spawn-cube 3 #00FFAA
    /// </code>
    /// </summary>
    [CommandPrefix("sample.player")]
    public class CommandsSamplePlayer : MonoBehaviour
    {
        public enum PlayerMode
        {
            Normal,
            GodMode,
            NoClip
        }

        [SerializeField, Tooltip("Health of the player.")]
        [Command(MonoTargetType.All, Description = "Health of every sample player.")]
        float _health = 100f;

        [SerializeField, Tooltip("How the player is affected by damage and collisions.")]
        [Command("mode", Description = "Normal, GodMode or NoClip.")]
        PlayerMode _mode = PlayerMode.Normal;

        private const float MaxHealth = 100f;

        private Renderer _renderer;

        [Command("tint", "Color of the player's renderer.")]
        public Color Tint
        {
            get => _renderer != null ? _renderer.material.color : Color.white;
            set
            {
                if (_renderer != null) _renderer.material.color = value;
            }
        }

        private void Awake()
        {
            TryGetComponent(out _renderer);
        }

        [Command(Description = "Restores health, up to the maximum. Returns the new health.")]
        public float Heal(float amount = 10f)
        {
            _health = Mathf.Min(MaxHealth, _health + amount);
            return _health;
        }

        [Command(Description = "Deals damage, unless the player is in GodMode. Returns the new health.")]
        public float Damage(float amount)
        {
            if (_mode != PlayerMode.GodMode) _health = Mathf.Max(0f, _health - amount);
            return _health;
        }

        [Command(Description = "Moves the player to a position.")]
        public void Teleport(Vector3 position)
        {
            transform.position = position;
        }

        [Command("spawn-cube", "Spawns cubes in a row at the world origin.")]
        private static string SpawnCube(int count = 1, Color color = default)
        {
            Color cubeColor = color == default ? Color.white : color;
            for (int i = 0; i < count; i++)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.position = new Vector3(i * 1.5f, 0.5f, 0f);
                cube.GetComponent<Renderer>().material.color = cubeColor;
            }
            return $"Spawned {count} cube(s).";
        }
    }
}
