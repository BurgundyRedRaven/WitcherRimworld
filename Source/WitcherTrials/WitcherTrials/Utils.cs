using RimWorld;
using Verse;
using System.Linq;

namespace WitcherTrials
{
    public static class WitcherDefCache
    {
        public static TraitDef ChildOfDestiny => DefDatabase<TraitDef>.GetNamedSilentFail("ChildOfDestiny");
        public static XenotypeDef WitcherInitiate => DefDatabase<XenotypeDef>.GetNamedSilentFail("Witcher_Initiate");
        public static XenotypeDef WitcherFull => DefDatabase<XenotypeDef>.GetNamedSilentFail("Witcher_Full");
        public static XenotypeDef WitcherLegendary => DefDatabase<XenotypeDef>.GetNamedSilentFail("Witcher_Legendary");
        public static HediffDef TrialBossBuff => DefDatabase<HediffDef>.GetNamedSilentFail("Witcher_TrialBossBuff");
        public static HediffDef TrialTargetHediff => DefDatabase<HediffDef>.GetNamedSilentFail("Witcher_TrialTargetHediff");
        public static GeneDef MutagenicMetabolism => DefDatabase<GeneDef>.GetNamedSilentFail("Witcher_MutagenicMetabolism");
        public static GeneDef ImmortalVitality => DefDatabase<GeneDef>.GetNamedSilentFail("Witcher_ImmortalVitality");
        public static StatDef MutagenTolerance => DefDatabase<StatDef>.GetNamedSilentFail("MutagenTolerance");
        public static HediffDef WitcherToxicity => DefDatabase<HediffDef>.GetNamedSilentFail("Witcher_Toxicity");
        public static HediffDef PetrisPhilterEffect => DefDatabase<HediffDef>.GetNamedSilentFail("WitcherPotion_PetrisPhilterEffect");
        public static StatDef FocusRegen => DefDatabase<StatDef>.GetNamedSilentFail("Witcher_FocusRegen");
        public static HediffDef QuenHediff => DefDatabase<HediffDef>.GetNamedSilentFail("WitcherSign_QuenEffect");
        public static HediffDef YrdenDebuff => DefDatabase<HediffDef>.GetNamedSilentFail("WitcherSign_YrdenDebuff");
        public static ThingDef YrdenTrapDef => DefDatabase<ThingDef>.GetNamedSilentFail("WitcherSign_YrdenTrap");
        public static GeneDef NightVisionGene => DefDatabase<GeneDef>.GetNamedSilentFail("Witcher_NightVision");
    }

    public static class WitcherTrialsUtility
    {
        public static void AwakenAmulet(Map map, ThingDef dormantDef, ThingDef activeDef)
        {
            if (map == null) return;
            foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
            {
                if (pawn.apparel == null) continue;

                Apparel dormant = pawn.apparel.WornApparel.FirstOrDefault(a => a.def == dormantDef);
                if (dormant != null)
                {
                    pawn.apparel.Remove(dormant);
                    dormant.Destroy();

                    Apparel active = (Apparel)ThingMaker.MakeThing(activeDef);
                    pawn.apparel.Wear(active, false);
                    Messages.Message($"{pawn.NameShortColored}'s amulet has awakened!", pawn, MessageTypeDefOf.PositiveEvent);
                }
            }
        }
    }
}