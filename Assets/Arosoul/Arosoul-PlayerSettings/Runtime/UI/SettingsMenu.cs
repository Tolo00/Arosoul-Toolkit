using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Arosoul.PlayerSettings {

    /// <summary>
    /// Generates the settings menu at runtime from a layout asset. Needs a PanelRenderer on the same object
    /// (with a Panel Settings asset assigned, Source Asset can stay empty), the menu is added to its root.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(PanelRenderer))]
    public sealed class SettingsMenu : MonoBehaviour {
        [SerializeField] SettingsLayout layout;
        [SerializeField] bool visibleOnStart = true;

        PanelRenderer panelRenderer;
        SettingsMenuView view;
        int uiVersion = -1;
        bool wantVisible;

        public event Action Closed;
        public bool IsOpen => view != null ? view.IsVisible : wantVisible;

        void Awake() {
            wantVisible = visibleOnStart;
            panelRenderer = GetComponent<PanelRenderer>();
            panelRenderer.RegisterUIReloadCallback(OnUIReload);
        }

        void OnDestroy() {
            if (panelRenderer != null) panelRenderer.UnregisterUIReloadCallback(OnUIReload);
            Teardown();
        }

        public void Open() {
            wantVisible = true;
            view?.Show();
        }

        /// <summary>Hides the menu. Edits that were not applied are discarded.</summary>
        public void Close() {
            wantVisible = false;
            GameSettings.Store.RevertPending();
            view?.Hide();
            Closed?.Invoke();
        }

        public void Toggle() {
            if (IsOpen) Close();
            else Open();
        }

        // Called when the panel's UI is (re)loaded. The version only changes on a real reload.
        void OnUIReload(PanelRenderer renderer, VisualElement root, int version) {
            if (view != null && version == uiVersion) return;
            uiVersion = version;

            Teardown();

            if (layout == null) {
                Debug.LogWarning("[Settings] SettingsMenu has no layout assigned.", this);
                return;
            }

            view = new SettingsMenuView(root, layout, GameSettings.Store);
            view.CloseRequested += Close;
            view.Build();

            if (wantVisible) view.Show();
        }

        void Teardown() {
            if (view == null) return;
            view.CloseRequested -= Close;
            view.Dispose();
            view = null;
        }
    }

}