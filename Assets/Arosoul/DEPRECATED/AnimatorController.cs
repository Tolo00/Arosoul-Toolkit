// using System;
// using System.Collections;
// using System.Collections.Generic;
// using Arosoul.Essentials;
// using UnityEngine;

// public class AnimatorController<EAnim> where EAnim : Enum {
//     private readonly Animator _animator;
//     private readonly Dictionary<EAnim, string> _animationLookup;
//     private readonly Queue<EAnim> _nextAnimQueue = new();

//     private EAnim _currentAnimation;
//     public EAnim CurrentAnimation => _currentAnimation;

//     private int _currentStateHash;

//     private float _lockTimer = 0f;
//     private bool _isPlayingQueuedAnim = false;

//     public AnimatorController(Dictionary<EAnim, string> animationDictionary, Animator animator, EAnim startAnimation) {
//         _animationLookup = animationDictionary ?? throw new ArgumentNullException(nameof(animationDictionary));
//         _animator = animator ?? throw new ArgumentNullException(nameof(animator));

//         Play(startAnimation);
//     }

//     /// <summary>
//     /// Plays an animation immediately (if not locked).
//     /// </summary>
//     public void Play(EAnim animation, bool lockForAnimLength = false) {
//         if (_lockTimer > 0) return;
//         if (animation.Equals(_currentAnimation)) return;

//         if (_animationLookup.TryGetValue(animation, out string animName)) {
//             _nextAnimQueue.Clear();
//             _animator.Play(animName, 0, 0f);

//             // Store hash for reliable state tracking
//             _currentStateHash = Animator.StringToHash(animName);
//             _currentAnimation = animation;

//             if (lockForAnimLength)
//                 TryLockForCurrentClip(animName);

//             return;
//         }

//         Debug.LogWarning($"[AnimatorController] Animation '{animation}' not found in lookup.", _animator);
//     }

//     public void AddToQueue(EAnim nextAnim) {
//         _nextAnimQueue.Enqueue(nextAnim);
//         TryStartQueueCoroutine();
//     }

//     public void LockAnimation(float duration) => _lockTimer = duration;
//     public void UnlockAnimation() => _lockTimer = 0f;

//     /// <summary>
//     /// Call this inside your MonoBehaviour's Update().
//     /// </summary>
//     public void Update() {
//         if (_lockTimer > 0f)
//             _lockTimer -= Time.deltaTime * _animator.speed;

//         // Detect animation change via state hash
//         var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
//         if (stateInfo.shortNameHash != _currentStateHash) {
//             _currentStateHash = stateInfo.shortNameHash;

//             // If the current animation finished and we have a queued one, start it
//             if (_nextAnimQueue.Count > 0 && !_isPlayingQueuedAnim)
//                 TryStartQueueCoroutine();
//         }
//     }

//     private void TryStartQueueCoroutine() {
//         if (_isPlayingQueuedAnim) return;
//         if (_nextAnimQueue.Count == 0) return;

//         _isPlayingQueuedAnim = true;
//         CoroutineRunner.Inst.StartCoroutine(PlayNextAnimCoroutine());
//     }

//     private IEnumerator PlayNextAnimCoroutine() {
//         while (_lockTimer > 0f)
//             yield return null;

//         if (_nextAnimQueue.Count > 0) {
//             var next = _nextAnimQueue.Dequeue();
//             Play(next);
//         }

//         _isPlayingQueuedAnim = false;
//     }

//     /// <summary>
//     /// Attempts to find the active clip to lock for its duration.
//     /// </summary>
//     private void TryLockForCurrentClip(string animName) {
//         var clips = _animator.GetCurrentAnimatorClipInfo(0);
//         foreach (var clipInfo in clips) {
//             var clip = clipInfo.clip;
//             if (clip != null && clip.name == animName) {
//                 LockAnimation(clip.length / _animator.speed);
//                 return;
//             }
//         }

//         // Fallback: search runtime clips if not yet transitioned
//         foreach (var clip in _animator.runtimeAnimatorController.animationClips) {
//             if (clip.name == animName) {
//                 LockAnimation(clip.length / _animator.speed);
//                 return;
//             }
//         }
//     }
// }
