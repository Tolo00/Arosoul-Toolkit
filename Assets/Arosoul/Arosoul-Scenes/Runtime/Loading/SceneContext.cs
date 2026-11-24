namespace Arosoul.Scenes
{
    /// <summary>
    /// Information about the scene being initialized and the operation that loads it.
    /// </summary>
    public sealed class SceneContext
    {
        public string Guid { get; }
        public string Name { get; }
        public SlotId Slot { get; }
        public object Payload { get; }

        public SceneContext(string guid, string name, SlotId slot, object payload)
        {
            Guid = guid;
            Name = name;
            Slot = slot;
            Payload = payload;
        }

        public bool HasPayload => Payload != null;

        public bool TryGetPayload<T>(out T payload)
        {
            if (Payload is T typed)
            {
                payload = typed;
                return true;
            }
            payload = default;
            return false;
        }

        public T GetPayload<T>()
        {
            return Payload is T typed ? typed : default;
        }
    }
}
