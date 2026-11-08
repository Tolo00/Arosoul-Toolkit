using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arosoul.SceneSystem
{
    public class TransitionManager : LazyLoaded<TransitionManager>
    {
        public bool IsBusy => ActiveOperation != null;

        public TransitionOperation ActiveOperation;

        private Transition _transitionInstance;
        private Coroutine _transitionCoroutine;

        private Dictionary<string, Transition> _transitionByKey;

        protected override void OnSingletonEnable()
        {
            RegisterKeys();
        }

        private void RegisterKeys()
        {
            _transitionByKey = new Dictionary<string, Transition>();
            for (int i = 0; i < SceneSystemSettings.Instance.RegisteredTransitions.Count; i++)
            {
                var entry = SceneSystemSettings.Instance.RegisteredTransitions[i];

                if (entry == null) continue;
                if (string.IsNullOrEmpty(entry.TransitionKey) || entry.TransitionPrefab == null)
                {
                    Debug.LogWarning($"Invalid transition entry found at index: {entry.TransitionKey}\nPlease make sure all entries are valid in Project Setting > Player > Scene System");
                    continue;
                }
                if (_transitionByKey.ContainsKey(entry.TransitionKey))
                {
                    Debug.LogWarning($"Duplicate transition key found: {entry.TransitionKey}\nPlease make sure all entries are valid in Project Setting > Player > Scene System");
                    continue;
                }

                _transitionByKey.Add(entry.TransitionKey, entry.TransitionPrefab);
            }
        }


        internal void StartTransition(TransitionOperation operation) {
            if (operation.Key == "") {
                Debug.LogWarning("No transition key selected.");
                return;
            }
            
            if (ActiveOperation != null) {
                if (ActiveOperation.Priority >= operation.Priority) {
                    Debug.LogWarning("Unable to start transition. Priority is less than or equal to the current one.");
                    return;
                }

                _transitionInstance.OnCancel();
                DestroyActive();
            }

            if (!_transitionByKey.TryGetValue(operation.Key, out _transitionInstance))
            {
                Debug.LogError($"Unable to find transition object for key: '{operation.Key}'\nMake sure transition registry don't contain empty providers in Project Settings > Player > Scene System", Inst);
                return;
            }
            
            ActiveOperation = operation;

            _transitionCoroutine = StartCoroutine(HandleTransitionRoutine(operation));
            ActiveOperation.OnCancelInternal += TransitionOperation_OnCancel;
        }
        
        private IEnumerator HandleTransitionRoutine(TransitionOperation operation) {        
            yield return StartCoroutine(_transitionInstance.In(operation.InTime));
            yield return StartCoroutine(_transitionInstance.Wait(operation.MinWaitTime));

            // Wait until allowed to transition out
            while (!operation.IsOutAllowed)
                yield return null;

            yield return StartCoroutine(_transitionInstance.Out(operation.OutTime));

            DestroyActive();
        }

        /// <summary>
        /// Destroy active transition.
        /// </summary>
        private void DestroyActive()
        {
            if (ActiveOperation == null) return;

            StopCoroutine(Inst._transitionCoroutine);
            Destroy(_transitionInstance.gameObject);

            ActiveOperation = null;
            _transitionCoroutine = null;
            _transitionInstance = null;
        }

        private void TransitionOperation_OnCancel() {
            DestroyActive();
        }
    }
}