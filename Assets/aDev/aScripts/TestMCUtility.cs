// using System.Collections;
// using System.Collections.Generic;

// using Unity.Collections;
// using UnityEngine;
// using MarchingCubes;
// using Unity.Mathematics;
// using UnityEngine.Rendering;


// public class TestMCUtility : MonoBehaviour
// {
//     private float cellSize;

//     [SerializeField]
//     private float gridSize;

//     [SerializeField]
//     private float cellHeight;

//     [SerializeField]
//     [Range(0, 255)]
//     private byte isoLevel;

//     [SerializeField]
//     private float heightOffset;

//     [SerializeField]
//     private float Amplitude;

//     [SerializeField]
//     private float MaxIndicesCountFactor;

//     [SerializeField]
//     private int MaxVerticesCount;

//     [SerializeField]
//     private MetaballMeshes _metaballMeshes;


//     private ScalarField<ScalarFieldCell> scalarField;
//     private NativeArray<DistanceFieldCell> distanceField;


//     #region TempNativeData
//     private NativeArray<VertexData> Vertices;
//     private NativeArray<ushort> Triangles;
//     private NativeArray<int> GeneratedIndicesCount;
//     private NativeArray<ushort> GeneratedVerticesCount;

//     private NativeHashMap<float3, ushort> VerticesHashMap;

//     private int _maxIndicesCount;

//     private void InitializeNativeMeshData()
//     {
//         _maxIndicesCount  = (int)(MaxIndicesCountFactor  * scalarField.Length);

//         Vertices = new NativeArray<VertexData>(MaxVerticesCount, Allocator.Persistent);
//         Triangles = new NativeArray<ushort>(_maxIndicesCount, Allocator.Persistent);
//         GeneratedIndicesCount = new NativeArray<int>(1, Allocator.Persistent);
//         GeneratedVerticesCount = new NativeArray<ushort>(1, Allocator.Persistent);

//         VerticesHashMap = new NativeHashMap<float3, ushort>(MaxVerticesCount, Allocator.Persistent);
//     }
//     #endregion

//     private void Start()
//     {
//         distanceField = new NativeArray<DistanceFieldCell>(16, Allocator.Persistent);
//         scalarField = new ScalarField<ScalarFieldCell>(
//             4,
//             3,
//             4,
//             Allocator.Persistent
//         );

//         cellSize = gridSize / 4.0f;

//         InitDistanceField();
//         InitScalarField();
//         // StartCoroutine(ScalarFieldInitialization());
//         InitializeNativeMeshData();
//         StartCoroutine(GenerateMeshDataDebugCoroutine());
//         // GenerateMeshDataNative();
//         // GenerateMeshImmediate();

//         // distanceField.Dispose();
//         // scalarField.Dispose();
//         VerticesHashMap.Dispose();
//         GeneratedVerticesCount.Dispose();
//         GeneratedIndicesCount.Dispose();
//         Triangles.Dispose();
//         Vertices.Dispose();
//     }

//     private void InitDistanceField()
//     { 
//         float3 rightDelta   = new float3((gridSize - cellSize) / 2, 0, 0);
//         float3 forwardDelta = new float3(0, 0, (gridSize - cellSize) / 2);
//         float3 StartCell = new float3(transform.position.x, 0, transform.position.z)
//             - rightDelta - forwardDelta;

//         float CellDelta = cellSize;

//         float3 red = new float3(0, 1, 1);
//         float3 blue = new float3(0.6f, 1, 1);

//         for (int row = 0; row < 4; row++)
//         {
//             for (int col = 0; col < 4; col++)
//             {
//                 float3 delta   = new float3(col * CellDelta, 0 ,row * CellDelta);
//                 DistanceFieldCell gridCell = new DistanceFieldCell()
//                 {
//                     LocalPos = StartCell + delta,
//                 };


//                 if (row == 0 || row == 3 ||
//                     col == 0 || col == 3)
//                 {
//                     gridCell.Value = 0;
//                     gridCell.MetaballColor = col <= 1 ? red : blue;
//                     distanceField[row * 4 + col] = gridCell;
//                     continue;
//                 }

//                 gridCell.Value = 0.9f;
//                 if (col <= 1)
//                 {
//                     gridCell.MetaballColor = red;
//                 }
//                 else
//                 {
//                     gridCell.MetaballColor = blue;
//                 }

