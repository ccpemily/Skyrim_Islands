using RimWorld.Planet;

namespace SkyrimIslands.World.Movement
{
    public readonly struct MovementTickOutput
    {
        public readonly MovementTickResult Result;
        public readonly PlanetTile NewSurfaceProjectionTile;

        public MovementTickOutput(MovementTickResult result, PlanetTile newSurfaceProjectionTile)
        {
            Result = result;
            NewSurfaceProjectionTile = newSurfaceProjectionTile;
        }
    }
}
