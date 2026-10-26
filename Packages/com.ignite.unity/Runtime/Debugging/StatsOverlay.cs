using Unity.Profiling;
using UnityEngine;

namespace Ignite.Unity.Debugging
{
    /// <summary>FPS và GC alloc mỗi frame (ProfilerRecorder). Chuỗi chỉ dựng lại 2 lần/giây.</summary>
    public sealed class StatsOverlay : MonoBehaviour
    {
        ProfilerRecorder _gcAlloc;
        float _timer;
        int _frames;
        string _text = string.Empty;
        GUIStyle _style;

        public bool Visible
        {
            get => enabled;
            set => enabled = value;
        }

        public long LastFrameGcBytes => _gcAlloc.Valid ? _gcAlloc.LastValue : -1;

        void OnEnable() => _gcAlloc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        void OnDisable() => _gcAlloc.Dispose();

        void Update()
        {
            _frames++;
            _timer += Time.unscaledDeltaTime;
            if (_timer < 0.5f)
                return;
            _text = $"{_frames / _timer:0} FPS · GC {LastFrameGcBytes} B/frame";
            _frames = 0;
            _timer = 0f;
        }

        void OnGUI()
        {
            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 14 };
            GUI.Label(new Rect(Screen.width - 260f, 8f, 250f, 24f), _text, _style);
        }
    }
}
