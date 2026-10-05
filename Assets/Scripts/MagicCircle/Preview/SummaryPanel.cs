// SummaryPanel: what the compiler made of the circle, the rules it raised, and
// the assumptions behind each value. Each A-xx tag carries its text as a tooltip.

using MagicCircleSim.Compiler;
using MagicCircleSim.Design;
using UnityEngine;

namespace MagicCircleSim.Preview
{
    public sealed class SummaryPanel
    {
        Vector2 scroll;
        bool showAssumptions;

        public void Draw(Rect rect, SpellIR spell, PreviewSkin skin)
        {
            GUI.DrawTexture(rect, skin.PanelTexture);
            GUILayout.BeginArea(new Rect(rect.x + 12, rect.y + 10, rect.width - 24, rect.height - 20));
            GUILayout.BeginHorizontal();
            GUILayout.Label("Phép đã biên dịch", skin.Title);
            GUILayout.FlexibleSpace();
            var status = new GUIStyle(skin.Tag) { fontSize = 11 };
            status.normal.textColor = spell.Valid ? PreviewSkin.Ok : PreviewSkin.Error;
            GUILayout.Label(spell.Valid ? "Hợp lệ" : "Chưa hợp lệ", status);
            GUILayout.EndHorizontal();

            scroll = GUILayout.BeginScrollView(scroll);
            foreach (var diagnostic in spell.Diagnostics)
            {
                var style = new GUIStyle(skin.Body);
                style.normal.textColor = diagnostic.Severity == Severity.Error ? PreviewSkin.Error : PreviewSkin.Info;
                GUILayout.Label("• " + diagnostic.Message, style);
            }
            GUILayout.Space(6);

            foreach (var row in SummaryRows.Of(spell))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(row.Layer, skin.MutedLabel, GUILayout.Width(52));
                GUILayout.Label(row.Label, skin.Body, GUILayout.Width(130));
                var value = new GUIStyle(skin.Body) { fontStyle = FontStyle.Bold, wordWrap = false };
                GUILayout.Label(row.Value, value);
                if (row.Defaulted) GUILayout.Label("mặc định", skin.Tag);
                foreach (var id in row.Assumptions)
                {
                    var tag = new GUIStyle(skin.Tag);
                    tag.normal.textColor = PreviewSkin.Assume;
                    GUILayout.Label(new GUIContent(id, Assumptions.TextOf(id)), tag);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);
            showAssumptions = GUILayout.Toggle(showAssumptions, $" Giả định cho các mục chưa xác nhận ({Assumptions.All.Count})", skin.MutedLabel);
            if (showAssumptions)
            {
                foreach (var a in Assumptions.All)
                {
                    GUILayout.Label(a.Rev != null ? $"{a.Id} [{a.Rev}] {a.Text}" : $"{a.Id} {a.Text}", skin.MutedLabel);
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
