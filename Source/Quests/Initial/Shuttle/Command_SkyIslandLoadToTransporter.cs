using RimWorld;
using Verse;

namespace SkyrimIslands.Quests.Initial.Shuttle
{
    public class Command_SkyIslandLoadToTransporter : Command
    {
        public CompSkyIslandMissionTransporter transComp = null!;

        public override void ProcessInput(UnityEngine.Event ev)
        {
            base.ProcessInput(ev);
            Find.WindowStack.Add(new Dialog_SkyIslandLoadTransporters(transComp.Map, transComp));
        }
    }
}
