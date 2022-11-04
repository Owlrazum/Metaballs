using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Jobs;

using MarchingCubes;

public class MetaballMeshGenerator : MonoBehaviour
{
    private const int READ = 0;
    private const int WRITE = 1;

    #region Serialized
    [System.Serializable]
    public class DistanceGridParams
    {
        public int GridResolution = 50;
        public float GridSize = 5;
        [Range(0, 1)]
        public float Threshold = 0.5f;
        public float UsualValue = 0.75f;
        public float MinimalValue = 0.1f;
        public Transform MetaballsParent;
    }
    [SerializeField]
    private DistanceGridParams _distanceGridParams;

    [System.Serializable]
    public class ScalarFieldParams
    {
        public int HeightLayersCount = 5;
        public float CellHeight = 0.03f;
        [Range(0, 255)]
        public byte IsoLevel = 100;
        public float HeightOffset = 0;
        public float Amplitude = 0.5f;
    }
    [SerializeField]
    private ScalarFieldParams _scalarFieldParams;

    [Header("Meshing")]
    [Space]
    [SerializeField]
    private MetaballMeshes _metaballMeshes;

    [SerializeField]
    [Tooltip("Relative to scalar field length")]
    private float MaxIndicesCountFactor = 3;

    [SerializeField]
    private int MaxVerticesCount = 2000;

#if UNITY_EDITOR
    [Header("Debug")]
    [Space]
    [SerializeField]
    private bool _shouldMoveMetaballs = false;

    [SerializeField]
    private bool _shouldDrawGridSize = true;
#endif
    #endregion

    #region PersistentNativeData
    private NativeArray<DistanceFieldCell>[] _distanceFieldDoubleBuffer;
    private NativeArray<DistanceFieldCell> DistanceFieldRead
    {
        get { return _distanceFieldDoubleBuffer[READ]; }
    }
    private NativeArray<DistanceFieldCell> DistanceFieldWrite
    {
        get { return _distanceFieldDoubleBuffer[WRITE]; }
    }

    private ScalarField<ScalarFieldCell>[] _scalarFieldDoubleBuffer;
    private ScalarField<ScalarFieldCell> ScalarFieldRead
    {
        get { return _scalarFieldDoubleBuffer[READ]; }
    }
    private ScalarField<ScalarFieldCell> ScalarFieldWrite
    {
        get { return _scalarFieldDoubleBuffer[WRITE]; }
    }

    private NativeArray<VertexData> _vertices;
    private NativeArray<ushort> _triangles;
    private NativeArray<int> _generatedIndicesCount;
    private NativeArray<ushort> _generatedVerticesCount;

    private NativeHashMap<float3, ushort> _verticesHashMap;

    private int _maxIndicesCount;

    private NativeArray<float3> _metaballsPos;
    private NativeArray<float3> _metaballsColors;
    private NativeArray<float> _metaballsRadius;

    private void GeneratePersistentData()
    {
        InitializeFieldsDoubleBuffers();
        InitializeMetaballsData();
        InitializeNativeMeshData();
    }

    private void InitializeFieldsDoubleBuffers()
    { 
        int cellsCount = _distanceGridParams.GridResolution * _distanceGridParams.GridResolution;
        _distanceFieldDoubleBuffer = new NativeArray<DistanceFieldCell>[2];

        _distanceFieldDoubleBuffer[READ] = new NativeArray<DistanceFieldCell>(cellsCount, Allocator.Persistent);
        _distanceFieldDoubleBuffer[WRITE] = new NativeArray<DistanceFieldCell>(cellsCount, Allocator.Persistent);

        _scalarFieldDoubleBuffer = new ScalarField<ScalarFieldCell>[2];

        _scalarFieldDoubleBuffer[READ] = new ScalarField<ScalarFieldCell>(
            _distanceGridParams.GridResolution,
            _scalarFieldParams.HeightLayersCount,
            _distanceGridParams.GridResolution,
            Allocator.Persistent
        );

        _scalarFieldDoubleBuffer[WRITE] = new ScalarField<ScalarFieldCell>(
            _distanceGridParams.GridResolution,
            _scalarFieldParams.HeightLayersCount,
            _distanceGridParams.GridResolution,
            Allocator.Persistent
        );
    }
    private void InitializeMetaballsData()
    { 
        _metaballsPos    = new NativeArray<float3>(_metaballs.Length, Allocator.Persistent);
        _metaballsColors = new NativeArray<float3>(_metaballs.Length, Allocator.Persistent);
        _metaballsRadius = new NativeArray<float>(_metaballs.Length, Allocator.Persistent);
    }
    private void InitializeNativeMeshData()
    {
        _maxIndicesCount  = (int)(MaxIndicesCountFactor  * ScalarFieldRead.Length);

        _vertices = new NativeArray<VertexData>(MaxVerticesCount, Allocator.Persistent);
        _triangles = new NativeArray<ushort>(_maxIndicesCount, Allocator.Persistent);
        _generatedIndicesCount = new NativeArray<int>(1, Allocator.Persistent);
        _generatedVerticesCount = new NativeArray<ushort>(1, Allocator.Persistent);

        _verticesHashMap = new NativeHashMap<float3, ushort>(MaxVerticesCount, Allocator.Persistent);
    }