//                 distanceField[row * 4 + col] = gridCell;
//             }
//         }
//     }

//     private void InitScalarField()
//     {
//         for (int index = 0; index < 4 * 4 * 3; index++)
//         {
//             int distanceIndex = scalarField.GetDistanceIndex(index);
//             float distanceFieldData = distanceField[distanceIndex].Value;

//             if (distanceFieldData < 0)
//             {
//                 scalarField.SetData(
//                     ScalarFieldCell.Empty((byte)((1 + distanceFieldData) * 255)),
//                     index
//                 );
//                 continue;
//             }

//             int heightIndex = scalarField.GetHeightIndex(index);
//             float heightLerpParam = ((float)heightIndex + 1) / scalarField.Height;
//             byte value = (byte)(Mathf.Abs(Amplitude * heightLerpParam - 0.5f * distanceFieldData + heightOffset) * 255);
//             ScalarFieldCell scalarFieldCell = new ScalarFieldCell()
//             {
//                 Value = value,
//                 MetaballColor = distanceField[distanceIndex].MetaballColor,
//             };
//             scalarField.SetData(scalarFieldCell, index);
//         }
//     }

//     private IEnumerator GenerateMeshDataDebugCoroutine()
//     {
//         ushort vertexCount = 0;
//         int indexCount = 0;

//         Dictionary<float3, ushort> debugVerticesHashMap = new Dictionary<float3, ushort>();
//         VertexData[] debugVertices = new VertexData[MaxVerticesCount];
//         ushort[] debugTriangles = new ushort[_maxIndicesCount];

//         for (int y = 0; y < scalarField.Height - 1; y++)
//         {
//             for (int z = 0; z < scalarField.Depth - 1; z++)
//             {
//                 for (int x = 0; x < scalarField.Width - 1; x++)
//                 {
//                     int3 scalarFieldLocalPos = new int3(x, y, z);

//                     MarchingCube<MarchingCubeVertex> marchingCube = MCUtility.GetColoredMarhingCube(
//                         scalarField,
//                         distanceField,
//                         scalarFieldLocalPos,
//                         cellHeight
//                     );

//                     byte cubeIndex = MCUtility.CalculateCubeIndex(marchingCube, isoLevel);
//                     if (cubeIndex == 0 || cubeIndex == 255)
//                     {
//                         continue;
//                     }

//                     int edgeIndex = LookupTables.EdgeTable[cubeIndex];

//                     (PosVertexList pos, ColorVertexList color) vertexListData = MCUtility.GeneratePosColorVertexList(
//                         marchingCube,
//                         edgeIndex,
//                         isoLevel
//                     );

//                     int rowIndex = 15 * cubeIndex;

//                     for (int i = 0; LookupTables.TriangleTable[rowIndex + i] != -1 && i < 15; i += 3)
//                     {
//                         float3x3 triangle = new float3x3(
//                             vertexListData.pos[LookupTables.TriangleTable[rowIndex + i + 0]],
//                             vertexListData.pos[LookupTables.TriangleTable[rowIndex + i + 1]],
//                             vertexListData.pos[LookupTables.TriangleTable[rowIndex + i + 2]]
//                         );

//                         float3x3 colorTriangle = new float3x3(
//                             vertexListData.color[LookupTables.TriangleTable[rowIndex + i + 0]],
//                             vertexListData.color[LookupTables.TriangleTable[rowIndex + i + 1]],
//                             vertexListData.color[LookupTables.TriangleTable[rowIndex + i + 2]]
//                         );

//                         // for (int t = 0; t < 3; t++)
//                         // {
//                         //     Color rgb = Color.HSVToRGB(colorTriangle[t].x, colorTriangle[t].y, colorTriangle[t].z);
//                         //     Debug.DrawRay(triangle[t], Vector3.up * 0.1f, rgb, 100, false);
//                         // }


//                         bool areDiff01 = math.any(colorTriangle[0] != colorTriangle[1]);
//                         bool areDiff12 = math.any(colorTriangle[1] != colorTriangle[2]);
//                         bool areDiff20 = math.any(colorTriangle[2] != colorTriangle[0]);

//                         float3 normal = math.normalize(math.cross(triangle[1] - triangle[0], triangle[2] - triangle[0]));

