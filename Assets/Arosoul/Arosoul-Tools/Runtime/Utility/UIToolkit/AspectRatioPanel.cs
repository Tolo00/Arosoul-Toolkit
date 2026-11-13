/*
This is free and unencumbered software released into the public
domain.
Anyone is free to copy, modify, publish, use, compile, sell, or
distribute this software, either in source code form or as a compiled
binary, for any purpose, commercial or non-commercial, and by any
means.
In jurisdictions that recognize copyright laws, the author or authors
of this software dedicate any and all copyright interest in the
software to the public domain. We make this dedication for the
benefit of the public at large and to the detriment of our heirs and
successors. We intend this dedication to be an overt act of
relinquishment in perpetuity of all present and future rights to this
software under copyright law.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using UnityEngine;
using UnityEngine.UIElements;

namespace Arosoul.Tools 
{
    [UxmlElement] // Define the element using UxmlElement
    public partial class AspectRatioPanel : VisualElement
    {
        [UxmlAttribute("aspect-ratio-x")] // Define attributes using UxmlAttribute
        public int AspectRatioX { get; private set; } = 16;

        [UxmlAttribute("aspect-ratio-y")]
        public int AspectRatioY { get; private set; } = 9;

        [UxmlAttribute("balance-x")]
        public int BalanceX { get; private set; } = 50;

        [UxmlAttribute("balance-y")]
        public int BalanceY { get; private set; } = 50;

        public AspectRatioPanel()
        {
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = StyleKeyword.Undefined;
            style.bottom = StyleKeyword.Undefined;
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanelEvent);
        }


        void OnAttachToPanelEvent( AttachToPanelEvent e )
        {
            parent?.RegisterCallback<GeometryChangedEvent>( OnGeometryChangedEvent );
            FitToParent();
        }


        void OnGeometryChangedEvent( GeometryChangedEvent e )
        {
            FitToParent();
        }


        void FitToParent()
        {
            if (parent == null) return;
            var parentW = parent.resolvedStyle.width;
            var parentH = parent.resolvedStyle.height;
            if (float.IsNaN( parentW ) || float.IsNaN( parentH )) return;

            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = StyleKeyword.Undefined;
            style.bottom = StyleKeyword.Undefined;

            if (AspectRatioX <= 0.0f || AspectRatioY <= 0.0f)
            {
                style.width = parentW;
                style.height = parentH;
                return;
            }

            var ratio = Mathf.Min( parentW / AspectRatioX, parentH / AspectRatioY );
            var targetW = Mathf.Floor( AspectRatioX * ratio );
            var targetH = Mathf.Floor( AspectRatioY * ratio );
            style.width = targetW;
            style.height = targetH;

            var marginX = parentW - targetW;
            var marginY = parentH - targetH;
            style.left = Mathf.Floor( marginX * BalanceX / 100.0f );
            style.top = Mathf.Floor( marginY * BalanceY / 100.0f );
        }
    }
}