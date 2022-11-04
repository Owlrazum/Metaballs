using Unity.Mathematics;

namespace MarchingCubes
{
    public struct MarchingCubeVertex
    {
        public float3 Pos { get; set; }
        public float3 Color { get; set; }
        public byte Value { get; set; }
    }
}