//                         if (areDiff01 || areDiff12 || areDiff20)
//                         {
//                             yield return new WaitForSeconds(0.5f);
//                             float3 v1;
//                             float3 v2;

//                             ushort c;
//                             ushort i1;
//                             ushort i2;
//                             if (areDiff01 && areDiff12)
//                             {
//                                 c = 1;
//                                 i1 = 2;
//                                 i2 = 0;
//                                 v1 = (triangle[1] + triangle[2]) / 2;
//                                 v2 = (triangle[0] + triangle[1]) / 2;
//                             }
//                             else if (areDiff01 && areDiff20)
//                             {
//                                 c = 0;
//                                 i1 = 1;
//                                 i2 = 2;
//                                 v1 = (triangle[0] + triangle[1]) / 2;
//                                 v2 = (triangle[2] + triangle[0]) / 2;
//                             }
//                             else
//                             {
//                                 c = 2;
//                                 i1 = 0;
//                                 i2 = 1;
//                                 v1 = (triangle[2] + triangle[1]) / 2;
//                                 v2 = (triangle[0] + triangle[2]) / 2;
//                             }

//                             for (int n = 0; n < 3; n++)
//                             {
//                                 // ROUNDS THE FLOAT3 TO THE 3rd DECIMAL PLACE, OTHERWISE IT'S NOT USABLE AS A KEY
//                                 float3 vertexHash = math.floor(triangle[n] * 1000f + 0.5f) * 0.001f;
//                                 if (!debugVerticesHashMap.ContainsKey(vertexHash))
//                                 {
//                                     debugVerticesHashMap.Add(vertexHash, vertexCount);
//                                     debugVertices[vertexCount] = new VertexData(triangle[n], normal, colorTriangle[n]);
//                                     vertexCount++;
//                                     break;
//                                 }

//                                 if (n == c)
//                                 {
//                                     c = debugVerticesHashMap[vertexHash];
//                                 }
//                                 else if (n == i1)
//                                 {
//                                     i1 = debugVerticesHashMap[vertexHash];
//                                 }
//                                 else if (n == i2)
//                                 {
//                                     i2 = debugVerticesHashMap[vertexHash];
//                                 }
//                             }

//                             ushort firstIndex_1 = vertexCount++;
//                             debugVertices[firstIndex_1] = new VertexData(v1, normal, debugVertices[c].color);

//                             ushort firstIndex_2 = vertexCount++;
//                             debugVertices[firstIndex_2] = new VertexData(v1, normal, debugVertices[i1].color);

//                             ushort secondIndex_1 = vertexCount++;
//                             debugVertices[secondIndex_1] = new VertexData(v2, normal, debugVertices[c].color);

//                             ushort secondIndex_2 = vertexCount++;
//                             debugVertices[secondIndex_2] = new VertexData(v2, normal, debugVertices[i1].color);

//                             int triangleIndex = indexCount;
//                             debugTriangles[triangleIndex] = c;
//                             debugTriangles[triangleIndex + 1] = firstIndex_1;
//                             debugTriangles[triangleIndex + 2] = secondIndex_1;

//                             Debug.Log("Common");
//                             Debug.DrawLine(debugVertices[c].position, debugVertices[firstIndex_1].position, Color.red, 2, false);
//                             yield return new WaitForSeconds(0.5f);
//                             Debug.DrawLine(debugVertices[firstIndex_1].position, debugVertices[secondIndex_1].position, Color.red, 1.5f, false);
//                             yield return new WaitForSeconds(0.5f);
//                             Debug.DrawLine(debugVertices[secondIndex_1].position, debugVertices[c].position, Color.red, 1, false);
//                             yield return new WaitForSeconds(1);
//                             yield return new WaitForSeconds(1);

//                             indexCount += 3;


//                             triangleIndex = indexCount;
//                             debugTriangles[triangleIndex] = i1;
//                             debugTriangles[triangleIndex + 1] = firstIndex_2;
//                             debugTriangles[triangleIndex + 2] = secondIndex_2;

