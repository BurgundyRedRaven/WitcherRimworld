using RimWorld;
using Verse;

namespace WitcherTrials
{
    public class Recipe_TrialOfTheGrasses : Recipe_WitcherSurgeryBase
    {
        protected override bool IsValidForPawn(Pawn pawn)
        {
            XenotypeDef current = pawn.genes.Xenotype;
            return current != WitcherDefCache.WitcherInitiate &&
                   current != WitcherDefCache.WitcherFull &&
                   current != WitcherDefCache.WitcherLegendary;
        }
    }

    public class Recipe_TrialOfDreams : Recipe_WitcherSurgeryBase
    {
        protected override bool IsValidForPawn(Pawn pawn)
        {
            return pawn.genes.Xenotype == WitcherDefCache.WitcherInitiate;
        }
    }

    public class Recipe_ApexMutations : Recipe_WitcherSurgeryBase
    {
        protected override bool IsValidForPawn(Pawn pawn)
        {
            return pawn.genes.Xenotype == WitcherDefCache.WitcherFull;
        }
    }
}