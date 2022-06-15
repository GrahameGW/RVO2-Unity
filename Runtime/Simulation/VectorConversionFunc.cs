using UnityEngine;
using Vector2 = TiercelFoundry.RVO2.Vector2;

namespace TiercelFoundry.RVO2
{
    public static class VectorConversionFunc
    {
        public static Vector2 ToXY(Vector3 vector3)
        {
            return new Vector2(vector3.x, vector3.y);
        }

        public static Vector2 ToXZ(Vector3 vector3)
        {
            return new Vector2(vector3.x, vector3.z);
        }

        public static Vector2 ToYX(Vector3 vector3)
        {
            return new Vector2(vector3.y, vector3.x);
        }

        public static Vector2 ToYZ(Vector3 vector3)
        {
            return new Vector2(vector3.y, vector3.z);
        }

        public static Vector2 ToZX(Vector3 vector3)
        {
            return new Vector2(vector3.z, vector3.x);
        }

        public static Vector2 ToZY(Vector3 vector3)
        {
            return new Vector2(vector3.z, vector3.y);
        }
    }
}