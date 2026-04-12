using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace SkyrimIslands.Quests.Initial.Shuttle
{
    public class CompSkyIslandMissionTransporter : CompTransporter
    {
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (LoadingInProgressOrReadyToLaunch && innerContainer.Any)
            {
                TaggedString label = AnythingLeftToLoad ? "取消装载" : "卸载";
                TaggedString desc = AnythingLeftToLoad ? "取消当前装载计划。" : "将当前已经装入穿梭机的内容全部卸下。";
                yield return new Command_Action
                {
                    defaultLabel = label,
                    defaultDesc = desc,
                        icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel"),
                        action = delegate
                        {
                            CancelLoad();
                        }
                    };
            }

            yield return new Command_SkyIslandLoadToTransporter
            {
                defaultLabel = LoadingInProgressOrReadyToLaunch ? "调整装载" : "装载穿梭机",
                defaultDesc = "打开穿梭机装载界面，安排要登船的殖民者和物资。",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/LoadTransporter"),
                transComp = this
            };
        }
    }
}
