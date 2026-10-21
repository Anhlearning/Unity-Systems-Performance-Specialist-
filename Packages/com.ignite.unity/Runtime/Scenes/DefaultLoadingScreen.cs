using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Ignite.Unity.Scenes
{
    /// <summary>
    /// Màn loading mặc định nằm trong scene Boot: canvas và camera riêng, tự dựng UI lúc Awake.
    /// Chỉ chạy Update khi đang hiện, nên lúc chơi bình thường không tốn gì mỗi frame.
    /// </summary>
    public sealed class DefaultLoadingScreen : MonoBehaviour, ILoadingScreen
    {
        [SerializeField] float fadeSeconds = 0.15f;
        [Tooltip("Tốc độ tối đa của thanh hiển thị, đơn vị 'cả thanh mỗi giây'.")]
        [SerializeField] float barSpeed = 2.5f;

        Canvas _canvas;
        CanvasGroup _group;
        RectTransform _fill;
        Camera _camera;
        float _target;
        float _shown;

        void Awake()
        {
            BuildUi();
            SetVisible(false);
            enabled = false;
        }

        public async UniTask ShowAsync()
        {
            _target = 0f;
            _shown = 0f;
            ApplyBar();
            SetVisible(true);
            enabled = true;
            await Fade(0f, 1f);
        }

        public void SetProgress(float progress)
        {
            _target = Mathf.Clamp01(progress);
        }

        public async UniTask HideAsync()
        {
            _target = 1f;
            while (_shown < 1f)
                await UniTask.Yield();

            await Fade(1f, 0f);
            enabled = false;
            SetVisible(false);
        }

        void Update()
        {
            _shown = Mathf.MoveTowards(_shown, _target, barSpeed * Time.unscaledDeltaTime);
            ApplyBar();
        }

        async UniTask Fade(float from, float to)
        {
            float t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Lerp(from, to, t / fadeSeconds);
                await UniTask.Yield();
            }
            _group.alpha = to;
        }

        void ApplyBar()
        {
            _fill.anchorMax = new Vector2(_shown, 1f);
        }

        void SetVisible(bool visible)
        {
            _canvas.enabled = visible;
            // Camera riêng chỉ bật lúc loading, để màn hình không trống khi mọi scene khác đã unload.
            _camera.enabled = visible;
        }

        void BuildUi()
        {
            var cameraGo = new GameObject("LoadingCamera");
            cameraGo.transform.SetParent(transform, false);
            _camera = cameraGo.AddComponent<Camera>();
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.black;
            _camera.cullingMask = 0;
            _camera.depth = -100;

            var canvasGo = new GameObject("LoadingCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = short.MaxValue;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _group = canvasGo.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = true;

            var background = CreateImage("Background", canvasGo.transform, Color.black);
            Stretch(background, Vector2.zero, Vector2.one);

            var track = CreateImage("BarTrack", canvasGo.transform, new Color(1f, 1f, 1f, 0.15f));
            Stretch(track, new Vector2(0.2f, 0.1f), new Vector2(0.8f, 0.12f));

            _fill = CreateImage("BarFill", track, Color.white);
            Stretch(_fill, Vector2.zero, new Vector2(0f, 1f));
        }

        static RectTransform CreateImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
