using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct MoveInput : IComponentData
{
   public float2 Direction;
}
