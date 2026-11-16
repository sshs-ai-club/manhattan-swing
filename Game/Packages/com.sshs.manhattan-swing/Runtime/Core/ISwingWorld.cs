using System.Collections.Generic;
using System.Numerics;

namespace ManhattanSwing.Core
{
    public struct RayHit
    {
        public float Distance;
        public Vector3 Point;
        public Vector3 Normal;
        /// <summary>True when a web may hold on here: a building, not the street.</summary>
        public bool CanHold;
        /// <summary>For the box city only: index into <see cref="CityData.Boxes"/>, or -1. Other worlds leave it -1.</summary>
        public int BoxIndex;
    }

    /// <summary>
    /// Everything the swing, the aim help and the race need to know about the city. This is the only
    /// link between the game and the map it runs on.
    ///
    /// Two versions exist:
    /// <list type="bullet">
    /// <item><see cref="CityCollision"/>: the light test map's boxes, plain code, used for tests.</item>
    /// <item><c>PhysicsWorld</c> (in ManhattanSwing.Game): Unity colliders, for a map such as the
    /// detailed city in the original repository.</item>
    /// </list>
    /// All positions are in the same metres as the Unity scene: X east, Y up, Z north.
    /// </summary>
    public interface ISwingWorld
    {
        /// <summary>
        /// The first surface along a ray. <paramref name="direction"/> has length 1.
        /// Returns false when nothing is hit within <paramref name="maxDistance"/>.
        /// </summary>
        bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out RayHit hit);

        /// <summary>
        /// Pushes the player's body out of everything it overlaps. The body is a sphere at eye level
        /// with feet <paramref name="footDrop"/> metres below its centre: roofs and the street hold it
        /// that much higher. Adds one outward normal per surface touched to <paramref name="contacts"/>.
        /// </summary>
        bool ResolveSphere(ref Vector3 centre, float radius, float footDrop, List<Vector3> contacts);

        /// <summary>The height of the street near a point. Webs shorten to keep swings above it.</summary>
        float GroundHeight(Vector3 near);

        /// <summary>
        /// False when a player is so far off the map that they should be sent back to the last ring.
        /// <paramref name="margin"/> is extra room around the map's edge, in metres.
        /// </summary>
        bool IsInPlayArea(Vector3 position, float margin);
    }
}
