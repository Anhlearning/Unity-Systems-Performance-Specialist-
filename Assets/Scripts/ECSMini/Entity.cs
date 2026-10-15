using UnityEngine;

public readonly struct Entity
{
    public readonly int Id;
    public readonly int Gen;
    public Entity(int id, int generation)
    {
        Id = id;
        Gen = generation;
    }
    public override string ToString() => $"E{Id}v{Gen}";   // chỉ dùng để log
}