//                             Debug.Log("i1");
//                             Debug.DrawLine(debugVertices[i1].position, debugVertices[firstIndex_2].position, Color.red, 2, false);
//                             yield return new WaitForSeconds(0.5f);
//                             Debug.DrawLine(debugVertices[firstIndex_2].position, debugVertices[secondIndex_2].position, Color.red, 1.5f, false);
//                             yield return new WaitForSeconds(0.5f);
//                             Debug.DrawLine(debugVertices[secondIndex_2].position, debugVertices[i1].position, Color.red, 1, false);
//                             yield return new WaitForSeconds(1);
//                             yield return new WaitForSeconds(1);

//                             indexCount += 3;


//                             triangleIndex = indexCount;
//                             debugTriangles[triangleIndex] = i2;
//                             debugTriangles[triangleIndex + 1] = i1;
//                             debugTriangles[triangleIndex + 2] = secondIndex_2;

//                             Debug.Log("i2");
//                             Debug.DrawLine(debugVertices[i2].position, debugVertices[i1].position, Color.red, 2, false);
//                             yield return new WaitForSeconds(0.5f);
//                             Debug.DrawLine(debugVertices[i1].position, debugVertices[secondIndex_2].position, Color.red, 1.5f, false);
//                             yield return new WaitForSeconds(0.5f);
//                             Debug.DrawLine(debugVertices[secondIndex_2].position, debugVertices[i2].position, Color.red, 1, false);
//                             yield return new WaitForSeconds(1);
//                             yield return new WaitForSeconds(1);

//                             indexCount += 3;
//                         }
//                         else
//                         {
//                             for (int n = 0; n < 3; n++)
//                             {
//                                 // ROUNDS THE FLOAT3 TO THE 3rd DECIMAL PLACE, OTHERWISE IT'S NOT USABLE AS A KEY
//                                 float3 vertexHash = math.floor(triangle[n] * 1000f + 0.5f) * 0.001f;

//                                 int triangleIndex = indexCount + n;
//                                 switch (debugVerticesHashMap.ContainsKey(vertexHash))
//                                 {
//                                     case true:
//                                         debugTriangles[triangleIndex] = debugVerticesHashMap[vertexHash];
//                                         continue;
//                                     case false:
//                                         debugVerticesHashMap.Add(vertexHash, vertexCount);
//                                         debugVertices[vertexCount] = new VertexData(triangle[n], normal, colorTriangle[n]);
//                                         debugTriangles[triangleIndex] = vertexCount;
//                                         vertexCount++;
//                                         break;
//                                 }
//                             }

//                             indexCount += 3;
//                         }
//                     }
//                 }
//             }
//         }

//         areDisposed = true;
//         scalarField.Dispose();
//         distanceField.Dispose();
//     }

//     private void GenerateMeshDataNative()
//     {
//         GeneratedVerticesCount[0] = 0;

//         for (int y = 0; y < scalarField.Height - 1; y++)
//         {
//             for (int z = 0; z < scalarField.Depth - 1; z++)
//             {
//                 for (int x = 0; x < scalarField.Width - 1; x++)
//                 {
//                     int3 scalarFieldLocalPos = new int3(x, y, z);

//                     MarchingCube<MarchingCubeVertex> marchingCube = MCUtility.GetColoredMarhingCube(
//                         scalarField,
//                         distanceField,
//                         scalarFieldLocalPos,
//                         cellHeight
//                     );

//                     byte cubeIndex = MCUtility.CalculateCubeIndex(marchingCube, isoLevel);
//                     if (cubeIndex == 0 || cubeIndex == 255)
//                     {
//                         continue;
//                     }

//                     int edgeIndex = LookupTables.EdgeTable[cubeIndex];

//                     (PosVertexList pos, ColorVertexList color) vertexListData = MCUtility.GeneratePosColorVertexList(
//                         marchingCube,
//                         edgeIndex,
//                         isoLevel
//                     );

//                     int rowIndex = 15 * cubeIndex;

//                     for (int i = 0; LookupTables.TriangleTable[rowIndex + i] != -1 && i < 15; i += 3)
//                     {
//                         float3x3 triangle = new float3x3(
//                             vertexListData.pos[LookupTables.TriangleTable[rowIndex + i + 0]],
//                             vertexListData.pos[LookupTables.TriangleTable[rowIndex + i + 1]],
//                             vertexListData.pos[LookupTables.TriangleTable[rowIndex + i + 2]]
//                         );

