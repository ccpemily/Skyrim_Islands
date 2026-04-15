using System.Collections.Generic;
using System.Linq;
using SkyrimIslands.Research.Categories.CloudSea;
using SkyrimIslands.Research.Categories.NodeObservation;
using SkyrimIslands.Research.Categories.Spire;
using Verse;

namespace SkyrimIslands.Research.Categories
{
    public class SkyResearchCategoryRegistry : IExposable
    {
        private SpireResearchCategory spireCategory = new SpireResearchCategory();
        private CloudSeaResearchCategory cloudSeaCategory = new CloudSeaResearchCategory();
        private NodeObservationCategory nodeCategory = new NodeObservationCategory();

        public List<ISkyResearchCategory> All
        {
            get
            {
                return new List<ISkyResearchCategory>
                {
                    spireCategory,
                    cloudSeaCategory,
                    nodeCategory
                };
            }
        }

        public ISkyResearchCategory? Get(SkyIslandDataTypeDef dataType)
        {
            if (dataType == SkyrimIslandsDefOf.SkyrimIslandsData_SpireResearch)
            {
                return spireCategory;
            }

            if (dataType == SkyrimIslandsDefOf.SkyrimIslandsData_CloudSeaResearch)
            {
                return cloudSeaCategory;
            }

            if (dataType == SkyrimIslandsDefOf.SkyrimIslandsData_NodeObservation)
            {
                return nodeCategory;
            }

            return null;
        }

        public void ExposeData()
        {
            Scribe_Deep.Look(ref spireCategory, "spireCategory");
            Scribe_Deep.Look(ref cloudSeaCategory, "cloudSeaCategory");
            Scribe_Deep.Look(ref nodeCategory, "nodeCategory");

            spireCategory ??= new SpireResearchCategory();
            cloudSeaCategory ??= new CloudSeaResearchCategory();
            nodeCategory ??= new NodeObservationCategory();
        }
    }
}
