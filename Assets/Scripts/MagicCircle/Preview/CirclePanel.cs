// CirclePanel: the circle as the player builds it, inked in the element's
// color. Clicking a placed rune takes it off, like the web build's rune hooks.

using MagicCircleSim.Circle;
using MagicCircleSim.Design;
using UnityEngine;

namespace MagicCircleSim.Preview
{
    public sealed class CirclePanel
    {
        CircleDrawing drawing;
        Color ink = Color.white;

        /// <summary>Keeps the hit areas and the ink in step with the circle.</summary>
        public void SetCircle(MagicCircle circle)
        {
            drawing = DrawCircle.Build(circle);
            ink = PreviewSkin.Hex(drawing.Ink);
        }

        public void Draw(Rect rect, Texture circleTexture, SpellSession session, PreviewSkin skin)
        {
            GUI.DrawTexture(rect, skin.PanelTexture);
            var image = new Rect(rect.x + 8, rect.y + 8, rect.width - 16, rect.width - 16);
            var previous = GUI.color;
            GUI.color = ink;
            GUI.DrawTexture(image, circleTexture, ScaleMode.ScaleToFit, true);
            GUI.color = previous;

            var hovered = RuneUnder(image, Event.current.mousePosition);
            var hint = hovered != null ? $"Nhấn để gỡ rune “{Runes.Find(hovered)?.Label}”" : "Nhấn vào một rune trên vòng để gỡ nó.";
            GUI.Label(new Rect(rect.x + 10, image.yMax + 2, rect.width - 20, 20), hint, skin.MutedLabel);

            var e = Event.current;
            if (hovered != null && e.type == EventType.MouseDown && e.button == 0)
            {
                session.RemoveRune(hovered);
                e.Use();
            }
        }

        string RuneUnder(Rect image, Vector2 mouse)
        {
            if (drawing == null || !image.Contains(mouse)) return null;
            var box = drawing.Box;
            // GUI y grows downward like the drawing's, so no flip.
            var x = box.MinX + (mouse.x - image.x) / image.width * box.Size;
            var y = box.MinY + (mouse.y - image.y) / image.height * box.Size;
            return drawing.RuneAt(new Vec2(x, y));
        }
    }
}
