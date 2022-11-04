using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;

using UnityEngine;

namespace MarchingCubes
{ 
    [BurstCompile]
    public struct JobInitDistanceField : IJobFor
    {
        [ReadOnly]
        public NativeArray<float3> MetaballsPosInput;

        [ReadOnly] 
        // in HSL space
        public NativeArray<float3> MetaballsColorsInput;

        [ReadOnly] 
        public NativeArray<float> MetaballsRadiusInput;

        [WriteOnly]
        public NativeArray<DistanceFieldCell> GridCellsWrite;

        public float Threshold;
        public float UsualValue;
        public float MinimalValue;

        public int GridResolution;
        public float CellDelta;
        public float3 StartCell;

        public void Execute(int e)
        {
            int row = e / GridResolution;
            int col = e % GridResolution;

            float3 delta   = new float3(col * CellDelta, 0 ,row * CellDelta);
            DistanceFieldCell gridCell = new DistanceFieldCell()
            {
                LocalPos = StartCell + delta,
            };

            float alpha = 0;
            float3 color = float3.zero;
            int affectingColorsCount = 0;
            float totatlHue = 0;
            for (int i = 0; i < MetaballsPosInput.Length; i++)
            {
                float distance = math.distance(gridCell.LocalPos, MetaballsPosInput[i]);
                if (distance > MetaballsRadiusInput[i])
                {
                    continue;
                }

                float factor = (MetaballsRadiusInput[i] - distance) / MetaballsRadiusInput[i];
                alpha += factor;
                affectingColorsCount++;
                totatlHue += MetaballsColorsInput[i].x;
            }

            color = MetaballsColorsInput[0];
            color.x = totatlHue / affectingColorsCount;

            gridCell.MetaballColor = color;

            if (alpha >= Threshold)
            {
                float lerpParam = Mathf.InverseLerp(Threshold, 1, alpha);
                gridCell.Value = Mathf.Lerp(MinimalValue, UsualValue, EaseOut(lerpParam));
            }
            else
            { 
                float lerpParam = Mathf.InverseLerp(0, Threshold, alpha);
                gridCell.Value = Mathf.Lerp(-0.1f, MinimalValue, EaseOut(lerpParam));
            }

            GridCellsWrite[e] = gridCell;
        }

        public static float EaseIn(float lerpParam)
        {
            return lerpParam * lerpParam;
        }

        public static float Flip(float t)
        {
            return 1 - t;
        }

        public static float EaseOut(float lerpParam)
        {
            return Flip(EaseIn(Flip(lerpParam)));
        }

        public static float EaseInOut(float lerpParam)
        {
            return Mathf.Lerp(EaseIn(lerpParam), EaseOut(lerpParam), lerpParam);
        }
    }
}

                // float3 pos = StartCell + delta;
                // float rayLength = 0.01f * gridCell.Value;
                // Debug.DrawRay(pos, Vector3.forward * rayLength, Color.red);
                // Debug.DrawRay(pos, Vector3.back * rayLength, Color.red);
                // Debug.DrawRay(pos, Vector3.left * rayLength, Color.red);
                // Debug.DrawRay(pos, Vector3.right * rayLength, Color.red);
                // Debug.DrawRay(pos, Vector3.up *, Color.black, -1, true);