using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arosoul.Transitions
{
    /// <summary>
    /// Reference counted input blocking. Transitions and scene operations acquire a block while they are active.
    ///
    /// The overlay panel blocks UI Toolkit pointer input by itself while a block is held.
    /// For gameplay input and navigation (keyboard, gamepad), subscribe to <see cref="BlockedChanged"/>
    /// or check <see cref="IsBlocked"/> in your input layer.
    /// </summary>
    public static class InputBlocker
    {
        private static readonly List<Token> _tokens = new List<Token>();

        public static bool IsBlocked => _tokens.Count > 0;
        public static int BlockCount => _tokens.Count;

        /// <summary>
        /// Raised when the blocked state changes. The argument is the new state.
        /// </summary>
        public static event Action<bool> BlockedChanged;

        /// <summary>
        /// Reasons of all active blocks. Used by the debugger.
        /// </summary>
        public static IReadOnlyList<string> GetReasons()
        {
            var reasons = new List<string>(_tokens.Count);
            for (int i = 0; i < _tokens.Count; i++)
                reasons.Add(_tokens[i].Reason);
            return reasons;
        }

        /// <summary>
        /// Block input until the returned token is disposed.
        /// </summary>
        public static IDisposable Acquire(string reason = "")
        {
            bool wasBlocked = IsBlocked;
            var token = new Token(reason);
            _tokens.Add(token);
            if (!wasBlocked) BlockedChanged?.Invoke(true);
            return token;
        }

        private static void Release(Token token)
        {
            if (!_tokens.Remove(token)) return;
            if (!IsBlocked) BlockedChanged?.Invoke(false);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _tokens.Clear();
            BlockedChanged = null;
        }

        private sealed class Token : IDisposable
        {
            public readonly string Reason;
            private bool _disposed;

            public Token(string reason)
            {
                Reason = reason;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                Release(this);
            }
        }
    }
}
