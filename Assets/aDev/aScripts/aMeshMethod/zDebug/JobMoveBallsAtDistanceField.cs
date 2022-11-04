using Unity.Collections;
using Unity.Burst;
using UnityEngine;
using UnityEngine.Jobs;

using MarchingCubes;

[BurstCompile]
public struct JobMoveBallsAtDistanceField : IJobParallelForTransform
{
    [ReadOnly]
    public NativeArray<DistanceFieldCell> InputGridCells;

    public Vector3 DumpPos;

    public void Execute(int i, TransformAccess transform)
    {
        if (InputGridCells[i].Value >= 0)
        {
            transform.position = InputGridCells[i].LocalPos;
        }
        else
        { 
            transform.position = DumpPos;
        }
    }
}