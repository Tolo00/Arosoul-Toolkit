using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Arosoul.PlayerSettings {

    public sealed class HeaderDecoration : LayoutDecoration {
        static readonly DecorationParameter[] parameters = { DecorationParameter.Text("text", "Text", "Header") };

        public override string Id => "decoration:header";
        public override string DisplayName => "Header";
        public override IReadOnlyList<DecorationParameter> Parameters => parameters;

        public override VisualElement Build(DecorationContext context) {
            var label = new Label(context.GetText("text"));
            label.AddToClassList("settings-header");
            return label;
        }
    }

    public sealed class SeparatorDecoration : LayoutDecoration {
        public override string Id => "decoration:separator";
        public override string DisplayName => "Separator Line";

        public override VisualElement Build(DecorationContext context) {
            var line = new VisualElement();
            line.AddToClassList("settings-separator");
            return line;
        }
    }

    public sealed class SpacingDecoration : LayoutDecoration {
        static readonly DecorationParameter[] parameters = { DecorationParameter.Number("height", "Height", 16f) };

        public override string Id => "decoration:spacing";
        public override string DisplayName => "Spacing";
        public override IReadOnlyList<DecorationParameter> Parameters => parameters;

        public override VisualElement Build(DecorationContext context) {
            var spacer = new VisualElement();
            spacer.AddToClassList("settings-spacer");
            spacer.style.height = context.GetNumber("height");
            return spacer;
        }
    }

}