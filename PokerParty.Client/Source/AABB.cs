using OpenTK.Mathematics;
using System;

namespace PokerParty.Client
{
    public static class AABB
    {
        public static bool Test(Vector3 rayOrigin, Vector3 rayDir, Vector3 lb, Vector3 rt, out float t)
        {
            Vector3 dirfrac = Vector3.Zero;

            // r.dir is unit direction vector of ray
            dirfrac.X = 1.0f / rayDir.X;
            dirfrac.Y = 1.0f / rayDir.Y;
            dirfrac.Z = 1.0f / rayDir.Z;
            // lb is the corner of AABB with minimal coordinates - left bottom, rt is maximal corner
            // r.org is origin of ray
            float t1 = (lb.X - rayOrigin.X) * dirfrac.X;
            float t2 = (rt.X - rayOrigin.X) * dirfrac.X;
            float t3 = (lb.Y - rayOrigin.Y) * dirfrac.Y;
            float t4 = (rt.Y - rayOrigin.Y) * dirfrac.Y;
            float t5 = (lb.Z - rayOrigin.Z) * dirfrac.Z;
            float t6 = (rt.Z - rayOrigin.Z) * dirfrac.Z;

            float tmin = Math.Max(Math.Max(Math.Min(t1, t2), Math.Min(t3, t4)), Math.Min(t5, t6));
            float tmax = Math.Min(Math.Min(Math.Max(t1, t2), Math.Max(t3, t4)), Math.Max(t5, t6));

            // if tmax < 0, ray (line) is intersecting AABB, but the whole AABB is behind us
            if (tmax < 0)
            {
                t = tmax;
                return false;
            }

            // if tmin > tmax, ray doesn't intersect AABB
            if (tmin > tmax)
            {
                t = tmax;
                return false;
            }

            t = tmin;
            return true;
        }
    }
}
