using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using SkyrimIslands.World;
using UnityEngine;
using Verse;

namespace SkyrimIslands.Patches
{
    [HarmonyPatch(typeof(WorldGrid), nameof(WorldGrid.GetGizmos))]
    public static class WorldGrid_SkyIslandSelectGizmoPatch
    {
        private static Texture2D? iconCache;

        private static Texture2D Icon
        {
            get
            {
                iconCache ??= SkyrimIslandsTextureCache.WorldRoutePlannerTex;
                return iconCache;
            }
        }

        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result)
        {
            foreach (Gizmo gizmo in __result)
            {
                yield return gizmo;
            }

            if (PlanetLayer.Selected?.Def != PlanetLayerDefOf.Surface)
            {
                yield break;
            }

            List<SkyIslandMapParent> playerIslands = Find.WorldObjects.AllWorldObjects
                .OfType<SkyIslandMapParent>()
                .Where(static i => i.Faction == Faction.OfPlayer)
                .ToList();

            if (playerIslands.Count == 0)
            {
                yield break;
            }

            Command_Action command = new Command_Action
            {
                defaultLabel = "选中空岛",
                defaultDesc = "选中并定位到玩家的天空岛（不切换层级）",
                icon = Icon,
                action = delegate
                {
                    if (playerIslands.Count == 1)
                    {
                        SelectAndJumpTo(playerIslands[0]);
                    }
                    else
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>();
                        foreach (SkyIslandMapParent island in playerIslands)
                        {
                            SkyIslandMapParent localIsland = island;
                            options.Add(new FloatMenuOption(localIsland.Label, delegate
                            {
                                SelectAndJumpTo(localIsland);
                            }));
                        }
                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                }
            };

            yield return command;
        }

        private static void SelectAndJumpTo(SkyIslandMapParent island)
        {
            if (island == null)
            {
                return;
            }

            Find.WorldSelector.Select(island, false);
            PlanetTile surfaceTile = island.SurfaceProjectionTile;
            if (surfaceTile.Valid)
            {
                Find.WorldCameraDriver.JumpTo(surfaceTile);
            }
            else
            {
                Find.WorldCameraDriver.JumpTo(island.Tile);
            }
        }
    }
}
