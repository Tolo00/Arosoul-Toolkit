
using UnityEngine;

namespace Arosoul.SceneSystem
{
    [System.Serializable]
    public class TransitionEntry
    {
        [SerializeField] private string _transitionKey;
        [SerializeField] private Transition _transitionPrefab;
        
        public string TransitionKey => _transitionKey;
        public Transition TransitionPrefab => _transitionPrefab;
    }
}
