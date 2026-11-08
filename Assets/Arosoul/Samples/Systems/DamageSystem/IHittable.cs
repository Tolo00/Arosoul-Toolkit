using UnityEngine;

public interface IHittable {
    bool CanHit { get; }
    void OnHit(Object sender, int amount);
}