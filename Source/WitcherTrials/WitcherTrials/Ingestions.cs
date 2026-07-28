using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace WitcherTrials
{
    public class IngestionOutcomeDoer_ToxicPotion : IngestionOutcomeDoer
    {
        public float severity = 0.35f;

        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
        {
            float toxicResistance = pawn.GetStatValue(WitcherDefCache.MutagenTolerance);
            float finalSeverity = severity * (1f - toxicResistance);

            if (finalSeverity > 0f)
            {
                HealthUtility.AdjustSeverity(pawn, WitcherDefCache.WitcherToxicity, finalSeverity);
            }
        }
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats(ThingDef parentDef)
        {
            yield return new StatDrawEntry(
                StatCategoryDefOf.Drug,
                "Witcher Toxicity",
                $"+{(severity * 100f):F0}%",
                "The base amount of Witcher Toxicity this potion inflicts. Witchers with Mutagen Tolerance will resist a percentage of this.",
                2500);
        }
    }

    public class IngestionOutcomeDoer_WhiteHoney : IngestionOutcomeDoer
    {
        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
        {
            var hediffsToRemove = pawn.health.hediffSet.hediffs.Where(h =>
                h.def.defName == "Witcher_Toxicity" ||
                h.def == HediffDefOf.ToxicBuildup ||
                (h.def.defName.StartsWith("WitcherPotion_") && h.def.defName.EndsWith("Effect"))
            ).ToList();

            foreach (Hediff hediff in hediffsToRemove)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }
    }
}