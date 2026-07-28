using RimWorld;
using RimWorld.Planet;
using System.Linq;
using Verse;

namespace WitcherTrials
{
    public class Hediff_TrialOfTheGrasses : Hediff_WitcherTrialBase
    {
        protected override XenotypeDef TargetXenotype => WitcherDefCache.WitcherInitiate;
        protected override string SuccessMessage => $"{pawn.NameShortColored} survived the Trial of the Grasses and is now a Witcher Initiate.";

        protected override void HandleSeverityTick()
        {
            if (pawn.IsHashIntervalTick(2500))
            {
                bool isChildOfDestiny = WitcherDefCache.ChildOfDestiny != null && pawn.story?.traits?.HasTrait(WitcherDefCache.ChildOfDestiny) == true;

                if (pawn.ageTracker.AgeBiologicalYears > 13)
                {
                    this.Severity += isChildOfDestiny ? 0.0005f : 0.001f;
                }
                if (isChildOfDestiny)
                {
                    this.Severity -= 0.002f;
                }
            }
        }
    }

    public class Hediff_TrialOfDreams : Hediff_WitcherTrialBase
    {
        protected override XenotypeDef TargetXenotype => WitcherDefCache.WitcherFull;
        protected override string SuccessMessage => $"{pawn.NameShortColored} has survived the trial and is now a full Witcher.";

        protected override void ApplyGenes(XenotypeDef def)
        {
            foreach (GeneDef geneDef in def.genes.Where(g => !pawn.genes.HasGene(g)))
            {
                pawn.genes.AddGene(geneDef, false);
            }
        }
    }

    public class Hediff_ApexMutations : Hediff_WitcherTrialBase
    {
        protected override XenotypeDef TargetXenotype => WitcherDefCache.WitcherLegendary;
        protected override string SuccessMessage => $"{pawn.NameShortColored} survived the apex mutations and is now a legendary witcher.";

        protected override void HandleSeverityTick()
        {
            if (pawn.IsHashIntervalTick(2500))
            {
                bool isChildOfDestiny = WitcherDefCache.ChildOfDestiny != null && pawn.story?.traits?.HasTrait(WitcherDefCache.ChildOfDestiny) == true;
                if (isChildOfDestiny)
                {
                    this.Severity -= 0.005f;
                }
            }
        }
    }

    public class Hediff_TrialTarget : HediffWithComps
    {
        public ThingDef DormantAmuletDef;
        public ThingDef ActiveAmuletDef;

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            Map map = this.pawn.Map ?? this.pawn.MapHeld ?? this.pawn.Corpse?.Map;

            if (map != null && DormantAmuletDef != null && ActiveAmuletDef != null)
            {
                WitcherTrialsUtility.AwakenAmulet(map, DormantAmuletDef, ActiveAmuletDef);

                if (map.Parent is Site site)
                {
                    site.parts.RemoveAll(p => p.def.defName.Contains("Trial"));
                }
                CheckAndSpawnNextSite();
            }
            else if (map == null)
            {
                Log.Error("[WitcherTrials] Couldn't find the map after the death of the target. Amulet not activated.");
            }
        }

        private void CheckAndSpawnNextSite()
        {
            var props = DormantAmuletDef.GetCompProperties<CompProperties_TriggerTrialQuest>();
            if (props == null) return;

            bool hasAmulet = CheckForAmuletGlobally();
            if (!hasAmulet) return;

            if (TileFinder.TryFindNewSiteTile(out PlanetTile planetTile, 7, 25, false, null, -1, false))
            {
                SitePartParams siteParams = new SitePartParams { threatPoints = props.threatPoints };
                Site newSite = SiteMaker.MakeSite(new SitePartDefWithParams[] { new SitePartDefWithParams(props.sitePartDef, siteParams) }, planetTile, Faction.OfMechanoids);
                newSite.customLabel = $"Trial of the Mountain ({props.schoolName})";

                Find.WorldObjects.Add(newSite);
                Find.LetterStack.ReceiveLetter(
                    $"Trial: {props.schoolName}",
                    $"You still possess a dormant {props.schoolName} amulet.\n\nA new trial location has been revealed on the map.",
                    LetterDefOf.PositiveEvent,
                    newSite
                );
            }
        }

        private bool CheckForAmuletGlobally()
        {
            foreach (Map m in Find.Maps.Where(x => x.IsPlayerHome))
            {
                if (m.listerThings.ThingsOfDef(DormantAmuletDef).Any()) return true;
                if (m.mapPawns.FreeColonists.Any(p => p.inventory.innerContainer.Contains(DormantAmuletDef) || p.apparel.WornApparel.Any(a => a.def == DormantAmuletDef))) return true;
            }
            return Find.WorldObjects.Caravans.Where(c => c.IsPlayerControlled).Any(caravan => caravan.AllThings.Any(t => t.def == DormantAmuletDef));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref DormantAmuletDef, "DormantAmuletDef");
            Scribe_Defs.Look(ref ActiveAmuletDef, "ActiveAmuletDef");
        }
    }
}