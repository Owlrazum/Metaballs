using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;

using UnityEngine;


public class CurrentMetaballColorGiver : MonoBehaviour
{
    [SerializeField]
    private Color _yellowColor;

    [SerializeField]
    private Color _blueColor;

    [SerializeField]
    private Color _redColor;

    private void Awake()
    {
        MetaballColorConverter.AssignYellowColor(ColorUtilities.Color2HSL(_yellowColor));
        MetaballColorConverter.AssignBlueColor(ColorUtilities.Color2HSL(_blueColor));
        MetaballColorConverter.AssignRedColor(ColorUtilities.Color2HSL(_redColor));
    }

    private void OnDestroy()
    { 
    }

    private int GetCurrentMetaballColor()
    {
        return 0;
    }
}
