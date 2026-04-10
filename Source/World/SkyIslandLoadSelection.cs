using Verse;

namespace SkyrimIslands.World
{
    public sealed class SkyIslandLoadSelection
    {
        public SkyIslandLoadSelection(Thing thing, int count)
        {
            Thing = thing;
            Count = count;
        }

        public Thing Thing { get; }

        public int Count { get; }
    }
}
