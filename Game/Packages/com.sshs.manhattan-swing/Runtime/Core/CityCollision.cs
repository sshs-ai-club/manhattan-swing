using System;
using System.Collections.Generic;
using System.Numerics;

namespace ManhattanSwing.Core
{
    /// <summary>
    /// Ray casts and sphere push-out against the city's boxes and the flat ground (y = 0).
    /// Boxes are sorted into a grid of square cells so each query only looks at nearby buildings.
    /// This replaces Unity physics: no colliders or GameObjects are created per building.
    /// The ground is flat at height 0.
    /// </summary>
    public sealed class CityCollision : ISwingWorld
    {
        private const float CellSize = 60f;

        private readonly CityBox[] boxes;
        private readonly float minX, minZ;
        private readonly int cellsX, cellsZ;
        private readonly int[] cellStart;    // boxes of cell c are cellItems[cellStart[c] .. cellStart[c + 1])
        private readonly int[] cellItems;
        private readonly int[] visitStamp;   // avoids testing a box twice when it spans several cells
        private int stamp;

        public CityCollision(CityData city)
        {
            boxes = city.Boxes;
            visitStamp = new int[boxes.Length];

            float maxX = 0, maxZ = 0;
            minX = minZ = 0;
            for (int i = 0; i < boxes.Length; i++)
            {
                float reach = Reach(boxes[i]);
                minX = Math.Min(minX, boxes[i].CentreX - reach); maxX = Math.Max(maxX, boxes[i].CentreX + reach);
                minZ = Math.Min(minZ, boxes[i].CentreZ - reach); maxZ = Math.Max(maxZ, boxes[i].CentreZ + reach);
            }
            cellsX = Math.Max(1, (int)Math.Ceiling((maxX - minX) / CellSize));
            cellsZ = Math.Max(1, (int)Math.Ceiling((maxZ - minZ) / CellSize));

            // Two passes: count the boxes in every cell, then fill.
            var counts = new int[cellsX * cellsZ + 1];
            for (int i = 0; i < boxes.Length; i++)
                ForEachCellOf(boxes[i], cell => counts[cell + 1]++);
            for (int c = 0; c < cellsX * cellsZ; c++) counts[c + 1] += counts[c];
            cellStart = counts;
            cellItems = new int[cellStart[cellsX * cellsZ]];
            var next = (int[])cellStart.Clone();
            for (int i = 0; i < boxes.Length; i++)
            {
                int index = i;
                ForEachCellOf(boxes[i], cell => cellItems[next[cell]++] = index);
            }
        }

        /// <summary>True when the point is inside the city's edge, or within <paramref name="margin"/> metres of it.</summary>
        public bool IsInPlayArea(Vector3 position, float margin)
        {
            float x = position.X, z = position.Z;
            return x >= minX - margin && z >= minZ - margin &&
                   x <= minX + cellsX * CellSize + margin && z <= minZ + cellsZ * CellSize + margin;
        }

        public float GroundHeight(Vector3 near) { return 0; }

        /// <summary>The city's edge seen from above: the area <see cref="IsInPlayArea"/> uses with no margin.</summary>
        public void GetPlayArea(out float minimumX, out float minimumZ, out float maximumX, out float maximumZ)
        {
            minimumX = minX; minimumZ = minZ;
            maximumX = minX + cellsX * CellSize; maximumZ = minZ + cellsZ * CellSize;
        }

