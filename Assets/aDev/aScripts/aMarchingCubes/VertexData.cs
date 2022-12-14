using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace MarchingCubes
{ 
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexData
    {
        public float3 position;
        public float3 normal;
        public float3 color;
        public float2 uv;

        public VertexData(float3 position, float3 normal, float3 color, float2 uv)
        {
            this.position = position;
            this.normal = normal;
            this.color = color;
            this.uv = uv;
        }

        public static readonly VertexAttributeDescriptor[] VertexBufferMemoryLayout =
        {
            new VertexAttributeDescriptor(VertexAttribute.Position),
            new VertexAttributeDescriptor(VertexAttribute.Normal),
            new VertexAttributeDescriptor(VertexAttribute.Color),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2)
    };

        public override string ToString()
        {
            return position.ToString();
        }
    }
}
