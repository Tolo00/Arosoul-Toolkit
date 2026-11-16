using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Arosoul.PlayerSettings {

    /// <summary>One way of showing a section, shown in the layout editor's display dropdown (e.g. "Keyboard Keybinds").</summary>
    public sealed class SectionDisplay {
        public string Id { get; }
        public string Name { get; }
        public SectionDisplay(string id, string name) { Id = id; Name = name; }
    }

    /// <summary>What a section gets when its UI is built.</summary>
    public sealed class SectionContext {
        readonly Func<string, VisualTreeAsset> templates;

        public SectionContext(string displayId, bool instant, bool useDefaultStyle, Func<string, VisualTreeAsset> templates) {
            DisplayId = displayId;
            Instant = instant;
            UseDefaultStyle = useDefaultStyle;
            this.templates = templates;
        }

        /// <summary>The display option chosen in the layout editor.</summary>
        public string DisplayId { get; }

        /// <summary>True if the category applies changes immediately.</summary>
        public bool Instant { get; }

        /// <summary>False if the theme turned the default style off.</summary>
        public bool UseDefaultStyle { get; }

        /// <summary>UXML template by name: the theme's override if it has one, otherwise Resources/PlayerSettings/{name}.</summary>
        public VisualTreeAsset GetTemplate(string name) => templates(name);
    }

    /// <summary>
    /// A block of UI that is not a single value, e.g. all keybinds of an action map. Shows up in the layout editor
    /// like a setting (toggle + display dropdown) and takes part in Apply / Undo / Reset / the unapplied changes prompt.
    /// Provide sections with a SettingSectionProvider. This is how integrations plug in without the core knowing them.
    /// </summary>
    public abstract class SettingSection {
        /// <summary>Unique id, stored in the layout asset. Must not collide with a setting id.</summary>
        public abstract string Id { get; }
        public abstract string DisplayName { get; }
        public virtual string Group => "Other";
        public abstract IReadOnlyList<SectionDisplay> Displays { get; }

        public abstract VisualElement Build(SectionContext context);

        // Pending changes (all optional)
        public virtual bool HasPending => false;
        public virtual void ApplyPending() { }
        public virtual void RevertPending() { }

        /// <param name="displayId">Which display of the section the Reset button belongs to.</param>
        public virtual void ResetToDefaults(string displayId, bool applyImmediately) { }

        /// <summary>Raised when HasPending may have changed, the menu updates its buttons.</summary>
        public event Action StateChanged;
        protected void RaiseStateChanged() => StateChanged?.Invoke();
    }

    /// <summary>Finds sections. Derive from this (public, parameterless constructor), providers are discovered automatically.</summary>
    public abstract class SettingSectionProvider {
        public abstract IEnumerable<SettingSection> GetSections();
    }

}
