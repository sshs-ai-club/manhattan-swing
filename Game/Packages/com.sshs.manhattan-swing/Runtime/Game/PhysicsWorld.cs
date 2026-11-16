using System;
using System.Collections.Generic;
using ManhattanSwing.Core;
using UnityEngine;
using Num = System.Numerics;

namespace ManhattanSwing.Game
{
    /// <summary>
    /// Answers the swing's questions about the city with Unity colliders. Use it on a map whose
    /// buildings have colliders, such as the detailed city in the original repository.
    ///
    /// The light test map has its own faster box collision (<see cref="CityCollision"/>); press F4
    /// there to switch to this class and compare.
    ///
    /// Settings a map will probably need to change are the public fields below.
    /// </summary>
    public sealed class PhysicsWorld : ISwingWorld
    {
        /// <summary>Which layers the body collides with and webs can hit. Ignore Raycast is left out.</summary>
        public LayerMask Layers = ~(1 << 2);

        /// <summary>Height of the street in the scene, for a map whose street is flat.</summary>
        public float Street;

        /// <summary>
        /// Height of the street near a point. Webs shorten to keep the bottom of a swing at least
        /// <see cref="SwingSettings.GroundClearance"/> above it. By default this is <see cref="Street"/>;
        /// a map whose street is not flat can replace it with a function that looks the height up.
        /// </summary>
        public Func<Vector3, float> StreetHeight;

        /// <summary>
        /// Whether a web may hold on where a ray hit. The default holds anything except flat street:
        /// a surface facing up within a metre of <see cref="StreetHeight"/>.
        /// </summary>
        public Func<RaycastHit, bool> CanHold;

        /// <summary>
        /// The play area seen from above (only X and Z are used). A player outside it, plus the
        /// margin, is sent back to the last ring. Leave empty (size zero) for no limit.
        /// </summary>
        public Bounds PlayArea;

        private readonly CapsuleCollider probe;
        private readonly Collider[] overlaps = new Collider[64];

        public PhysicsWorld()
        {
            StreetHeight = position => Street;
            CanHold = hit => !(hit.normal.y > 0.7f && hit.point.y < StreetHeight(hit.point) + 1f);

            // A capsule shaped like the body, used only to measure how far it overlaps a wall. It sits
            // on the Ignore Raycast layer, which the queries leave out, so it never hits itself.
            var holder = new GameObject("Swing body probe") { layer = 2, hideFlags = HideFlags.HideInHierarchy };
            UnityEngine.Object.DontDestroyOnLoad(holder);
            probe = holder.AddComponent<CapsuleCollider>();
            probe.isTrigger = true;
            probe.direction = 1;   // along Y
        }

        public bool Raycast(Num.Vector3 origin, Num.Vector3 direction, float maxDistance, out RayHit hit)
        {
            hit = new RayHit { Distance = maxDistance, BoxIndex = -1 };
            RaycastHit unityHit;
            if (!Physics.Raycast(origin.ToUnity(), direction.ToUnity(), out unityHit, maxDistance, Layers,
                    QueryTriggerInteraction.Ignore))
            {
                hit.Point = origin + direction * maxDistance;
                return false;
            }
            hit.Distance = unityHit.distance;
            hit.Point = unityHit.point.ToCore();
            hit.Normal = unityHit.normal.ToCore();
            hit.CanHold = CanHold(unityHit);
            return true;
        }

        public bool ResolveSphere(ref Num.Vector3 centre, float radius, float footDrop, List<Num.Vector3> contacts)
        {
            // The body runs from the eyes down to the feet: a capsule from centre - footDrop to centre.
            probe.radius = radius;
            probe.height = footDrop + 2 * radius;
            probe.center = new Vector3(0, -footDrop / 2, 0);

            bool touched = false;
            for (int pass = 0; pass < 3; pass++)
            {
                Vector3 top = centre.ToUnity(), bottom = top - Vector3.up * footDrop;
                int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, overlaps, Layers, QueryTriggerInteraction.Ignore);
                bool moved = false;
                for (int i = 0; i < count; i++)
                {
                    Collider other = overlaps[i];
                    if (other == probe) continue;
                    Vector3 direction; float distance;
                    if (!Physics.ComputePenetration(probe, centre.ToUnity(), Quaternion.identity,
                            other, other.transform.position, other.transform.rotation, out direction, out distance)) continue;
                    if (distance <= 1e-5f) continue;
                    centre += (direction * distance).ToCore();
                    if (contacts != null) contacts.Add(direction.ToCore());
                    touched = moved = true;
                }
                if (!moved) break;
            }
            return touched;
        }

        public float GroundHeight(Num.Vector3 near) { return StreetHeight(near.ToUnity()); }

        public bool IsInPlayArea(Num.Vector3 position, float margin)
        {
            if (PlayArea.size == Vector3.zero) return true;
            return position.X >= PlayArea.min.x - margin && position.X <= PlayArea.max.x + margin &&
                   position.Z >= PlayArea.min.z - margin && position.Z <= PlayArea.max.z + margin;
        }

        /// <summary>When the scene moves its whole world (a floating origin), the play area moves with it.</summary>
        public void ShiftOrigin(Vector3 delta)
        {
            PlayArea.center += delta;
            Street += delta.y;
        }
    }
}
