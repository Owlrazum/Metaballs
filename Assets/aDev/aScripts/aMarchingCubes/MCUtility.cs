using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

namespace MarchingCubes
{ 
    public static class MCUtility
    {
        /// <summary>
        /// Interpolates the vertex's position.
        /// p - corner.
        /// v - density.
        /// isolevel - The density level where a surface will be created.
        /// Densities below this will be inside the surface (solid),
        /// and densities above this will be outside the surface (air)
        /// </summary>
        /// <returns>The interpolated vertex's position</returns>
        public static float3 VertexInterpolate(float3 p1, float3 p2, float v1, float v2, float isolevel)
        {
            return p1 + (isolevel - v1) * (p2 - p1) / (v2 - v1);
        }

        public static float HueInterpolate(float c1, float c2, float v1, float v2, float isoLevel)
        {
            return c1 + (isoLevel - v1) * (c2 - c1) / (v2 - v1);
        }

        public static byte CalculateCubeIndex(MarchingCube<MarchingCubeVertex> marchingCube, byte isoLevel)
        { 
            byte cubeIndex = (byte)math.select(0, 1,   marchingCube.Corner1.Value < isoLevel);
            cubeIndex     |= (byte)math.select(0, 2,   marchingCube.Corner2.Value < isoLevel);
            cubeIndex     |= (byte)math.select(0, 4,   marchingCube.Corner3.Value < isoLevel);
            cubeIndex     |= (byte)math.select(0, 8,   marchingCube.Corner4.Value < isoLevel);
            cubeIndex     |= (byte)math.select(0, 16,  marchingCube.Corner5.Value < isoLevel);
            cubeIndex     |= (byte)math.select(0, 32,  marchingCube.Corner6.Value < isoLevel);
            cubeIndex     |= (byte)math.select(0, 64,  marchingCube.Corner7.Value < isoLevel);
            cubeIndex     |= (byte)math.select(0, 128, marchingCube.Corner8.Value < isoLevel);

            return cubeIndex;
        }

        public static MarchingCube<MarchingCubeVertex> GetColoredMarhingCube(
            ScalarField<ScalarFieldCell> scalarField,
            NativeArray<DistanceFieldCell> distanceField,
            int3 localPosition,
            float cellHeight
        )
        { 
            MarchingCube<MarchingCubeVertex> marchingCube = new MarchingCube<MarchingCubeVertex>();
            for (int i = 0; i < 8; i++)
            {
                int3 cubeCorner = localPosition + LookupTables.CubeCorners[i];
                if (scalarField.TryGetData(cubeCorner, out ScalarFieldCell cell))
                {
                    int distanceFieldIndex = 
                        scalarField.GetDistanceIndex(new int2(cubeCorner.x, cubeCorner.z));

                    float3 pos = distanceField[distanceFieldIndex].LocalPos;
                    pos.y = cellHeight * cubeCorner.y;
                    marchingCube[i] = new MarchingCubeVertex(){
                        Pos = pos,
                        Value = cell.Value,
                        Color = cell.MetaballColor
                    };
                }
            }
            return marchingCube;
        }

        public static (PosVertexList, ColorVertexList) GeneratePosColorVertexList(
            MarchingCube<MarchingCubeVertex> marchingCube, 
            int edgeIndex, 
            byte isoLevel
        )
        {
            PosVertexList posList = new PosVertexList();
            ColorVertexList colorList = new ColorVertexList();

            for (int i = 0; i < 12; i++)
            {
                if ((edgeIndex & (1 << i)) == 0) { continue; }

                int edgeStartIndex = LookupTables.EdgeIndexTable[2 * i + 0];
                int edgeEndIndex = LookupTables.EdgeIndexTable[2 * i + 1];

                float3 corner1 = marchingCube[edgeStartIndex].Pos;
                float3 corner2 = marchingCube[edgeEndIndex].Pos;

                float density1 = marchingCube[edgeStartIndex].Value / 255f;
                float density2 = marchingCube[edgeEndIndex].Value / 255f;

                float isoLevelFloat = isoLevel / 255f;

                posList[i] = VertexInterpolate(corner1, corner2, density1, density2, isoLevelFloat);
                colorList[i] = marchingCube[edgeEndIndex].Color;
                if (math.any(marchingCube[edgeStartIndex].Color != marchingCube[edgeEndIndex].Color))
                { 
                    float3 average = new float3(1, 1, 1);
                }
                

                // totalColor += average;
                // indicesList[colorCount] = i;
                // colorCount++;
            }

            // float3 averageColor = totalColor / colorCount;
            // for (int i = 0; i < colorCount; i++)
            // { 
            //     colorList[indicesList[i]] = averageColor;
            // }

            return (posList, colorList);
        }
    }
}