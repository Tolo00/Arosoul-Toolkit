namespace Arosoul.Essentials
{
    public struct DataChange<T>
    {
        public T OldValue;
        public T NewValue;

        public DataChange(T oldValue, T newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}