using Unity.Mathematics;

public static class MetaballColorConverter
{
    public static void AssignYellowColor(float3 color)
    {
        _yellowColor = color;
    }

    public static void AssignBlueColor(float3 color)
    {
        _blueColor = color;
    }

    public static void AssignRedColor(float3 color)
    {
        _redColor = color;
    }


    private static float3 _yellowColor = new float3(60 / 360.0f, 1, 1);
    private static float3 _blueColor = new float3(240 / 360.0f, 1, 1);
    private static float3 _redColor = new float3(0, 1, 1);
    public static float3 ConvertEnumToFloat3(MetaballColor enumColor)
    {
        switch (enumColor)
        { 
            case MetaballColor.Yellow:
                return _yellowColor;
            case MetaballColor.Blue:
                return _blueColor;
            case MetaballColor.Red:
                return _redColor;
        }

        return float3.zero;
    }
}