    private void DisposePersistentData()
    {
        _distanceFieldDoubleBuffer[READ].Dispose();
        _distanceFieldDoubleBuffer[WRITE].Dispose();
        _scalarFieldDoubleBuffer[READ].Dispose();
        _scalarFieldDoubleBuffer[WRITE].Dispose();

        _vertices.Dispose();
        _triangles.Dispose();
        _generatedIndicesCount.Dispose();
        _generatedVerticesCount.Dispose();

        _verticesHashMap.Dispose();

        _metaballsPos.Dispose();
        _metaballsRadius.Dispose();
        _metaballsColors.Dispose();
    }

    private void SwapGridCellsDoubleBuffer()
    {
        NativeArray<DistanceFieldCell> tmp = _distanceFieldDoubleBuffer[0];
        _distanceFieldDoubleBuffer[0] = _distanceFieldDoubleBuffer[1];
        _distanceFieldDoubleBuffer[1] = tmp;
    }
    private void SwapScalarFieldDoubleBuffer()
    {
        // print("Swapped scalar field");
        ScalarField<ScalarFieldCell> tmp = _scalarFieldDoubleBuffer[0];
        _scalarFieldDoubleBuffer[0] = _scalarFieldDoubleBuffer[1];
        _scalarFieldDoubleBuffer[1] = tmp;
    }
    #endregion

    #region Jobs
    private JobHandle _distanceGridJobHandle;
    private JobHandle _prevDistanceGridJobHandle;
    private JobInitDistanceField _distanceGridJob;

    private JobHandle _scalarFieldJobHandle;
    private JobInitScalarField _scalarFieldJob;

    private JobHandle _generateMeshJobHandle;
    private JobGenerateMesh _generateMeshJob;

    private JobHandle _generateUVJobHandle;
    private JobGenerateUV _generateUVJob;
    #endregion

    private Metaball[] _metaballs;

    private float _cellSize;
    private float3 _rightDelta;  
    private float3 _forwardDelta;

    private MeshUpdateFlags _meshUpdateFlagsDonts =
        MeshUpdateFlags.DontValidateIndices |
        MeshUpdateFlags.DontResetBoneBounds |
        MeshUpdateFlags.DontNotifyMeshUsers |
        MeshUpdateFlags.DontRecalculateBounds;


    private void Awake()
    {
        InitializeGrid();
    }

    private void InitializeGrid()
    {
        if (!enabled)
        {
            return;
        }
        int distanceFieldLength =
            _distanceGridParams.GridResolution *
            _distanceGridParams.GridResolution;

        _metaballs = new Metaball[_distanceGridParams.MetaballsParent.childCount];
        for (int i = 0; i < _distanceGridParams.MetaballsParent.childCount; i++)
        {
            _distanceGridParams.MetaballsParent.GetChild(i).TryGetComponent(out _metaballs[i]);
        }

        _cellSize = _distanceGridParams.GridSize / _distanceGridParams.GridResolution;
        _rightDelta   = new float3((_distanceGridParams.GridSize - _cellSize) / 2, 0, 0);
        _forwardDelta = new float3(0, 0, (_distanceGridParams.GridSize - _cellSize) / 2);

        GeneratePersistentData();
    }
    public void PlusGridSize(int _gridResolution, float _gridSize)
    {
        //_distanceGridParams.GridResolution += _gridResolution;
        _distanceGridParams.GridSize += _gridSize;
        _cellSize = _distanceGridParams.GridSize / _distanceGridParams.GridResolution;
        // InitializeGrid();
    }

#if UNITY_EDITOR
    private const float COLOR_UPDATE_TIME = 3;
    private float _color_update_timer = 0;

    private void MoveMetaballsUpdate()
    {
        if (!_shouldMoveMetaballs)
        {
            return;
        }

        _color_update_timer += Time.deltaTime;
        bool switchColors = false;
        if (_color_update_timer > COLOR_UPDATE_TIME)
        { 
            _color_update_timer = 0;
            switchColors = true;
        }

        float timeOffset = 0.2f;
        for (int i = 0; i < _metaballs.Length; i++)
        {
            _metaballs[i].MoveCircilar(timeOffset + i * 0.2f);
            if (switchColors)
            {
                _metaballs[i].SetCurrentMetaballColor(UnityEngine.Random.Range(0, 3));
            }
        }
    }
#endif

    private void UpdateMetaballsData()
    {
        for (int i = 0; i < _metaballs.Length; i++)
        {
            _metaballsPos[i] = math.float3(
                _metaballs[i].transform.position.x,
                0,
                _metaballs[i].transform.position.z
            );
            _metaballsColors[i] = MetaballColorConverter.ConvertEnumToFloat3( _metaballs[i].GetMetaballColor());
            _metaballsRadius[i] = _metaballs[i].GetRadius();
        }
    }