        /// <summary>Finds the first building or ground hit along a ray. <paramref name="direction"/> must have length 1.</summary>
        public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out RayHit hit)
        {
            hit = new RayHit { Distance = maxDistance, BoxIndex = -1 };
            bool found = false;

            if (direction.Y < -1e-6f && origin.Y > 0)
            {
                float t = -origin.Y / direction.Y;
                if (t < hit.Distance)
                {
                    hit.Distance = t; hit.Normal = Vector3.UnitY; found = true;
                }
            }

            // Walk the grid cells the ray passes through, nearest first (a 2D DDA).
            stamp++;
            int cx = CellX(origin.X), cz = CellZ(origin.Z);
            int stepX = direction.X > 0 ? 1 : -1, stepZ = direction.Z > 0 ? 1 : -1;
            float nextBoundaryX = minX + (cx + (stepX > 0 ? 1 : 0)) * CellSize;
            float nextBoundaryZ = minZ + (cz + (stepZ > 0 ? 1 : 0)) * CellSize;
            float tMaxX = Math.Abs(direction.X) > 1e-6f ? (nextBoundaryX - origin.X) / direction.X : float.PositiveInfinity;
            float tMaxZ = Math.Abs(direction.Z) > 1e-6f ? (nextBoundaryZ - origin.Z) / direction.Z : float.PositiveInfinity;
            float tDeltaX = Math.Abs(direction.X) > 1e-6f ? CellSize / Math.Abs(direction.X) : float.PositiveInfinity;
            float tDeltaZ = Math.Abs(direction.Z) > 1e-6f ? CellSize / Math.Abs(direction.Z) : float.PositiveInfinity;
            float cellEntry = 0;

            while (cellEntry <= hit.Distance)
            {
                if (cx >= 0 && cx < cellsX && cz >= 0 && cz < cellsZ)
                {
                    int cell = cz * cellsX + cx;
                    for (int k = cellStart[cell]; k < cellStart[cell + 1]; k++)
                    {
                        int i = cellItems[k];
                        if (visitStamp[i] == stamp) continue;
                        visitStamp[i] = stamp;
                        float t; Vector3 normal;
                        if (RayBox(boxes[i], origin, direction, out t, out normal) && t < hit.Distance)
                        {
                            hit.Distance = t; hit.Normal = normal; hit.BoxIndex = i; found = true;
                        }
                    }
                }
                if (tMaxX < tMaxZ) { cellEntry = tMaxX; tMaxX += tDeltaX; cx += stepX; }
                else { cellEntry = tMaxZ; tMaxZ += tDeltaZ; cz += stepZ; }
                if (float.IsInfinity(cellEntry)) break;
            }

            hit.Point = origin + direction * hit.Distance;
            hit.CanHold = hit.BoxIndex >= 0;
            return found;
        }

        /// <summary>
        /// Pushes a sphere out of every building and the ground it overlaps.
        /// </summary>
        /// <param name="centre">Sphere centre; moved to a free position.</param>
        /// <param name="footDrop">
        /// How far below the sphere the body's feet are. Roofs and the ground hold the sphere this much
        /// higher, so a sphere at eye level stands at eye height instead of lying on the floor.
        /// </param>
        /// <param name="contacts">Receives one outward normal per surface touched.</param>
        /// <returns>True when anything was touched.</returns>
        public bool ResolveSphere(ref Vector3 centre, float radius, float footDrop, List<Vector3> contacts)
        {
            bool touched = false;
            float reach = radius + footDrop;

            for (int pass = 0; pass < 3; pass++)
            {
                bool moved = false;
                int x0 = CellX(centre.X - radius), x1 = CellX(centre.X + radius);
                int z0 = CellZ(centre.Z - radius), z1 = CellZ(centre.Z + radius);
                stamp++;
                for (int cz = Math.Max(z0, 0); cz <= Math.Min(z1, cellsZ - 1); cz++)
                for (int cx = Math.Max(x0, 0); cx <= Math.Min(x1, cellsX - 1); cx++)
                {
                    int cell = cz * cellsX + cx;
                    for (int k = cellStart[cell]; k < cellStart[cell + 1]; k++)
                    {
                        int i = cellItems[k];
                        if (visitStamp[i] == stamp) continue;
                        visitStamp[i] = stamp;
                        Vector3 normal;
                        if (PushOutOfBox(boxes[i], ref centre, radius, footDrop, out normal))
                        {
                            if (contacts != null) contacts.Add(normal);
                            touched = moved = true;
                        }
                    }
                }
                if (!moved) break;
            }

            if (centre.Y < reach)
            {
                centre.Y = reach;
                if (contacts != null) contacts.Add(Vector3.UnitY);
                touched = true;
            }
            return touched;
        }

        // --- One box ------------------------------------------------------------------

