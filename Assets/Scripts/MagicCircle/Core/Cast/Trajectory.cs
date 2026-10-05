// Where an instance is after flying for some seconds, from Vòng 4's path and
// speed. Pure. Caster frame, as in SpawnLayout.
//
//   Trajectory.At(new SpawnPoint(0, 2), PathValue.Straight, 10, 1.5);

using System;
using MagicCircleSim.Compiler;
using MagicCircleSim.Design;

namespace MagicCircleSim.Cast
{
    public readonly struct TrajectoryPoint
    {
        public readonly double X, Y, Z;
        /// <summary>Unit heading on the ground plane.</summary>
        public readonly double DirX, DirZ;

        public TrajectoryPoint(double x, double y, double z, double dirX, double dirZ)
        {
            X = x;
            Y = y;
            Z = z;
            DirX = dirX;
            DirZ = dirZ;
        }
    }

    public static class Trajectory
    {
        /// <summary>Instances hover this high above the ground.</summary>
        public const double HoverM = 1;

        public static TrajectoryPoint At(SpawnPoint spawn, PathValue path, double speed, double flightS)
        {
            if (path == PathValue.Straight)
            {
                // A08: every instance flies forward, parallel to the others.
                return new TrajectoryPoint(spawn.X, HoverM, spawn.Z + speed * flightS, 0, 1);
            }
            // V4_MOVE_01: orbit around the cast position. A13 keeps a center instance moving.
            var r0 = Math.Sqrt(spawn.X * spawn.X + spawn.Z * spawn.Z);
            var radius = Math.Max(r0, Defaults.MinOrbitRadiusM);
            var startAzimuth = r0 > 1e-6 ? Math.Atan2(spawn.X, spawn.Z) : 0;
            var azimuth = startAzimuth + speed / radius * flightS;
            return new TrajectoryPoint(radius * Math.Sin(azimuth), HoverM, radius * Math.Cos(azimuth), Math.Cos(azimuth), -Math.Sin(azimuth));
        }
    }
}
