using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace WitcherTrials
{
    public class CompProperties_RestraintBed : CompProperties
    {
        public HediffDef hediffToApply;
        public CompProperties_RestraintBed() { compClass = typeof(Comp_RestraintBed); }
    }

    public class Comp_RestraintBed : ThingComp
    {
        public CompProperties_RestraintBed Props => (CompProperties_RestraintBed)props;
        private List<Pawn> restrainedPawns = new List<Pawn>();
        private bool restraintsActive = false;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref restraintsActive, "restraintsActive", false);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;

            yield return new Command_Toggle
            {
                defaultLabel = "Toggle Restraints",
                defaultDesc = "Lock or unlock the physical restraints on this bed.",
                icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true),
                isActive = () => restraintsActive,
                toggleAction = () =>
                {
                    restraintsActive = !restraintsActive;
                    if (!restraintsActive) ReleaseAll();
                }
            };
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!(parent is Building_Bed bed)) return;

            if (!restraintsActive)
            {
                if (restrainedPawns.Count > 0) ReleaseAll();
                return;
            }

            for (int i = restrainedPawns.Count - 1; i >= 0; i--)
            {
                Pawn p = restrainedPawns[i];
                if (p == null || p.CurrentBed() != bed)
                {
                    RemoveHediff(p);
                    restrainedPawns.RemoveAt(i);
                }
            }

            if (bed.CurOccupants != null)
            {
                foreach (Pawn occupant in bed.CurOccupants.Where(o => o != null && !restrainedPawns.Contains(o)))
                {
                    if (!occupant.health.hediffSet.HasHediff(Props.hediffToApply))
                    {
                        occupant.health.AddHediff(Props.hediffToApply);
                    }
                    restrainedPawns.Add(occupant);
                }
            }
        }

        private void ReleaseAll()
        {
            foreach (var pawn in restrainedPawns) RemoveHediff(pawn);
            restrainedPawns.Clear();
        }

        private void RemoveHediff(Pawn p)
        {
            if (p == null) return;
            Hediff hediff = p.health.hediffSet.GetFirstHediffOfDef(Props.hediffToApply);
            if (hediff != null) p.health.RemoveHediff(hediff);
        }
    }

    public class CompProperties_TriggerTrialQuest : CompProperties
    {
        public SitePartDef sitePartDef;
        public float threatPoints = 500f;
        public string schoolName;
        public CompProperties_TriggerTrialQuest() { this.compClass = typeof(CompTriggerTrialQuest); }
    }

    public class CompTriggerTrialQuest : ThingComp
    {
        public CompProperties_TriggerTrialQuest Props => (CompProperties_TriggerTrialQuest)this.props;
        private bool questTriggered = false;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad && !questTriggered && !IsSiteActive(Props.sitePartDef))
            {
                TriggerQuest();
            }
        }

        private void TriggerQuest()
        {
            Map map = this.parent.MapHeld ?? Find.AnyPlayerHomeMap;
            if (map == null || !TileFinder.TryFindNewSiteTile(out PlanetTile planetTile, 7, 25, false, null, -1, false)) return;

            SitePartParams siteParams = new SitePartParams { threatPoints = Props.threatPoints };
            Site site = SiteMaker.MakeSite(new SitePartDefWithParams[] { new SitePartDefWithParams(Props.sitePartDef, siteParams) }, planetTile, Faction.OfMechanoids);
            site.customLabel = $"Trial of the Mountain ({Props.schoolName})";

            Find.WorldObjects.Add(site);
            Find.LetterStack.ReceiveLetter(
                $"Trial: {Props.schoolName}",
                $"Crafting the dormant amulet has revealed a hidden location.\n\nTravel there, overcome the trial, and awaken the {Props.schoolName} amulet.",
                LetterDefOf.PositiveEvent,
                site
            );
            questTriggered = true;
        }

        private bool IsSiteActive(SitePartDef def) => Find.WorldObjects.Sites.Any(s => s.parts.Any(p => p.def == def));

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref questTriggered, "questTriggered", false);
        }
    }

    public class CompProperties_WitcherSet : CompProperties
    {
        public string setTag;
        public HediffDef setHediff;
        public int requiredPieces = 2;
        public CompProperties_WitcherSet() { this.compClass = typeof(CompWitcherSet); }
    }

    public class CompWitcherSet : ThingComp
    {
        public CompProperties_WitcherSet Props => (CompProperties_WitcherSet)this.props;

        public override void CompTick()
        {
            base.CompTick();
            if (this.parent is Apparel apparel && apparel.Wearer != null && apparel.Wearer.IsHashIntervalTick(60))
            {
                int pieces = apparel.Wearer.apparel.WornApparel.Count(w =>
                {
                    var comp = w.GetComp<CompWitcherSet>();
                    return comp != null && comp.Props.setTag == Props.setTag;
                });

                if (pieces >= Props.requiredPieces)
                {
                    if (!apparel.Wearer.health.hediffSet.HasHediff(Props.setHediff))
                    {
                        apparel.Wearer.health.AddHediff(Props.setHediff);
                    }
                }
                else
                {
                    Hediff h = apparel.Wearer.health.hediffSet.GetFirstHediffOfDef(Props.setHediff);
                    if (h != null) apparel.Wearer.health.RemoveHediff(h);
                }
            }
        }
    }

    public class HediffCompProperties_StopBleeding : HediffCompProperties
    {
        public HediffCompProperties_StopBleeding() { compClass = typeof(HediffComp_StopBleeding); }
    }

    public class HediffComp_StopBleeding : HediffComp
    {
        public override void CompPostTick(ref float severityAdjustment)
        {
            if (Pawn.IsHashIntervalTick(60))
            {
                foreach (Hediff hediff in Pawn.health.hediffSet.hediffs.Where(h => h.Bleeding))
                {
                    hediff.Tended(1f, 1f);
                }
            }
        }
    }
}