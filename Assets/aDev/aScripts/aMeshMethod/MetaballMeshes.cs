using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MetaballMeshes : MonoBehaviour
{
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Mesh _mesh;
    private void Awake()
    {
        TryGetComponent(out _meshFilter);
        TryGetComponent(out _meshRenderer);

        _mesh = _meshFilter.mesh;
        _mesh.MarkDynamic();
    }
    public void AssignMesh(Mesh mesh)
    {
        _meshFilter.mesh = mesh;
    }

    public Mesh GetMesh()
    {
        return _mesh;
    }
}