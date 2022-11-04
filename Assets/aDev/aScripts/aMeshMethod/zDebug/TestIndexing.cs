using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Jobs;

using MarchingCubes;

public class TestIndexing : MonoBehaviour
{
    [SerializeField]
    private int2 _gridResolution;

    [SerializeField]
    private int heightCount;

    [SerializeField]
    private int index;

    private void OnEnable()
    {
        int3 xyz = IndexUtilities.IndexToXyz(index, _gridResolution.x, _gridResolution.y);
        Debug.Log(xyz);
        Debug.Log(IndexUtilities.XyzToIndex(xyz, _gridResolution.x, _gridResolution.y));
    }
}