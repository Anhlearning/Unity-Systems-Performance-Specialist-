using Cysharp.Threading.Tasks;

namespace Ignite.Unity.Scenes
{
    public interface ILoadingScreen
    {
        UniTask ShowAsync();

        /// <summary>Tiến trình thật, 0..1. Màn loading tự làm mượt phần hiển thị.</summary>
        void SetProgress(float progress);

        /// <summary>Chờ thanh tiến trình chạy tới đích rồi mới ẩn.</summary>
        UniTask HideAsync();
    }
}
