// using System.Collections;
// using Unity.Mathematics;
// using Unity.Collections;
// using UnityEngine;
// using UnityEngine.Rendering;

// using MarchingCubes;

// public class DebugMetaballMeshGenSequence : MonoBehaviour
// {
//     private const int READ = 0;
//     private const int WRITE = 1;

//     private float3 POS_RIGHT   = new float3(1, 0, 0);
//     private float3 POS_UP      = new float3(0, 1, 0);
//     private float3 POS_FORWARD = new float3(0, 0, 1);

//     #region Serialized
//     [System.Serializable]
//     public class DistanceGridParams
//     {
//         public int2 GridResolution = math.int2(50, 50);
//         public float2 GridSize = math.float2(5, 5);
//         [Range(0, 1)]
//         public float Threshold = 0.5f;
//         public float MetaballRadius = 0.5f;
//         public float IntersectValue = 0.5f;
//         public Transform MetaballsParent;
//     }

//     [SerializeField]
//     private DistanceGridParams _distanceGridParams;

//     [System.Serializable]
//     public class ScalarFieldParams
//     {
//         public int HeightLayersCount = 3;
//         public float StartHeight = 0.5f;
//         public float Amplitude = 1;
//         public float HeightOffset = -0.5f;
//         [Range(0, 255)]
//         public byte IsoLevel = 100;
//     }

//     [SerializeField]
//     private ScalarFieldParams _scalarFieldParams;

//     [Header("Meshing")]
//     [Space]
//     [SerializeField]
//     private MetaballMeshes _metaballMeshes;

//     [Header("Pooling")]
//     [Space]
//     [SerializeField]
//     private GameObject _prefab;

//     [Header("Debug")]
//     [Space]
//     [SerializeField]
//     private float _debugDrawTime = 1000;

//     [SerializeField]
//     private bool _shouldMoveMetaballs = false;
//     [SerializeField]
//     private float3 _dumpPos = math.float3(0, -1000, 0);
//     #endregion

//     #region PersistentNativeData
//     private float3[] _metaballsPos;

//     private DistanceFieldCell[] _distanceField;
//     private DebugScalarField<byte> _scalarField;
    
//     private int _trianglesCount;
//     private VertexData[] _vertices;
//     private ushort[] _triangles;

//     // So it was possible to disable
//     private void Start()
//     { 

//     }

//     private void InitializeContainers()
//     {
//         int cellsCount = _distanceGridParams.GridResolution.x * _distanceGridParams.GridResolution.y;
//         _distanceField = new DistanceFieldCell[cellsCount];

//         _scalarField = new DebugScalarField<byte>(
//             _distanceGridParams.GridResolution.x,
//             _scalarFieldParams.HeightLayersCount,
//             _distanceGridParams.GridResolution.y
//         );

//         int maxLength = 15 * _scalarField.Length;
//         _trianglesCount = 0;
//         _vertices = new VertexData[maxLength];
//         _triangles = new ushort[maxLength];
//     }
//     #endregion

//     private DebugMoveMetaball[] _moveMetaballs;

//     private float _cellSize;

//     private void Awake()
//     {
//         if (!enabled)
//         {
//             return;
//         }
//         _moveMetaballs = new DebugMoveMetaball[_distanceGridParams.MetaballsParent.childCount];
//         for (int i = 0; i < _distanceGridParams.MetaballsParent.childCount; i++)
//         {
//             _distanceGridParams.MetaballsParent.GetChild(i).TryGetComponent(out _moveMetaballs[i]);
//         }
//         _cellSize = _distanceGridParams.GridSize.x / _distanceGridParams.GridResolution.x;
//         InitializeContainers();

//         _meshGenSequence = GenMeshSequence();
//         StartCoroutine(_meshGenSequence);
//     }

//     private void InitializeDistanceField()
//     {
//         _metaballsPos = new float3[_distanceGridParams.MetaballsParent.childCount];
//         for (int i = 0; i < _distanceGridParams.MetaballsParent.childCount; i++)
//         {
//             _metaballsPos[i] = math.float3(
//                 _distanceGridParams.MetaballsParent.GetChild(i).position.x,
//                 0,
//                 _distanceGridParams.MetaballsParent.GetChild(i).position.z
//             );
//         }

//         float3 startCellPos = math.float3(transform.position.x, 0, transform.position.z)
//             + math.float3(-1, 0, -1) * (_distanceGridParams.GridSize.x - _cellSize) / 2;

//         for (int row = 0; row < _distanceGridParams.GridResolution.y; row++)
//         {
//             for (int col = 0; col < _distanceGridParams.GridResolution.x; col++)
//             {
//                 float3 rightDelta = col * _cellSize * math.float3(1, 0, 0);
//                 float3 forwardDelta = row * _cellSize * math.float3(0, 0, 1);
//                 DistanceFieldCell gridCell = new DistanceFieldCell()
//                 {
//                     LocalPos = startCellPos + rightDelta + forwardDelta
//                 };

