using RimWorld.Planet;

namespace SkyrimIslands.World
{
    public class SkyIslandMapParent : SpaceMapParent
    {
        public override string Label
        {
            get
            {
                if (!string.IsNullOrEmpty(Name))
                {
                    return Name;
                }

                return "Sky Island";
            }
        }
    }
}