//                         float3x3 colorTriangle = new float3x3(
//                             vertexListData.color[LookupTables.TriangleTable[rowIndex + i + 0]],
//                             vertexListData.color[LookupTables.TriangleTable[rowIndex + i + 1]],
//                             vertexListData.color[LookupTables.TriangleTable[rowIndex + i + 2]]
//                         );

//                         // for (int t = 0; t < 3; t++)
//                         // {
//                         //     Color rgb = Color.HSVToRGB(colorTriangle[t].x, colorTriangle[t].y, colorTriangle[t].z);
//                         //     Debug.DrawRay(triangle[t], Vector3.up * 0.1f, rgb, 100, false);
//                         // }


//                         bool areDiff01 = math.any(colorTriangle[0] != colorTriangle[1]);
//                         bool areDiff12 = math.any(colorTriangle[1] != colorTriangle[2]);
//                         bool areDiff20 = math.any(colorTriangle[2] != colorTriangle[0]);

//                         float3 normal = math.normalize(math.cross(triangle[1] - triangle[0], triangle[2] - triangle[0]));

//                         if (areDiff01 || areDiff12 || areDiff20)
//                         {
//                             float3 v1;
//                             float3 v2;

//                             ushort c;
//                             ushort i1;
//                             ushort i2;
//                             if (areDiff01 && areDiff12)
//                             {
//                                 c = 1;
//                                 i1 = 2;
//                                 i2 = 0;
//                                 v1 = (triangle[1] + triangle[2]) / 2;
//                                 v2 = (triangle[0] + triangle[1]) / 2;
//                             }
//                             else if (areDiff01 && areDiff20)
//                             {
//                                 c = 0;
//                                 i1 = 1;
//                                 i2 = 2;
//                                 v1 = (triangle[0] + triangle[1]) / 2;
//                                 v2 = (triangle[2] + triangle[0]) / 2;
//                             }
//                             else
//                             {
//                                 c = 2;
//                                 i1 = 0;
//                                 i2 = 1;
//                                 v1 = (triangle[2] + triangle[1]) / 2;
//                                 v2 = (triangle[0] + triangle[2]) / 2;
//                             }

//                             for (int n = 0; n < 3; n++)
//                             {
//                                 // ROUNDS THE FLOAT3 TO THE 3rd DECIMAL PLACE, OTHERWISE IT'S NOT USABLE AS A KEY
//                                 float3 vertexHash = math.floor(triangle[n] * 1000f + 0.5f) * 0.001f;
//                                 if (!VerticesHashMap.ContainsKey(vertexHash))
//                                 {
//                                     VerticesHashMap.Add(vertexHash, GeneratedVerticesCount[0]);
//                                     Vertices[GeneratedVerticesCount[0]] = new VertexData(triangle[n], normal, colorTriangle[n]);
//                                     GeneratedVerticesCount[0]++;
//                                     break;
//                                 }

//                                 if (n == c)
//                                 {
//                                     c = VerticesHashMap[vertexHash];
//                                 }
//                                 else if (n == i1)
//                                 {
//                                     i1 = VerticesHashMap[vertexHash];
//                                 }
//                                 else if (n == i2)
//                                 {
//                                     i2 = VerticesHashMap[vertexHash];
//                                 }
//                             }

//                             ushort firstIndex_1 = GeneratedVerticesCount[0]++;
//                             Vertices[firstIndex_1] = new VertexData(v1, normal, Vertices[c].color);

//                             ushort firstIndex_2 = GeneratedVerticesCount[0]++;
//                             Vertices[firstIndex_2] = new VertexData(v1, normal, Vertices[i1].color);

//                             ushort secondIndex_1 = GeneratedVerticesCount[0]++;
//                             Vertices[secondIndex_1] = new VertexData(v2, normal, Vertices[c].color);

//                             ushort secondIndex_2 = GeneratedVerticesCount[0]++;
//                             Vertices[secondIndex_2] = new VertexData(v2, normal, Vertices[i1].color);

//                             int triangleIndex = GeneratedIndicesCount[0];
//                             Triangles[triangleIndex] = c;
//                             Triangles[triangleIndex + 1] = firstIndex_1;
//                             Triangles[triangleIndex + 2] = secondIndex_1;

//                             GeneratedIndicesCount[0] += 3;


//                             triangleIndex = GeneratedIndicesCount[0];
//                             Triangles[triangleIndex] = i1;
//                             Triangles[triangleIndex + 1] = firstIndex_2;
//                             Triangles[triangleIndex + 2] = secondIndex_2;

