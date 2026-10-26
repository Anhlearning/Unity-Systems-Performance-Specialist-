using Ignite.Core.Save;
using Newtonsoft.Json.Linq;
using Scellecs.Morpeh;

namespace Ignite.Ecs
{
    /// <summary>
    /// Module muốn lưu tiến trình thì cài thêm interface này. EcsWorldHost tự đăng ký nó với SaveService
    /// khi World được tạo, và gỡ ra (sau khi chụp lần cuối) trước khi World bị huỷ.
    /// </summary>
    public interface ISaveableModule
    {
        string SaveKey { get; }
        JToken Save(World world);

        /// <summary>Có thể được gọi trước khi system chạy frame đầu tiên: module tự tạo entity cần thiết nếu chưa có.</summary>
        void Load(World world, JToken data);
    }

    sealed class ModuleSaveParticipant : ISaveParticipant
    {
        readonly ISaveableModule _module;
        readonly World _world;

        public ModuleSaveParticipant(ISaveableModule module, World world)
        {
            _module = module;
            _world = world;
        }

        public string SaveKey => _module.SaveKey;
        public JToken Capture() => _module.Save(_world);
        public void Restore(JToken data) => _module.Load(_world, data);
    }
}
