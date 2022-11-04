using Unity.Mathematics;

namespace MarchingCubes
{ 
    public struct DistanceFieldCell
    {
        public float3 LocalPos { get; set; }
        public float3 MetaballColor { get; set; }
        public float Value { get; set; }
    }
}