    private void ScheduleScalarFieldInitializersJobs()
    {
#if UNITY_EDITOR
        MoveMetaballsUpdate();
#endif

        float3 startCellPos = new float3(transform.position.x, 0, transform.position.z)
            - _rightDelta - _forwardDelta;

        _distanceGridJob = new JobInitDistanceField()
        {
            GridResolution = _distanceGridParams.GridResolution,
            StartCell = startCellPos,
            CellDelta = _cellSize,
            Threshold = _distanceGridParams.Threshold,
            UsualValue = _distanceGridParams.UsualValue,
            MinimalValue = _distanceGridParams.MinimalValue,

            MetaballsPosInput = _metaballsPos,
            MetaballsColorsInput = _metaballsColors,
            MetaballsRadiusInput = _metaballsRadius,
            GridCellsWrite = DistanceFieldWrite
        };
        _distanceGridJobHandle = _distanceGridJob.ScheduleParallel(DistanceFieldWrite.Length, 16, default);//, _prevDistanceGridJobHandle);

        _scalarFieldJob = new JobInitScalarField()
        {
            InputDistanceField = DistanceFieldRead,
            OutputScalarField = ScalarFieldWrite,
            HeightOffset = _scalarFieldParams.HeightOffset,
            Amplitude = _scalarFieldParams.Amplitude
        };
        _scalarFieldJobHandle = _scalarFieldJob.ScheduleParallel(ScalarFieldWrite.Length, 32, default);

#if UNITY_EDITOR
        if (_shouldDrawGridSize)
        {
            Debug.DrawRay(startCellPos, Vector3.up, Color.black);
            Debug.DrawRay(startCellPos + _rightDelta * 2, Vector3.up, Color.red);
            Debug.DrawRay(startCellPos + _forwardDelta * 2, Vector3.up, Color.green);
            Debug.DrawRay(startCellPos + (_rightDelta + _forwardDelta) * 2, Vector3.up, Color.blue);
        }
#endif
    }

    private bool _wasUpdatedOnce;
    private void Update()
    {
        UpdateMetaballsData();
        ScheduleScalarFieldInitializersJobs();

        _generateMeshJob = new JobGenerateMesh()
        {
            IsoLevel = _scalarFieldParams.IsoLevel,
            CellHeight = _scalarFieldParams.CellHeight,

            InputScalarField = ScalarFieldRead,
            InputDistanceField = DistanceFieldRead,

            VerticesHashMap = _verticesHashMap,

            GeneratedIndicesCount = _generatedIndicesCount,
            GeneratedVerticesCount = _generatedVerticesCount,

            OutputVertices = _vertices,
            OutputTriangles = _triangles
        };

        _generateMeshJobHandle = _generateMeshJob.Schedule(_scalarFieldJobHandle);

        _generateUVJob = new JobGenerateUV()
        {
            MaxHeight = _scalarFieldParams.CellHeight * _scalarFieldParams.HeightLayersCount,
            VerticesToModify = _vertices
        };

        JobHandle.ScheduleBatchedJobs();
    }

    private void LateUpdate()
    {
        FinishJobs();
    }

    private void FinishJobs(bool isBeforeDestroy = false)
    {
        // _prevDistanceGridJobHandle = _distanceGridJobHandle;

        _distanceGridJobHandle.Complete();
        _scalarFieldJobHandle.Complete();
        _generateMeshJobHandle.Complete();
        _generateUVJobHandle = _generateUVJob.ScheduleParallel(_generatedVerticesCount[0], 64, _generateMeshJobHandle);
        _generateUVJobHandle.Complete();

        if (isBeforeDestroy)
        {
            return;
        }

        GenerateMeshImmediate();
        
        SwapGridCellsDoubleBuffer();
        SwapScalarFieldDoubleBuffer();

        _wasUpdatedOnce = false;
    }

    public void GenerateMeshImmediate()
    {
        Mesh mesh = _metaballMeshes.GetMesh();

        mesh.SetVertexBufferParams(_generatedVerticesCount[0], VertexData.VertexBufferMemoryLayout);
        mesh.SetIndexBufferParams(_generatedIndicesCount[0], IndexFormat.UInt16);

        mesh.SetVertexBufferData(_vertices, 0, 0, _generatedVerticesCount[0], 0, _meshUpdateFlagsDonts);
        mesh.SetIndexBufferData(_triangles, 0, 0, _generatedIndicesCount[0], _meshUpdateFlagsDonts);

        mesh.subMeshCount = 1;
        SubMeshDescriptor subMesh = new SubMeshDescriptor(
            indexStart: 0, 
            indexCount: _generatedIndicesCount[0]
        );
        mesh.SetSubMesh(0, subMesh);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        _metaballMeshes.AssignMesh(mesh);
        
        _verticesHashMap.Clear();
        _generatedIndicesCount[0] = 0;
        _generatedVerticesCount[0] = 0;
    }

    private void OnDestroy()
    {
        if (!enabled)
        {
            return;
        }

        // FinishJobs(isBeforeDestroy: true);
        DisposePersistentData();
    }
}