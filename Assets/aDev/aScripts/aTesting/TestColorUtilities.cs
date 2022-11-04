using UnityEngine;
using UnityEngine.Assertions;
using Unity.Mathematics;

public class TestColorUtilities : MonoBehaviour
{
    private void Start()
    {
        AssertUtilities.AreApproximatelyEqual( new float3(0, 0, 0), 
                        ColorUtilities.RGB2HSL(new float3(0, 0, 0)));
        AssertUtilities.AreApproximatelyEqual( new float3(0, 0, 1), 
                        ColorUtilities.RGB2HSL(new float3(1, 1, 1)));
        AssertUtilities.AreApproximatelyEqual( new float3(0, 1, 0.5f), 
                        ColorUtilities.RGB2HSL(new float3(1, 0, 0)));

        AssertUtilities.AreApproximatelyEqual( new float3(120, 1, 0.5f), 
                        ColorUtilities.RGB2HSL(new float3(0, 1, 0)));
        AssertUtilities.AreApproximatelyEqual( new float3(240, 1, 0.5f), 
                        ColorUtilities.RGB2HSL(new float3(0, 0, 1)));
    }
}