        private static bool RayBox(CityBox box, Vector3 origin, Vector3 direction, out float distance, out Vector3 normal)
        {
            distance = 0; normal = Vector3.Zero;
            float ox, oz, dx, dz;
            box.ToLocal(origin.X - box.CentreX, origin.Z - box.CentreZ, out ox, out oz);
            box.ToLocal(direction.X, direction.Z, out dx, out dz);

            // Slab test: find where the ray is inside all three pairs of faces at once.
            float tEnter = 0, tExit = float.PositiveInfinity;
            int enterAxis = -1; float enterSign = 0;
            if (!Slab(ox, dx, -box.HalfX, box.HalfX, 0, ref tEnter, ref tExit, ref enterAxis, ref enterSign)) return false;
            if (!Slab(origin.Y, direction.Y, 0, box.Height, 1, ref tEnter, ref tExit, ref enterAxis, ref enterSign)) return false;
            if (!Slab(oz, dz, -box.HalfZ, box.HalfZ, 2, ref tEnter, ref tExit, ref enterAxis, ref enterSign)) return false;
            if (enterAxis < 0) return false;   // the ray starts inside the box

            distance = tEnter;
            if (enterAxis == 1) normal = new Vector3(0, enterSign, 0);
            else
            {
                float wx, wz;
                box.ToWorld(enterAxis == 0 ? enterSign : 0, enterAxis == 2 ? enterSign : 0, out wx, out wz);
                normal = new Vector3(wx, 0, wz);
            }
            return true;
        }

        private static bool Slab(float origin, float direction, float min, float max, int axis,
            ref float tEnter, ref float tExit, ref int enterAxis, ref float enterSign)
        {
            if (Math.Abs(direction) < 1e-8f) return origin >= min && origin <= max;
            float t0 = (min - origin) / direction, t1 = (max - origin) / direction;
            float sign = -1;                       // entering through the min face: normal points to -axis
            if (t0 > t1) { float swap = t0; t0 = t1; t1 = swap; sign = 1; }
            if (t0 > tEnter) { tEnter = t0; enterAxis = axis; enterSign = sign; }
            if (t1 < tExit) tExit = t1;
            return tEnter <= tExit;
        }

        private static bool PushOutOfBox(CityBox box, ref Vector3 centre, float radius, float footDrop, out Vector3 normal)
        {
            normal = Vector3.Zero;
            float top = box.Height + footDrop;
            if (centre.Y - radius >= top) return false;

            float lx, lz;
            box.ToLocal(centre.X - box.CentreX, centre.Z - box.CentreZ, out lx, out lz);
            float nearX = Clamp(lx, -box.HalfX, box.HalfX);
            float nearZ = Clamp(lz, -box.HalfZ, box.HalfZ);
            float nearY = Clamp(centre.Y, 0, top);
            float ex = lx - nearX, ey = centre.Y - nearY, ez = lz - nearZ;
            float distanceSquared = ex * ex + ey * ey + ez * ez;
            if (distanceSquared >= radius * radius) return false;

            float pushX, pushY, pushZ, push;
            if (distanceSquared > 1e-10f)
            {
                // The centre is outside the box: push straight away from the nearest point on it.
                float distance = (float)Math.Sqrt(distanceSquared);
                pushX = ex / distance; pushY = ey / distance; pushZ = ez / distance;
                push = radius - distance;
            }
            else
            {
                // The centre is inside the box: leave through the nearest face.
                float toSideX = box.HalfX - Math.Abs(lx), toSideZ = box.HalfZ - Math.Abs(lz), toTop = top - centre.Y;
                pushX = pushY = pushZ = 0;
                if (toTop <= toSideX && toTop <= toSideZ) { pushY = 1; push = toTop + radius; }
                else if (toSideX <= toSideZ) { pushX = lx >= 0 ? 1 : -1; push = toSideX + radius; }
                else { pushZ = lz >= 0 ? 1 : -1; push = toSideZ + radius; }
            }

            float wx, wz;
            box.ToWorld(pushX, pushZ, out wx, out wz);
            normal = new Vector3(wx, pushY, wz);
            centre += normal * push;
            return true;
        }

        // --- Grid helpers -------------------------------------------------------------

        private static float Reach(CityBox box)
        {
            return (float)Math.Sqrt(box.HalfX * box.HalfX + box.HalfZ * box.HalfZ);
        }

        private int CellX(float x) { return (int)Math.Floor((x - minX) / CellSize); }
        private int CellZ(float z) { return (int)Math.Floor((z - minZ) / CellSize); }

        private void ForEachCellOf(CityBox box, Action<int> action)
        {
            float reach = Reach(box);
            int x0 = Math.Max(CellX(box.CentreX - reach), 0), x1 = Math.Min(CellX(box.CentreX + reach), cellsX - 1);
            int z0 = Math.Max(CellZ(box.CentreZ - reach), 0), z1 = Math.Min(CellZ(box.CentreZ + reach), cellsZ - 1);
            for (int cz = z0; cz <= z1; cz++)
            for (int cx = x0; cx <= x1; cx++)
                action(cz * cellsX + cx);
        }

        private static float Clamp(float value, float min, float max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}
