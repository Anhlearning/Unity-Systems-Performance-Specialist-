// MagicCirclePreview: the simulator screen. Build a circle on the left, cast it
// in the middle, read what the compiler made of it on the right. The 3D stage
// renders into the middle of the screen and the IMGUI panels frame it.

using System.Collections.Generic;
using MagicCircleSim.Compiler;
using MagicCircleSim.View;
using UnityEngine;

namespace MagicCircleSim.Preview
{
    [DisallowMultipleComponent]
    public sealed class MagicCirclePreview : MonoBehaviour
    {
        const float Margin = 12;
        const float HeaderH = 44;
        const float LeftW = 400;
        const float RightW = 400;
        /// <summary>The layout is designed at this size and scaled to the screen.</summary>
        const float DesignH = 900, DesignW = 1440;

        [SerializeField] CastStage stage;
        [SerializeField] Camera stageCamera;
        [Tooltip("Cast again as soon as a cast ends.")]
        [SerializeField] bool loop;

        readonly SpellSession session = new SpellSession();
        readonly CirclePanel circlePanel = new CirclePanel();
        readonly SummaryPanel summaryPanel = new SummaryPanel();
        PalettePanel palettePanel;
        PreviewSkin skin;

        public SpellSession Session => session;

        public void Configure(CastStage castStage, Camera camera)
        {
            stage = castStage;
            stageCamera = camera;
        }

        void Start()
        {
            palettePanel = new PalettePanel();
            session.Changed += OnCircleChanged;
            stage.Finished += OnCastFinished;
            OnCircleChanged();
        }

        void OnCircleChanged()
        {
            stage.SetCircle(session.Circle);
            circlePanel.SetCircle(session.Circle);
            // While a cast plays, the stage previews the circle live as it is edited.
            if (!stage.Casting) return;
            if (session.Spell.Valid) stage.Cast(session.Spell);
            else stage.Clear();
        }

        /// <summary>A cast always performs the spell as it was compiled when it was triggered.</summary>
        public void Cast()
        {
            if (session.Spell.Valid) stage.Cast(session.Spell);
        }

        void OnCastFinished()
        {
            if (loop) Cast();
        }

        void OnGUI()
        {
            skin ??= new PreviewSkin();
            var scale = Mathf.Max(0.5f, Mathf.Min(Screen.height / DesignH, Screen.width / DesignW));
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            var w = Screen.width / scale;
            var h = Screen.height / scale;

            var body = new Rect(0, HeaderH, w, h - HeaderH);
            var left = new Rect(Margin, body.y + Margin, LeftW, body.height - 2 * Margin);
            var right = new Rect(w - RightW - Margin, left.y, RightW, left.height);
            var middle = Rect.MinMaxRect(left.xMax + Margin, left.y, right.x - Margin, left.yMax);

            FrameStage(middle, w, h, scale);
            Header(new Rect(0, 0, w, HeaderH));

            var circleH = LeftW + 24;
            circlePanel.Draw(new Rect(left.x, left.y, left.width, circleH), stage.Circle.Texture, session, skin);
            palettePanel.Draw(Rect.MinMaxRect(left.x, left.y + circleH + Margin, left.xMax, left.yMax), session, skin);
            summaryPanel.Draw(right, session.Spell, skin);
            CastControls(middle);
            Tooltip();
        }

        /// <summary>The camera renders only the middle, so paint the rest of the screen around it.</summary>
        void FrameStage(Rect middle, float w, float h, float scale)
        {
            if (Event.current.type == EventType.Repaint)
            {
                GUI.DrawTexture(new Rect(0, 0, w, middle.y), skin.BgTexture);
                GUI.DrawTexture(new Rect(0, middle.yMax, w, h - middle.yMax), skin.BgTexture);
                GUI.DrawTexture(new Rect(0, middle.y, middle.x, middle.height), skin.BgTexture);
                GUI.DrawTexture(new Rect(middle.xMax, middle.y, w - middle.xMax, middle.height), skin.BgTexture);
            }
            if (stageCamera != null)
            {
                stageCamera.pixelRect = new Rect(middle.x * scale, Screen.height - middle.yMax * scale, middle.width * scale, middle.height * scale);
            }
        }

