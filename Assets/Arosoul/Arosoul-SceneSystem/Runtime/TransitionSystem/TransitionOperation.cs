using System;
using System.Collections;
using UnityEngine;

namespace Arosoul.SceneSystem
{
    public class TransitionOperation
    {    
        internal string Key = "";

        internal float InTime;
        internal float MinWaitTime;
        internal float OutTime;

        internal bool IsOutAllowed = true;

        public int Priority = 0;

        public Action OnInCompleted;
        public Action OnWaitCompleted;
        public Action OnOutCompleted;

        public Action OnCancel;

        /// <summary>
        /// Prevents the transition from proceeding to its "Out" phase until explicitly allowed.
        /// 
        /// Call this before or during the Wait phase if you need to delay exiting until
        /// another system (e.g. a scene manager or asynchronous loader) has completed.
        /// 
        /// Once the external condition is ready, call <see cref="GiveOutPermission"/> to continue.
        /// </summary>
        public TransitionOperation WaitForOutPermission() {
            IsOutAllowed = false;
            return this;
        }

        /// <summary>
        /// Signals that the transition is now allowed to proceed to its "Out" phase.
        /// 
        /// Typically called by an external system after an asynchronous operation completes
        /// (such as scene loading, data fetching, or a user input trigger).
        /// </summary>
        public TransitionOperation GiveOutPermission() {
            IsOutAllowed = true;
            return this;
        }

        
        internal Action OnCancelInternal;

        /// <summary>
        /// Cancel transition if active.
        /// </summary>
        public void Cancel() {
            OnCancelInternal?.Invoke();
        }    
    }
}