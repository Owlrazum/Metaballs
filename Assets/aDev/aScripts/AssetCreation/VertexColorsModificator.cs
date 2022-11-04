using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class VertexColorsModificator : MonoBehaviour
{
    [SerializeField]
    private float _radius;

    [SerializeField]
    private Color _centerColor = Color.red;

    [SerializeField]
    private Color _outerColor;

    [SerializeField]
    private bool _isNewColors;

    public bool IsSharedMesh;
    public string AssetName;

    public Mesh EditVertexColors()
    {
        print("AAA");
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh toEdit;
        if (IsSharedMesh)
        { 
            toEdit = meshFilter.sharedMesh;
        }
        else
        {
            toEdit = meshFilter.mesh;
        }

        Vector3[] vertices = toEdit.vertices;
        Color[] colors = toEdit.colors;

        if (_isNewColors)
        { 
            colors = new Color[vertices.Length];
        }

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 pos = vertices[i];
            pos.y = 0;

            float lerpParam = pos.magnitude / _radius;
            colors[i] = Color.Lerp(_centerColor, _outerColor, EaseInOut(lerpParam));
        }

        toEdit.colors = colors;
        return toEdit;
    }

    public float EaseIn(float lerpParam)
    {
        return lerpParam * lerpParam;
    }

    public float Flip(float t)
    {
        return 1 - t;
    }

    public float EaseOut(float lerpParam)
    {
        return Flip(EaseIn(Flip(lerpParam)));
    }

    public float EaseInOut(float lerpParam)
    {
        return Mathf.Lerp(EaseIn(lerpParam), EaseOut(lerpParam), lerpParam);
    }
}

