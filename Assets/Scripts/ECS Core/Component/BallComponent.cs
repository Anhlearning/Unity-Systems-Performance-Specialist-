using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
public struct BallComponent : IComponentData
{
    public float3 Position;
    public float3 Velocity;
    
}
