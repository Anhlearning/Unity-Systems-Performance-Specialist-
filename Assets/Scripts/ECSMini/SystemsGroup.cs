using System.Collections.Generic;

public interface ISystem
{
    void OnAwake(World w);       // chạy 1 lần: lấy pool, tạo filter, cache mọi thứ cần dùng
    void OnUpdate(float dt);     // chạy mỗi frame
}

// Chạy các system theo thứ tự thêm vào, Commit() sau MỖI system (giống Morpeh):
// - Trong lúc 1 system chạy, world bị khoá → mọi Add/Remove/Destroy bị hoãn,
//   cả system nhìn thấy cùng một trạng thái từ đầu đến cuối.
// - System sau thấy ĐẦY ĐỦ thay đổi của system trước.
public sealed class SystemsGroup
{
    readonly World _world;
    readonly List<ISystem> _systems = new();

    public SystemsGroup(World world) { _world = world; }

    public SystemsGroup Add(ISystem system) { _systems.Add(system); return this; }

    public void Awake()
    {
        for (int i = 0; i < _systems.Count; i++) _systems[i].OnAwake(_world);
    }

    public void Update(float dt)
    {
        for (int i = 0; i < _systems.Count; i++)
        {
            _world.BeginIteration();             // khoá
            try { _systems[i].OnUpdate(dt); }
            finally { _world.EndIteration(); }   // mở khoá ngoài cùng → world.Commit()
        }
    }
}
