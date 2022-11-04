using System;
using Unity.Mathematics;

/// <summary>
/// A 3-dimensional field of data
/// </summary>
public struct DebugScalarField<T> where T : struct
{
    private T[] _data;

    public int Width { get; }
    public int Height { get; }
    public int Depth { get; }

    public int3 Size => new int3(Width, Height, Depth);
    public int Length => Width * Height * Depth;

    public DebugScalarField(int widthArg, int heightArg, int depthArg)
    {
#if UNITY_EDITOR
        if (widthArg < 0 || heightArg < 0 || depthArg < 0)
        {
            throw new ArgumentException("The dimensions of this scalarField must all be positive!");
        }
#endif

        _data = new T[widthArg * heightArg * depthArg];

        Width = widthArg;
        Height = heightArg;
        Depth = depthArg;
    }

    public DebugScalarField(int3 sizeArg) : this(sizeArg.x, sizeArg.y, sizeArg.z) { }

    public void SetData(T data, int3 localPosition)
    {
        int index = IndexUtilities.XyzToIndex(localPosition, Width, Depth);
        SetData(data, index);
    }

    public void SetData(T data, int x, int y, int z)
    {
        int index = IndexUtilities.XyzToIndex(x, y, z, Width, Depth);
        SetData(data, index);
    }

    public void SetData(T data, int index)
    {
        _data[index] = data;
    }

    public bool TryGetData(int3 localPosition, out T data)
    {
        return TryGetData(localPosition.x, localPosition.y, localPosition.z, out data);
    }

    public bool TryGetData(int x, int y, int z, out T data)
    {
        int index = IndexUtilities.XyzToIndex(x, y, z, Width, Depth);
        return TryGetData(index, out data);
    }

    public bool TryGetData(int index, out T data)
    {
        if (index >= 0 && index < _data.Length)
        {
            data = GetData(index);
            //Debug.Log("D " + data + " scalarField index " + index + " " + _data.Length);
            return true;
        }
        //Debug.Log("===================FALSE===============");
        data = default;
        return false;
    }

    public T GetData(int3 localPosition)
    {
        return GetData(localPosition.x, localPosition.y, localPosition.z);
    }

    public T GetData(int x, int y, int z)
    {
        int index = IndexUtilities.XyzToIndex(x, y, z, Width, Depth);
        return GetData(index);
    }

    public T GetData(int index)
    {
        return _data[index];
    }

    public int GetIndex(int3 localPos)
    {
        return IndexUtilities.XyzToIndex(localPos, Width, Depth);
    }

    public int3 GetXyz(int localPos)
    {
        return IndexUtilities.IndexToXyz(localPos, Width, Depth);
    }

    public int GetHeightIndex(int localPos)
    {
        return IndexUtilities.XyzToY(localPos, Width, Depth);
    }

    public int GetDistanceIndex(int localPos)
    {
        int x = IndexUtilities.XyzToX(localPos, Width);
        int y = IndexUtilities.XyzToZ(localPos, Width, Depth);
        return IndexUtilities.XyToIndex(new int2(x, y), Width);
    }

    public int GetDistanceIndex(int2 xy)
    {
        return IndexUtilities.XyToIndex(xy, Width);
    }
}