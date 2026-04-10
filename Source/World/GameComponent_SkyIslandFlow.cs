using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace SkyrimIslands.World
{
    public class GameComponent_SkyIslandFlow : GameComponent
    {
        private const string MigrationQuestTag = "SkyrimIslandsMigrationQuest";
        private static bool startupPromptQueuedThisSession;

        private bool startupPromptSent;

        public GameComponent_SkyIslandFlow(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref startupPromptSent, "startupPromptSent", false);
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();

            if (startupPromptSent || startupPromptQueuedThisSession || !ModsConfig.OdysseyActive)
            {
                return;
            }

            startupPromptSent = true;
            startupPromptQueuedThisSession = true;

            LongEventHandler.ExecuteWhenFinished(delegate
            {
                Map? sourceMap = Find.Maps.FirstOrDefault((Map map) => map.IsPlayerHome);
                if (sourceMap == null)
                {
                    Log.Warning("[Skyrim Islands] Could not find the starting home map.");
                    return;
                }

                if (Find.QuestManager.QuestsListForReading.Any((Quest quest) => quest.tags.Contains(MigrationQuestTag)))
                {
                    return;
                }

                Quest quest2 = Quest.MakeRaw();
                quest2.root = SkyrimIslandsDefOf.SkyrimIslands_MigrationQuest;
                quest2.name = "空岛启程";
                quest2.description = "调查迫降的未知穿梭机，选择同行的殖民者、机械族与物资，随后前往云层上方建立空岛殖民地。";
                quest2.acceptanceExpireTick = Find.TickManager.TicksGame + 15 * 60000;
                quest2.tags.Add(MigrationQuestTag);

                QuestPart_SkyIslandMigration mission = quest2.AddPart<QuestPart_SkyIslandMigration>();
                mission.inSignalEnable = quest2.InitiateSignal;
                mission.sourceMap = sourceMap;
                mission.landingCell = DropCellFinder.GetBestShuttleLandingSpot(sourceMap, Faction.OfPlayer);
                mission.signalListenMode = QuestPart.SignalListenMode.OngoingOnly;

                Find.LetterStack.ReceiveLetter(
                    "未知穿梭机信号",
                    "殖民地附近侦测到一艘未知穿梭机的迫降信号。它的导航核心中残留着一组指向云层上方的坐标。你可以调查这艘穿梭机，并尝试启程前往空岛。",
                    LetterDefOf.NeutralEvent,
                    new LookTargets(mission.landingCell, sourceMap));

                Find.QuestManager.Add(quest2);
            });
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

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
    }
}
