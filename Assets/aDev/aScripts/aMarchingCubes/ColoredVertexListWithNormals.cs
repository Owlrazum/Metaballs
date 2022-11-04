using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;

namespace MarchingCubes
{ 
    /// <summary>
    /// A container for a vertex list with 12 vertices and a MetaballColor
    /// c - vertex
    /// </summary>
    public struct NormalsList : IEnumerable<float3>
    {
        private float3 _n1;
        private float3 _n2;
        private float3 _n3;
        private float3 _n4;
        private float3 _n5;
        private float3 _n6;
        private float3 _n7;
        private float3 _n8;
        private float3 _n9;
        private float3 _n10;
        private float3 _n11;
        private float3 _n12;

        public float3 this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return _n1;
                    case 1: return _n2;
                    case 2: return _n3;
                    case 3: return _n4;
                    case 4: return _n5;
                    case 5: return _n6;
                    case 6: return _n7;
                    case 7: return _n8;
                    case 8: return _n9;
                    case 9: return _n10;
                    case 10: return _n11;
                    case 11: return _n12;
                    default: throw new ArgumentOutOfRangeException($"There are only 12 vertices! You tried to access the vertex at index {index}");
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        _n1 = value;
                        break;
                    case 1:
                        _n2 = value;
                        break;
                    case 2:
                        _n3 = value;
                        break;
                    case 3:
                        _n4 = value;
                        break;
                    case 4:
                        _n5 = value;
                        break;
                    case 5:
                        _n6 = value;
                        break;
                    case 6:
                        _n7 = value;
                        break;
                    case 7:
                        _n8 = value;
                        break;
                    case 8:
                        _n9 = value;
                        break;
                    case 9:
                        _n10 = value;
                        break;
                    case 10:
                        _n11 = value;
                        break;
                    case 11:
                        _n12 = value;
                        break;
                    default: throw new ArgumentOutOfRangeException($"There are only 12 vertices! You tried to access the vertex at index {index}");
                }
            }
        }

        public IEnumerator<float3> GetEnumerator()
        {
            for (int i = 0; i < 12; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}