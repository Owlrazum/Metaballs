using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using UnityEngine.Jobs;

using MarchingCubes;

[BurstCompile]
public struct JobMoveBallsAtVertices : IJobParallelForTransform
{
    [ReadOnly]
    public NativeArray<VertexData> InputVertices;

    public float3 DumpPos;
    public int MaxIterationCount;

    public void Execute(int i, TransformAccess transform)
    {
        if (i >= MaxIterationCount)
        {
            transform.position = DumpPos;
            return;
        }
        else
        {
            transform.position = InputVertices[i].position;
        }
    }
}