//                             GeneratedIndicesCount[0] += 3;


//                             triangleIndex = GeneratedIndicesCount[0];
//                             Triangles[triangleIndex] = i2;
//                             Triangles[triangleIndex + 1] = i1;
//                             Triangles[triangleIndex + 2] = secondIndex_2;

//                             GeneratedIndicesCount[0] += 3;
//                         }
//                         else
//                         {
//                             for (int n = 0; n < 3; n++)
//                             {
//                                 // ROUNDS THE FLOAT3 TO THE 3rd DECIMAL PLACE, OTHERWISE IT'S NOT USABLE AS A KEY
//                                 float3 vertexHash = math.floor(triangle[n] * 1000f + 0.5f) * 0.001f;

//                                 int triangleIndex = GeneratedIndicesCount[0] + n;
//                                 switch (VerticesHashMap.ContainsKey(vertexHash))
//                                 {
//                                     case true:
//                                         Triangles[triangleIndex] = VerticesHashMap[vertexHash];
//                                         continue;
//                                     case false:
//                                         VerticesHashMap.Add(vertexHash, GeneratedVerticesCount[0]);
//                                         Vertices[GeneratedVerticesCount[0]] = new VertexData(triangle[n], normal, colorTriangle[n]);
//                                         Triangles[triangleIndex] = GeneratedVerticesCount[0];
//                                         GeneratedVerticesCount[0]++;
//                                         break;
//                                 }
//                             }

//                             GeneratedIndicesCount[0] += 3;
//                         }
//                     }
//                 }
//             }
//         }
//     }

//     public void GenerateMeshImmediate()
//     {
//         Mesh mesh = _metaballMeshes.GetMesh();

//         mesh.SetVertexBufferParams(GeneratedVerticesCount[0], VertexData.VertexBufferMemoryLayout);
//         mesh.SetIndexBufferParams(GeneratedIndicesCount[0], IndexFormat.UInt16);

//         mesh.SetVertexBufferData(Vertices, 0, 0, GeneratedVerticesCount[0], 0, MeshUpdateFlags.DontValidateIndices);
//         mesh.SetIndexBufferData(Triangles, 0, 0, GeneratedIndicesCount[0], MeshUpdateFlags.DontValidateIndices);

//         mesh.subMeshCount = 1;
//         SubMeshDescriptor subMesh = new SubMeshDescriptor(
//             indexStart: 0, 
//             indexCount: GeneratedIndicesCount[0]
//         );
//         mesh.SetSubMesh(0, subMesh);

//         mesh.RecalculateNormals();
//         mesh.RecalculateBounds();

//         _metaballMeshes.AssignMesh(mesh);
//     }





//     private IEnumerator ScalarFieldInitialization()
//     {
//         for (int index = 0; index < 4 * 4 * 3; index++)
//         {
//             int distanceIndex = scalarField.GetDistanceIndex(index);
//             SNGDebug.DrawGridCell(distanceField[distanceIndex].LocalPos, 0.05f, 0.3f);
//             float distanceFieldData = distanceField[distanceIndex].Value;

//             yield return new WaitForSeconds(0.3f);
//             if (distanceFieldData < 0)
//             {
//                 scalarField.SetData(
//                     ScalarFieldCell.Empty((byte)((1 + distanceFieldData) * 255)),
//                     index
//                 );
//                 continue;
//             }

//             int heightIndex = scalarField.GetHeightIndex(index);
//             float heightLerpParam = ((float)heightIndex + 1) / scalarField.Height;
//             byte value = (byte)(Mathf.Abs(Amplitude * heightLerpParam - 0.5f * distanceFieldData + heightOffset) * 255);
//             ScalarFieldCell scalarFieldCell = new ScalarFieldCell()
//             {
//                 Value = value,
//                 MetaballColor = distanceField[distanceIndex].MetaballColor,
//             };
//             scalarField.SetData(scalarFieldCell, index);
//         }

//         distanceField.Dispose();
//         scalarField.Dispose();
//     }

//     private bool areDisposed;
//     private void OnDestroy()
//     {
//         if (!areDisposed)
//         {
//             scalarField.Dispose();
//             distanceField.Dispose();
//         }
//     }
// }
