using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;

using UnityEngine;

namespace MarchingCubes
{ 
    [BurstCompile]
    public struct JobInitScalarField : IJobFor
    { 
        [ReadOnly]
        public NativeArray<DistanceFieldCell> InputDistanceField;

        [WriteOnly]
        public ScalarField<ScalarFieldCell> OutputScalarField;

        public float HeightOffset;
        public float Amplitude;

        public void Execute(int index)
        {
            int distanceIndex = OutputScalarField.GetDistanceIndex(index);
            float distanceFieldData = InputDistanceField[distanceIndex].Value;

            if (distanceFieldData < 0)
            {
                OutputScalarField.SetData(
                    ScalarFieldCell.Empty((byte)((1 + distanceFieldData) * 255)), 
                    index
                );
                return;
            }

            int heightIndex = OutputScalarField.GetHeightIndex(index);
            float heightLerpParam = ((float)heightIndex + 1) / OutputScalarField.Height;
            byte value = (byte)(Mathf.Abs(Amplitude * heightLerpParam - 0.5f * distanceFieldData + HeightOffset) * 255);
            ScalarFieldCell scalarFieldCell = new ScalarFieldCell()
            {
                Value = value,
                MetaballColor = InputDistanceField[distanceIndex].MetaballColor,
            };
            OutputScalarField.SetData(scalarFieldCell, index);
        }
    }
}