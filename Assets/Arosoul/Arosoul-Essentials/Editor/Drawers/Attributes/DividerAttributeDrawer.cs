using UnityEditor;
using UnityEngine;

namespace Arosoul.Essentials.Editor {

    [CustomPropertyDrawer(typeof(DividerAttribute))]
    public class DividerAttributeDrawer : DecoratorDrawer
    {
        readonly float LINE_HEIGHT = EditorGUIUtility.singleLineHeight;
        readonly float LINE_THICKNESS_MULTIPLIER = 0.075f;
        readonly float LINE_INDENT = 15f;

        public override float GetHeight() {
            DividerAttribute dividerAttribute = (DividerAttribute)attribute;
            return dividerAttribute.thickness * LINE_HEIGHT * LINE_THICKNESS_MULTIPLIER + 
                    dividerAttribute.upperPadding * LINE_HEIGHT +
                    dividerAttribute.bottomPadding * LINE_HEIGHT;
        }

        public override void OnGUI(Rect position) {
            DividerAttribute dividerAttribute = (DividerAttribute)attribute;

            // Draw the line
            Rect lineRect = new Rect(position.x + LINE_INDENT, 
                                        position.y + (dividerAttribute.upperPadding * LINE_HEIGHT), 
                                        position.width, 
                                        dividerAttribute.thickness * LINE_HEIGHT * LINE_THICKNESS_MULTIPLIER);
            EditorGUI.DrawRect(lineRect, Color.gray);
        }
    }

}