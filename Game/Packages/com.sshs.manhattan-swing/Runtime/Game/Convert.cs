using UnityEngine;
using Num = System.Numerics;

namespace ManhattanSwing.Game
{
    /// <summary>The game rules use System.Numerics vectors; Unity uses its own. These convert between them.</summary>
    public static class Convert
    {
        public static Vector3 ToUnity(this Num.Vector3 v) { return new Vector3(v.X, v.Y, v.Z); }
        public static Num.Vector3 ToCore(this Vector3 v) { return new Num.Vector3(v.x, v.y, v.z); }
    }
}