//                 float alpha = 0;
//                 int metaballCount = 0;
//                 for (int i = 0; i < _metaballsPos.Length; i++)
//                 {
//                     float distance = math.distance(gridCell.LocalPos, _metaballsPos[i]);
//                     if (distance > _distanceGridParams.MetaballRadius)
//                     {
//                         continue;
//                     }

//                     alpha += (_distanceGridParams.MetaballRadius - distance) / _distanceGridParams.MetaballRadius;
//                     metaballCount++;
//                 }

//                 if (metaballCount > 0)
//                 {
//                     alpha = math.clamp(alpha, 0, 1);

//                     // Debug.DrawRay(gridCell.LocalPos, POS_UP * alpha * 4, Color.white, 10);
//                     if (alpha >= _distanceGridParams.Threshold)
//                     {
//                         float lerpParam = Mathf.InverseLerp(_distanceGridParams.Threshold, 1, alpha);
//                         float endValue = metaballCount > 1 ? _distanceGridParams.IntersectValue : 1;
//                         gridCell.Value = Mathf.Lerp(0, endValue, lerpParam);
//                         // Debug.DrawRay(gridCell.LocalPos, POS_UP * gridCell.Value * 0.5f, Color.black, 10);
//                     }
//                     else
//                     {
//                         gridCell.Value = -1;
//                     }
//                 }
//                 else
//                 {
//                     gridCell.Value = -1;
//                 }

//                 _distanceField[col * _distanceGridParams.GridResolution.x + row] = gridCell;
//             }
//         }
//     }

//     private void InitializeScalarField()
//     {
//         MoveMetaballsUpdate();
//         InitializeDistanceField();

//         float heightDelta = 1f / _scalarField.Height;
//         for (int y = 0; y < _scalarField.Height; y++)
//         {
//             float heightLerpParam = ((float)y + 1) / _scalarField.Height;
//             for (int z = 0; z < _scalarField.Depth; z++)
//             {
//                 for (int x = 0; x < _scalarField.Width; x++)
//                 {
//                     int3 scalarFieldIndex = new int3(x, y, z);
//                     int distanceIndex = _scalarField.GetDistanceIndex(new int2(x, z)); 
//                     float distanceFieldData = _distanceField[distanceIndex].Value;
//                     if (distanceFieldData < 0)
//                     {
//                         _scalarField.SetData(0, scalarFieldIndex);
//                         continue;
//                     }

//                     if (math.abs(heightLerpParam - distanceFieldData) <= heightDelta / 2)
//                     {
//                         _scalarField.SetData((byte)(distanceFieldData * 255), scalarFieldIndex);
//                         float3 pos = _distanceField[distanceIndex].LocalPos +
//                             new float3(0, _scalarFieldParams.StartHeight + _cellSize * y, 0);

//                         DrawSolidScalarField(pos);
//                     }
//                     else
//                     {
//                         _scalarField.SetData(0, scalarFieldIndex);
//                     }
//                 }
//             }
//         }
//     }

//     private IEnumerator _meshGenSequence = null;
//     private IEnumerator GenMeshSequence()
//     {
//         while (true)
//         { 
//             InitializeScalarField();
//             GenerateMeshData();
        
//             yield return StartCoroutine(GenerateMeshImmediate());
//         }
//     }

//     private void GenerateMeshData()
//     {
//         for (int y = 0; y < _scalarField.Height - 1; y++)
//         {
//             for (int z = 0; z < _scalarField.Depth - 1; z++)
//             {
//                 for (int x = 0; x < _scalarField.Width - 1; x++)
//                 {
//                     int3 scalarFieldLocalPos = new int3(x, y, z);
//                     int2 distanceFieldIndex = new int2(x, z);

//                     MarchingCube<(float3, byte)> voxelCorners = MCUtility.GetVoxelCorners(
//                         _scalarField,
//                         _distanceField,
//                         scalarFieldLocalPos,
//                         _cellSize
//                     );

//                     byte cubeIndex = MCUtility.CalculateCubeIndex(voxelCorners, _scalarFieldParams.IsoLevel);
//                     if (cubeIndex == 0 || cubeIndex == 255)
//                     {
//                         continue;
//                     }

//                     int edgeIndex = LookupTables.EdgeTable[cubeIndex];

//                     VertexList vertexList = MCUtility.GenerateVertexList(
//                         voxelCorners,
//                         edgeIndex,
//                         _scalarFieldParams.IsoLevel
//                     );

//                     int rowIndex = 15 * cubeIndex;
//                     for (int i = 0; LookupTables.TriangleTable[rowIndex + i] != -1 && i < 15; i += 3)
//                     {
//                         float3 vertex1 = vertexList[LookupTables.TriangleTable[rowIndex + i + 0]];
//                         float3 vertex2 = vertexList[LookupTables.TriangleTable[rowIndex + i + 1]];
//                         float3 vertex3 = vertexList[LookupTables.TriangleTable[rowIndex + i + 2]];

