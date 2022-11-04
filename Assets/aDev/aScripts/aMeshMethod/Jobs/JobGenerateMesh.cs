using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace MarchingCubes
{
    [BurstCompile]
    public struct JobGenerateMesh : IJob
    {
        [ReadOnly]
        public ScalarField<ScalarFieldCell> InputScalarField;

        [ReadOnly]
        public NativeArray<DistanceFieldCell> InputDistanceField;

        public NativeArray<VertexData> OutputVertices;

        [WriteOnly]
        public NativeArray<ushort> OutputTriangles;

        public NativeArray<int> GeneratedIndicesCount;
        public NativeArray<ushort> GeneratedVerticesCount;

        public NativeHashMap<float3, ushort> VerticesHashMap;

        public int CurrentColor;

        public float CellHeight;

        /// <summary>
        /// The density level where a surface will be created. Densities below this will be inside the surface (solid),
        /// and densities above this will be outside the surface (air)
        /// </summary>
        public byte IsoLevel;

        public void Execute()
        {
            GeneratedVerticesCount[0] = 0;

            for (int y = 0; y < InputScalarField.Height - 1; y++)
            {
                for (int z = 0; z < InputScalarField.Depth - 1; z++)
                {
                    for (int x = 0; x < InputScalarField.Width - 1; x++)
                    {
                        int3 scalarFieldLocalPos = new int3(x, y, z);

                        MarchingCube<MarchingCubeVertex> marchingCube = MCUtility.GetColoredMarhingCube(
                            InputScalarField,
                            InputDistanceField,
                            scalarFieldLocalPos,
                            CellHeight
                        );

                        byte cubeIndex = MCUtility.CalculateCubeIndex(marchingCube, IsoLevel);
                        if (cubeIndex == 0 || cubeIndex == 255)
                        {
                            continue;
                        }

                        int edgeIndex = LookupTables.EdgeTable[cubeIndex];

                        (PosVertexList pos, ColorVertexList color) vertexListData = MCUtility.GeneratePosColorVertexList(
                            marchingCube,
                            edgeIndex,
                            IsoLevel
                        );

                        int rowIndex = 15 * cubeIndex;

                        for (int i = 0; LookupTables.TriangleTable[rowIndex + i] != -1 && i < 15; i += 3)
                        {
                            float3x3 triangle = new float3x3(
                                vertexListData.pos[LookupTables.TriangleTable[rowIndex + i + 0]],
                                vertexListData.pos[LookupTables.TriangleTable[rowIndex + i + 1]],
                                vertexListData.pos[LookupTables.TriangleTable[rowIndex + i + 2]]
                            );

                            float3 firstColor = vertexListData.color[LookupTables.TriangleTable[rowIndex + i + 0]];
                            float3 secondColor = vertexListData.color[LookupTables.TriangleTable[rowIndex + i + 1]];
                            float3 thirdColor = vertexListData.color[LookupTables.TriangleTable[rowIndex + i + 2]];

                            if (firstColor.x != secondColor.x)
                            {
                                if (thirdColor.x != secondColor.x)
                                {
                                    secondColor.x = firstColor.x;
                                }
                                else
                                {
                                    firstColor.x = secondColor.x;
                                }
                            }
                            else if (secondColor.x != thirdColor.x)
                            { 
                                if (firstColor.x != secondColor.x)
                                {
                                    thirdColor.x = secondColor.x;
                                }
                                else
                                {
                                    secondColor.x = thirdColor.x;
                                }
                            }
                            else if (thirdColor.x != firstColor.x)
                            { 
                                if (thirdColor.x != secondColor.x)
                                {
                                    firstColor.x = thirdColor.x;
                                }
                                else
                                {
                                    thirdColor.x = firstColor.x;
                                }
                            }

                            float3x3 colorTriangle = new float3x3(
                                firstColor,
                                secondColor,
                                thirdColor
                            );

                            float3 normal = math.normalize(math.cross(triangle[1] - triangle[0], triangle[2] - triangle[0]));

                            for (int n = 0; n < 3; n++)
                            {
                                // ROUNDS THE FLOAT3 TO THE 3rd DECIMAL PLACE, OTHERWISE IT'S NOT USABLE AS A KEY
                                float3 vertexHash = math.floor(triangle[n] * 1000f + 0.5f) * 0.001f;

                                int triangleIndex = GeneratedIndicesCount[0] + n;
                                switch (VerticesHashMap.ContainsKey(vertexHash))
                                {
                                    case true:
                                        // ushort existingIndex = VerticesHashMap[vertexHash];
                                        // if (OutputVertices[existingIndex].color.x != colorTriangle[0].x)
                                        // { 
                                        //     OutputVertices[GeneratedVerticesCount[0]] = new VertexData(OutputVertices[existingIndex].position, normal, colorTriangle[n]);
                                        //     OutputTriangles[triangleIndex] = GeneratedVerticesCount[0];
                                        //     GeneratedVerticesCount[0]++;
                                        // }
                                        // else
                                        // { 
                                            OutputTriangles[triangleIndex] = VerticesHashMap[vertexHash];
                                        // }
                                        continue;
                                    case false:
                                        VerticesHashMap.Add(vertexHash, GeneratedVerticesCount[0]);
                                        OutputVertices[GeneratedVerticesCount[0]] = new VertexData(triangle[n], normal, colorTriangle[n], new float2(1, 1));
                                        OutputTriangles[triangleIndex] = GeneratedVerticesCount[0];
                                        GeneratedVerticesCount[0]++;
                                        break;
                                }
                            }

                            GeneratedIndicesCount[0] += 3;
                        }
                    }
                }
            }
        }
    }
}