        void Header(Rect rect)
        {
            GUILayout.BeginArea(new Rect(rect.x + 20, rect.y + 8, rect.width - 40, rect.height - 8));
            GUILayout.BeginHorizontal();
            GUILayout.Label("Magic Circle Simulator", skin.Title);
            GUILayout.FlexibleSpace();
            foreach (var (name, circle) in Presets.All)
            {
                if (GUILayout.Button(name, skin.Button, GUILayout.Height(28))) session.Load(circle);
            }
            if (GUILayout.Button("Xoá vòng", skin.Button, GUILayout.Height(28))) session.Clear();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void CastControls(Rect middle)
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Space)
            {
                Cast();
                e.Use();
            }
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape && stage.Casting)
            {
                stage.Stop();
                e.Use();
            }
            MissingHint(middle);

            GUILayout.BeginArea(new Rect(middle.x + 12, middle.yMax - 48, middle.width - 24, 40));
            GUILayout.BeginHorizontal();
            var previous = GUI.enabled;
            GUI.enabled = session.Spell.Valid;
            if (GUILayout.Button(stage.Casting ? "Kích hoạt lại  [Space]" : "Kích hoạt  [Space]", skin.Primary, GUILayout.Width(200), GUILayout.Height(34))) Cast();
            GUI.enabled = previous;
            if (stage.Casting)
            {
                GUILayout.Space(8);
                if (GUILayout.Button("Dừng  [Esc]", skin.Button, GUILayout.Width(110), GUILayout.Height(34))) stage.Stop();
            }
            GUILayout.Space(12);
            loop = GUILayout.Toggle(loop, " Lặp lại", GUILayout.Height(34));
            GUILayout.FlexibleSpace();
            if (stage.Performance != null)
            {
                var sustaining = stage.Sustaining ? "  · duy trì" : "";
                GUILayout.Label($"{stage.Performance.Beat}{sustaining}  {stage.Performance.TimeS:0.00}s", skin.MutedLabel, GUILayout.Height(34));
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        /// <summary>Tells the player which required layers to fill before the circle can be cast (A14).</summary>
        void MissingHint(Rect middle)
        {
            var spell = session.Spell;
            if (spell.Valid) return;
            var lines = new List<string>();
            foreach (var d in spell.Diagnostics)
                if (d.Severity == Severity.Error) lines.Add("• " + d.Message);
            var style = new GUIStyle(skin.Body) { wordWrap = true, padding = new RectOffset(10, 10, 8, 8) };
            style.normal.background = skin.PanelTexture;
            style.normal.textColor = PreviewSkin.Error;
            var content = new GUIContent("Cần chọn trước khi kích hoạt:\n" + string.Join("\n", lines));
            var width = Mathf.Min(460, middle.width - 24);
            var height = style.CalcHeight(content, width);
            GUI.Label(new Rect(middle.x + 12, middle.y + 12, width, height), content, style);
        }

        void Tooltip()
        {
            if (string.IsNullOrEmpty(GUI.tooltip)) return;
            var content = new GUIContent(GUI.tooltip);
            var style = new GUIStyle(skin.Body) { padding = new RectOffset(8, 8, 6, 6) };
            style.normal.background = skin.BgTexture;
            style.normal.textColor = PreviewSkin.Assume;
            var width = Mathf.Min(320, style.CalcSize(content).x);
            var height = style.CalcHeight(content, width);
            var mouse = Event.current.mousePosition;
            var screenW = Screen.width / GUI.matrix.m00;
            GUI.Label(new Rect(Mathf.Min(mouse.x + 12, screenW - width - 4), mouse.y + 16, width, height), content, style);
        }

        void OnDestroy()
        {
            session.Changed -= OnCircleChanged;
            if (stage != null) stage.Finished -= OnCastFinished;
            palettePanel?.Dispose();
            skin?.Dispose();
        }
    }
}
