using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using UnityEngine.Jobs;

using MarchingCubes;

[BurstCompile]
public struct JobMoveBallsAtScalarField : IJobParallelForTransform
{
    [ReadOnly]
    public ScalarField<byte> InputScalarField;

    [ReadOnly]
    public NativeArray<DistanceFieldCell> InputGridCells;

    public float3 DumpPos;
    public float CellSize;
    public float StartHeight;
    public float IsoLevel;

    public void Execute(int i, TransformAccess transform)
    {
        // Debug.Log("J " + InputScalarField.GetData(i));
        if (InputScalarField.GetData(i) < IsoLevel)
        {
            int gridCellIndex = InputScalarField.GetDistanceIndex(i);
            float3 gridCellPos = InputGridCells[gridCellIndex].LocalPos;
            int gridCellHeight = InputScalarField.GetHeightIndex(i);
            gridCellPos.y = StartHeight + CellSize * gridCellHeight;
            transform.position = gridCellPos;
        }
        else
        { 
            transform.position = DumpPos;
        }
    }
}