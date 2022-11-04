using UnityEngine;

public class DebugMoveMetaball : MonoBehaviour
{
    private Vector3 _initialPos;

    private void Awake()
    {
        _initialPos = transform.position;
    }

    public void Move(float timeOffset)
    {
        transform.position = _initialPos + new Vector3(
            Mathf.Cos(Time.time + timeOffset) * 0.4f, 
            transform.position.y, 
            Mathf.Sin(Time.time + timeOffset) * 0.4f
        );
    }
}
