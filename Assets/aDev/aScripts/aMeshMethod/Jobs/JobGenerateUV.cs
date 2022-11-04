using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace MarchingCubes
{
    [BurstCompile]
    public struct JobGenerateUV : IJobFor
    {
        public NativeArray<VertexData> VerticesToModify;
        public float MaxHeight;

        public void Execute(int i)
        {
            VertexData toModify = VerticesToModify[i];
            float lerpParam = toModify.position.y / MaxHeight;
            toModify.uv = math.lerp(float2.zero, new float2(0, 1), lerpParam);
            VerticesToModify[i] = toModify;
        }
    }
}