//                         if (!vertex1.Equals(vertex2) && !vertex1.Equals(vertex3) && !vertex2.Equals(vertex3))
//                         {
//                             float3 normal = math.normalize(math.cross(vertex2 - vertex1, vertex3 - vertex1));

//                             int vertexIndex = _trianglesCount++ * 3;

//                             _vertices[vertexIndex + 0] = new VertexData(vertex1, normal);
//                             _triangles[vertexIndex + 0] = (ushort)(vertexIndex + 0);

//                             _vertices[vertexIndex + 1] = new VertexData(vertex2, normal);
//                             _triangles[vertexIndex + 1] = (ushort)(vertexIndex + 1);

//                             _vertices[vertexIndex + 2] = new VertexData(vertex3, normal);
//                             _triangles[vertexIndex + 2] = (ushort)(vertexIndex + 2);
//                         }
//                     }
//                 }
//             }
//         }
//     }

//     NativeArray<VertexData> _verticesNative;
//     NativeArray<ushort> _indicesNative;
//     private IEnumerator GenerateMeshImmediate()
//     {
//         Mesh mesh = new Mesh();
//         SubMeshDescriptor subMesh = new SubMeshDescriptor(0, 0);

//         mesh.SetVertexBufferParams(_trianglesCount * 3, VertexData.VertexBufferMemoryLayout);
//         mesh.SetIndexBufferParams(_trianglesCount * 3, IndexFormat.UInt16);

//         _verticesNative = new NativeArray<VertexData>(_vertices, Allocator.Persistent);
//         _indicesNative = new NativeArray<ushort>(_triangles, Allocator.Persistent);

//         for (int i = 0; i < _trianglesCount * 3; i++)
//         {
//             if (i % 3 == 0 && i != 0)
//             {
//                 Debug.DrawLine(_verticesNative[i - 1].position, _verticesNative[i - 2].position, Color.black, 1);
//                 Debug.DrawLine(_verticesNative[i - 2].position, _verticesNative[i - 3].position, Color.black, 1);
//                 Debug.DrawLine(_verticesNative[i - 3].position, _verticesNative[i - 1].position, Color.black, 1);
//                 yield return null;
//             }
//         }

//         mesh.SetVertexBufferData(_verticesNative, 0, 0, _trianglesCount * 3);
//         mesh.SetIndexBufferData(_indicesNative, 0, 0, _trianglesCount * 3);


//         // mesh.vertices = toAssignPos;
//         // mesh.normals = toAssignNormals;
//         // mesh.triangles = toAssignTriangles;

//         mesh.subMeshCount = 1;
//         subMesh.indexCount = _trianglesCount * 3;
//         mesh.SetSubMesh(0, subMesh);

//         mesh.RecalculateBounds();
//         _metaballMeshes.AssignMesh(mesh);

//         _verticesNative.Dispose();
//         _indicesNative.Dispose();
//         _trianglesCount = 0;
//     }

//     private void DrawSolidScalarField(float3 pos)
//     {
//         // return;
//         float3 right = POS_RIGHT * 0.05f;
//         float3 up = POS_FORWARD * 0.05f;
//         float3 depth = POS_FORWARD * 0.05f;
//         Debug.DrawRay(pos, right, Color.red, _debugDrawTime);
//         Debug.DrawRay(pos, -right, Color.red, _debugDrawTime);

//         Debug.DrawRay(pos, up, Color.red, _debugDrawTime);
//         Debug.DrawRay(pos, -up, Color.red, _debugDrawTime);

//         Debug.DrawRay(pos, depth, Color.red, _debugDrawTime);
//         Debug.DrawRay(pos, -depth, Color.red, _debugDrawTime);
//     }

//     private void DrawEmpty(float3 pos)
//     {
//         return;
//         float3 right = (POS_RIGHT + POS_FORWARD) * 0.05f;
//         float3 up = (POS_FORWARD - POS_RIGHT) * 0.05f;
//         float3 depth = POS_FORWARD * 0.05f;
//         Debug.DrawRay(pos, right, Color.black, _debugDrawTime);
//         Debug.DrawRay(pos, -right, Color.black, _debugDrawTime);

//         Debug.DrawRay(pos, up, Color.black, _debugDrawTime);
//         Debug.DrawRay(pos, -up, Color.black, _debugDrawTime);

//         Debug.DrawRay(pos, depth, Color.black, _debugDrawTime);
//         Debug.DrawRay(pos, -depth, Color.black, _debugDrawTime);
//     }

//     private void MoveMetaballsUpdate()
//     {
//         if (!_shouldMoveMetaballs)
//         {
//             return;
//         }
//         float timeOffset = 0.2f;
//         for (int i = 0; i < _moveMetaballs.Length; i++)
//         {
//             _moveMetaballs[i].Move(timeOffset + i * 0.1f);
//         }
//     }

//     private void OnDestroy()
//     {
//         if (!enabled)
//         {
//             return;
//         }
//         _verticesNative.Dispose();
//         _indicesNative.Dispose();
//     }
// }