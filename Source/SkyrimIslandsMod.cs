using Verse;

namespace SkyrimIslands
{
    public sealed class SkyrimIslandsMod : Mod
    {
        public SkyrimIslandsMod(ModContentPack content)
            : base(content)
        {
            Log.Message("[Skyrim Islands] Mod initialized.");
        }
    }
}
