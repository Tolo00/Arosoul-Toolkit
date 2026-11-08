using Arosoul.Essentials;
using UnityEngine;

[System.Serializable]
public class PoolParameters<T> where T : Component {
    public T Prefab;
    public Transform Parent;
    public int PrewarmCount = 0;
    public bool CollectionCheck = false;
    public int DefaultCapacity = 10;
    public int MaxSize = 100;
}

public class ObjectPoolManager : Singleton<ObjectPoolManager> {
    
    // EXAMPLE: How to define a pool
        // [Header("Pool Parameters")]

        // [SerializeField] PoolParameters<Bullet> _bulletPoolParams;

        // private ComponentPool<Bullet> _bulletPool;
        // public static ComponentPool<Bullet> BulletPool => Inst._bulletPool;

    void Awake() {
        InitializePools();        
    }

    private void InitializePools() {
        // EXAMPLE POOL:
            // _bulletPool = new (
            //     _bulletPoolParams.Prefab,
            //     _bulletPoolParams.Parent,
            //     _bulletPoolParams.PrewarmCount,
            //     _bulletPoolParams.CollectionCheck,
            //     _bulletPoolParams.DefaultCapacity,
            //     _bulletPoolParams.MaxSize
            // );
    }
}