using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using SkyrimIslands.World;

namespace SkyrimIslands.Quests.Initial
{
    public class GameComponent_SkyIslandFlow : GameComponent
    {
        private const string MigrationQuestTag = "SkyrimIslandsMigrationQuest";
        private const int StartupQuestDelayTicks = 300;
        private static readonly List<DelayedRealtimeAction> delayedRealtimeActions = new List<DelayedRealtimeAction>();

        private bool startupQuestCreated;
        private int startupQuestCreateTick = -1;

        public GameComponent_SkyIslandFlow(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref startupQuestCreated, "startupQuestCreated", false);
            Scribe_Values.Look(ref startupQuestCreateTick, "startupQuestCreateTick", -1);
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();

            if (startupQuestCreated || !ModsConfig.OdysseyActive)
            {
                return;
            }

            startupQuestCreateTick = Find.TickManager.TicksGame + StartupQuestDelayTicks;
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            TryCreateStartupQuest();

            for (int i = delayedRealtimeActions.Count - 1; i >= 0; i--)
            {
                if (Time.realtimeSinceStartup >= delayedRealtimeActions[i].executeAt)
                {
                    Action action = delayedRealtimeActions[i].action;
                    delayedRealtimeActions.RemoveAt(i);
                    action();
                }
            }

            if (!WorldRendererUtility.WorldBackgroundNow)
            {
                return;
            }

            Map? currentMap = Find.CurrentMap;
            if (currentMap?.Parent is not SkyIslandMapParent)
            {
                return;
            }

            PlanetLayer? skyLayer = Find.WorldGrid.FirstLayerOfDef(SkyrimIslandsDefOf.SkyrimIslands_SkyLayer);
            if (skyLayer != null && PlanetLayer.Selected != skyLayer)
            {
                PlanetLayer.Selected = skyLayer;
            }
        }

        public static void ScheduleRealtimeAction(float delaySeconds, Action action)
        {
            delayedRealtimeActions.Add(new DelayedRealtimeAction
            {
                executeAt = Time.realtimeSinceStartup + delaySeconds,
                action = action
            });
        }

        private void TryCreateStartupQuest()
        {
            if (startupQuestCreated || startupQuestCreateTick < 0 || Find.TickManager.TicksGame < startupQuestCreateTick)
            {
                return;
            }

            Map? sourceMap = Find.Maps.FirstOrDefault((Map map) => map.IsPlayerHome);
            if (sourceMap == null)
            {
                return;
            }

            if (Find.QuestManager.QuestsListForReading.Any((Quest quest) => quest.tags.Contains(MigrationQuestTag)))
            {
                startupQuestCreated = true;
                return;
            }

            Quest quest2 = Quest.MakeRaw();
            quest2.root = SkyrimIslandsDefOf.SkyrimIslands_MigrationQuest;
            quest2.name = "空岛启程";
            quest2.description = "殖民地附近侦测到一艘未知穿梭机的迫降信号。接取任务后，它会在殖民地附近降落。";
            quest2.acceptanceExpireTick = -1;
            quest2.tags.Add(MigrationQuestTag);

            QuestPart_SkyIslandMigration mission = quest2.AddPart<QuestPart_SkyIslandMigration>();
            mission.inSignalEnable = quest2.InitiateSignal;
            mission.sourceMap = sourceMap;
            mission.signalListenMode = QuestPart.SignalListenMode.OngoingOnly;

            Find.QuestManager.Add(quest2);
            Find.LetterStack.ReceiveLetter(
                "未知穿梭机信号",
                "殖民地附近侦测到一艘未知穿梭机的迫降信号。接取任务后，它会在殖民地附近降落。\n\n你可以在任务页查看详情并决定是否接受。",
                LetterDefOf.NeutralEvent,
                LookTargets.Invalid,
                quest: quest2);

            startupQuestCreated = true;
            startupQuestCreateTick = -1;
        }

        private struct DelayedRealtimeAction
        {
            public float executeAt;
            public Action action;
        }
    }
}
