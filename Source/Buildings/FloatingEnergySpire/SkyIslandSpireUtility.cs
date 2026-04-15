using System.Linq;
using Verse;
using SkyrimIslands.World;

namespace SkyrimIslands.Buildings.FloatingEnergySpire
{
    public static class SkyIslandSpireUtility
    {
        public static Building_FloatingEnergySpire? FindFloatingEnergySpire(Map? map)
        {
            if (map == null)
            {
                return null;
            }

            return map.listerThings.ThingsOfDef(SkyrimIslandsDefOf.SkyrimIslands_FloatingEnergySpire)
                .OfType<Building_FloatingEnergySpire>()
                .FirstOrDefault();
        }

        public static Map? GetStartingSkyIslandMap()
        {
            WorldComponent_SkyIslands? skyIslands = Find.World?.GetComponent<WorldComponent_SkyIslands>();
            if (skyIslands?.StartingSkyIsland?.HasMap == true)
            {
                return skyIslands.StartingSkyIsland.Map;
            }

            return Find.Maps.FirstOrDefault(static map => map.Parent is SkyIslandMapParent);
        }
    }
}
