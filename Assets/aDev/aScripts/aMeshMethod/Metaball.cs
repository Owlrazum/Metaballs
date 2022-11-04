using Unity.Mathematics;
using UnityEngine;

public enum MetaballColor
{ 
    Yellow,
    Blue, 
    Red
}

public class Metaball : MonoBehaviour
{
    [SerializeField]
    private MetaballColor _currentMetaballColor;
    
    public float _radius = 0.5f;

    private Vector3 _initialPos;

    private void Awake()
    {
        _initialPos = transform.position;
    }

    public void MoveCircilar(float timeOffset)
    {
        transform.position = _initialPos + new Vector3(
            Mathf.Cos(Time.time + timeOffset) * 0.4f, 
            transform.position.y, 
            Mathf.Sin(Time.time + timeOffset) * 0.4f
        );
    }

    public void SetCurrentMetaballColor(MetaballColor metaballColor)
    {
        _currentMetaballColor = metaballColor;
    }

    public void SetCurrentMetaballColor(int color)
    {
        switch (color)
        { 
            case 0:
                _currentMetaballColor = MetaballColor.Yellow;
                break;
            case 1:
                _currentMetaballColor = MetaballColor.Red;
                break;
            case 2:
                _currentMetaballColor = MetaballColor.Blue;
                break;
        }
    }
    public float GetRadius()
    {
        return _radius;
    }

    public MetaballColor GetMetaballColor()
    {
        return _currentMetaballColor;
    }
}