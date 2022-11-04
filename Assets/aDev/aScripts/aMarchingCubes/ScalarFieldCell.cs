using Unity.Mathematics;

namespace MarchingCubes
{ 
    public struct ScalarFieldCell
    {
        public byte Value { get; set; }
        public float3 MetaballColor { get; set; }

        public static ScalarFieldCell Empty(byte value)
        {
            return new ScalarFieldCell()
            {
                Value = 255,
                MetaballColor = 0,
            };
        }

        public static ScalarFieldCell EmptyHalf()
        {
            return new ScalarFieldCell()
            {
                Value = 200,
                MetaballColor = 0
            };
        }
    }
}