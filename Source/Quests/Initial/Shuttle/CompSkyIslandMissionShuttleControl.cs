using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace SkyrimIslands.Quests.Initial.Shuttle
{
    public class CompSkyIslandMissionShuttleControl : ThingComp
    {
        private bool migrationStarted;
        private int launchAvailableTick = -1;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            AcceptanceReport canStart = CanStartLaunchProgram();
            Command_Action command = new Command_Action
            {
                defaultLabel = "启动发射程序",
                defaultDesc = "在帝国穿梭机完成装载后，启动发射程序并执行空岛迁移。",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip"),
                action = delegate
                {
                    BeginLaunchSequence();
                }
            };

            if (!canStart.Accepted)
            {
                command.Disable(canStart.Reason);
            }

            yield return command;
        }

        public void InitializeForMission(int launchAvailableTick)
        {
            this.launchAvailableTick = launchAvailableTick;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref migrationStarted, "migrationStarted", false);
            Scribe_Values.Look(ref launchAvailableTick, "launchAvailableTick", -1);
        }

        private AcceptanceReport CanStartLaunchProgram()
        {
            if (launchAvailableTick < 0 || Find.TickManager.TicksGame < launchAvailableTick)
            {
                return "穿梭机尚未完成降落。";
            }

            if (migrationStarted)
            {
                return "发射程序已经启动。";
            }

            if (parent == null || parent.Destroyed || !parent.Spawned)
            {
                return "穿梭机已经无法起飞。";
            }

            CompTransporter? transporter = parent.TryGetComp<CompTransporter>();
            if (transporter == null || transporter.innerContainer.Count == 0)
            {
                return "请先装载至少一名殖民者和需要带走的物资。";
            }

            if (transporter.AnythingLeftToLoad)
            {
                return "穿梭机仍有尚未完成装载的内容。";
            }

            bool hasColonist = false;
            ThingOwner contents = transporter.innerContainer;
            for (int i = 0; i < contents.Count; i++)
            {
                if (contents[i] is Pawn pawn && pawn.IsColonist)
                {
                    hasColonist = true;
                    break;
                }
            }

            if (!hasColonist)
            {
                return "至少需要装载一名殖民者。";
            }

            return true;
        }

        private void BeginLaunchSequence()
        {
            if (!CanStartLaunchProgram().Accepted || migrationStarted || parent.Map == null)
            {
                return;
            }

            Find.WindowStack.Add(new Dialog_MessageBox(
                "是否在启程后遗弃当前殖民地？\n\n选择“是”会在空岛殖民地建立后放弃当前地表殖民地；选择“否”则保留原殖民地。",
                "是",
                delegate
                {
                    migrationStarted = true;
                    Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                    SkyIslandMigrationUtility.BeginMigrationFromLoadedShuttle(parent.Map, parent, true, parent.Position);
                },
                "否",
                delegate
                {
                    migrationStarted = true;
                    Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                    SkyIslandMigrationUtility.BeginMigrationFromLoadedShuttle(parent.Map, parent, false, parent.Position);
                },
                title: "启动发射程序"));
        }
    }
}
