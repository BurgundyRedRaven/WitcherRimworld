using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace WitcherTrials
{
    //Aard
    public class CompProperties_AbilityAard : CompProperties_AbilityEffect
    {
        public float stunDurationSeconds = 3f;
        public float damageAmount = 15f;
        public CompProperties_AbilityAard() { compClass = typeof(CompAbilityEffect_Aard); }
    }

    public class CompAbilityEffect_Aard : CompAbilityEffect
    {
        public new CompProperties_AbilityAard Props => (CompProperties_AbilityAard)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (target.HasThing && target.Thing is Pawn targetPawn && !targetPawn.Dead)
            {
                float intensity = parent.pawn.GetStatValue(StatDefOf.PsychicSensitivity);

                targetPawn.stances.stunner.StunFor(Mathf.RoundToInt(Props.stunDurationSeconds * intensity * 60f), parent.pawn, false, true);
                targetPawn.TakeDamage(new DamageInfo(DamageDefOf.Blunt, Props.damageAmount * intensity, 0.5f, -1f, parent.pawn));

                FleckMaker.ThrowDustPuffThick(target.Cell.ToVector3Shifted(), parent.pawn.Map, 2f, new Color(0.8f, 0.8f, 0.8f, 0.5f));
            }
        }
    }

    //Igni
    public class CompProperties_AbilityIgni : CompProperties_AbilityEffect
    {
        public float range = 5f;
        public float angle = 45f;
        public float damageAmount = 15f;
        public CompProperties_AbilityIgni() { compClass = typeof(CompAbilityEffect_Igni); }
    }

    public class CompAbilityEffect_Igni : CompAbilityEffect
    {
        public new CompProperties_AbilityIgni Props => (CompProperties_AbilityIgni)props;

        private List<IntVec3> GetAffectedCells(LocalTargetInfo target)
        {
            List<IntVec3> cells = new List<IntVec3>();
            Pawn caster = parent.pawn;
            Map map = caster.Map;
            if (map == null) return cells;

            Vector3 casterPos = caster.Position.ToVector3Shifted();
            Vector3 targetPos = target.Cell.ToVector3Shifted();
            float startAngle = (targetPos - casterPos).AngleFlat();

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(caster.Position, Props.range, true))
            {
                if (!cell.InBounds(map) || !GenSight.LineOfSight(caster.Position, cell, map)) continue;

                Vector3 cellPos = cell.ToVector3Shifted();
                float cellAngle = (cellPos - casterPos).AngleFlat();
                float delta = Mathf.Abs(Mathf.DeltaAngle(startAngle, cellAngle));

                if (delta <= Props.angle / 2f)
                {
                    cells.Add(cell);
                }
            }
            return cells;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Map map = parent.pawn.Map;
            if (map == null) return;

            float intensity = parent.pawn.GetStatValue(StatDefOf.PsychicSensitivity);
            float scaledDamage = Props.damageAmount * intensity;

            foreach (IntVec3 cell in GetAffectedCells(target))
            {
                FleckMaker.ThrowMicroSparks(cell.ToVector3Shifted(), map);
                FleckMaker.ThrowDustPuffThick(cell.ToVector3Shifted(), map, 1f, new Color(1f, 0.5f, 0.1f, 0.7f));

                List<Thing> things = cell.GetThingList(map);
                for (int i = things.Count - 1; i >= 0; i--)
                {
                    Thing t = things[i];
                    if (t is Pawn p && !p.Dead)
                    {
                        p.TakeDamage(new DamageInfo(DamageDefOf.Flame, scaledDamage, 0.5f, -1f, parent.pawn));
                    }
                }

                if (Rand.Value < 0.3f)
                {
                    FilthMaker.TryMakeFilth(cell, map, ThingDefOf.Filth_Ash);
                }
            }
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            GenDraw.DrawFieldEdges(GetAffectedCells(target), Color.red);
        }
    }

    // Quen
    public class CompProperties_AbilityQuen : CompProperties_AbilityEffect
    {
        public float shieldEnergy = 50f;
        public CompProperties_AbilityQuen() { compClass = typeof(CompAbilityEffect_Quen); }
    }

    public class CompAbilityEffect_Quen : CompAbilityEffect
    {
        public new CompProperties_AbilityQuen Props => (CompProperties_AbilityQuen)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;

            if (caster != null)
            {
                float intensity = caster.GetStatValue(StatDefOf.PsychicSensitivity);

                Hediff_Quen hediff = (Hediff_Quen)HediffMaker.MakeHediff(WitcherDefCache.QuenHediff, caster);
                hediff.energy = Props.shieldEnergy * intensity;

                HediffComp_Disappears disappears = hediff.TryGetComp<HediffComp_Disappears>();
                if (disappears != null)
                {
                    disappears.ticksToDisappear = Mathf.RoundToInt(disappears.ticksToDisappear * intensity);
                }

                caster.health.AddHediff(hediff);

                DefDatabase<SoundDef>.GetNamed("EnergyShield_Reset").PlayOneShot(new TargetInfo(caster.Position, caster.Map));
            }
        }
    }

    [StaticConstructorOnStartup]
    public class Hediff_Quen : HediffWithComps
    {
        public float energy = 50f;
        private static readonly Material ShieldMaterial = MaterialPool.MatFrom("Other/ShieldBubble", ShaderDatabase.Transparent, new Color(1f, 0.9f, 0.2f, 0.35f));

        public override void PostMake()
        {
            base.PostMake();
            energy = 50f;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref energy, "energy", 50f);
        }

        public bool AbsorbDamage(DamageInfo dinfo)
        {
            energy -= dinfo.Amount;

            if (pawn.Map != null)
            {
                FleckMaker.ThrowLightningGlow(pawn.Position.ToVector3Shifted(), pawn.Map, 1f);
                FleckMaker.ThrowMicroSparks(pawn.Position.ToVector3Shifted(), pawn.Map);

                if (energy <= 0f)
                {
                    DefDatabase<SoundDef>.GetNamed("EnergyShield_Broken").PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
                    FleckMaker.ThrowLightningGlow(pawn.Position.ToVector3Shifted(), pawn.Map, 2f);
                }
                else
                {
                    DefDatabase<SoundDef>.GetNamed("EnergyShield_AbsorbDamage").PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
                }
            }

            if (energy <= 0f)
            {
                pawn.health.RemoveHediff(this);
            }

            return true;
        }

        public void DrawShieldBubble(Vector3 drawLoc)
        {
            float radius = Mathf.Lerp(1.2f, 1.55f, energy / 50f);
            Vector3 drawPos = drawLoc;
            drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            Matrix4x4 matrix = default;
            matrix.SetTRS(drawPos, Quaternion.identity, new Vector3(radius, 1f, radius));
            Graphics.DrawMesh(MeshPool.plane10, matrix, ShieldMaterial, 0);
        }
    }

    // Axii
    public class CompProperties_AbilityAxii : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityAxii() { compClass = typeof(CompAbilityEffect_Axii); }
    }

    public class CompAbilityEffect_Axii : CompAbilityEffect
    {
        public new CompProperties_AbilityAxii Props => (CompProperties_AbilityAxii)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn targetPawn = target.Pawn;
            if (targetPawn == null || targetPawn.Dead) return;

            float sensitivity = targetPawn.GetStatValue(StatDefOf.PsychicSensitivity);
            if (sensitivity <= 0f)
            {
                Messages.Message("Immune to psychic effects.", targetPawn, MessageTypeDefOf.RejectInput, false);
                return;
            }

            FleckMaker.ThrowDustPuffThick(targetPawn.Position.ToVector3Shifted(), targetPawn.Map, 1.5f, new Color(0.3f, 0.8f, 0.4f, 0.8f));

            if (targetPawn.mindState != null && targetPawn.mindState.mentalStateHandler != null)
            {
                targetPawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, "Axii", true, false, false, null, false, false, true);
            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            Pawn pawn = target.Pawn;
            if (pawn == null) return false;

            if (pawn.GetStatValue(StatDefOf.PsychicSensitivity) <= 0f)
            {
                if (throwMessages)
                {
                    Messages.Message("Immune to psychic effects.", pawn, MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }
            return base.Valid(target, throwMessages);
        }
    }

    //Yrden
    [StaticConstructorOnStartup]
    public class Thing_YrdenTrap : ThingWithComps
    {
        public int ticksLeft = 1200;
        public float radius = 3.9f;
        private static readonly Material YrdenMaterial = MaterialPool.MatFrom("Other/YrdenCircle", ShaderDatabase.Transparent, new Color(0.6f, 0f, 0.8f, 0.5f));

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksLeft, "ticksLeft", 1200);
            Scribe_Values.Look(ref radius, "radius", 3.9f);
        }

        protected override void Tick()
        {
            base.Tick();
            ticksLeft--;
            if (ticksLeft <= 0)
            {
                Destroy();
                return;
            }

            if (this.IsHashIntervalTick(30))
            {
                ApplyDebuff();
            }
        }

        private void ApplyDebuff()
        {
            if (Map == null) return;
            foreach (Thing t in GenRadial.RadialDistinctThingsAround(Position, Map, radius, true))
            {
                if (t is Pawn p && !p.Dead && p.Faction != Faction.OfPlayer)
                {
                    Hediff hediff = p.health.hediffSet.GetFirstHediffOfDef(WitcherDefCache.YrdenDebuff);
                    if (hediff == null)
                    {
                        hediff = HediffMaker.MakeHediff(WitcherDefCache.YrdenDebuff, p);
                        p.health.AddHediff(hediff);
                    }

                    HediffComp_Disappears disappears = hediff.TryGetComp<HediffComp_Disappears>();
                    if (disappears != null)
                    {
                        disappears.ticksToDisappear = 60;
                    }
                }
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Vector3 drawPos = drawLoc;
            drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            Matrix4x4 matrix = default;
            matrix.SetTRS(drawPos, Quaternion.identity, new Vector3(radius * 2, 1f, radius * 2));
            Graphics.DrawMesh(MeshPool.plane10, matrix, YrdenMaterial, 0);
        }
    }

    public class CompProperties_AbilityYrden : CompProperties_AbilityEffect
    {
        public float radius = 3.9f;
        public int duration = 1200;
        public CompProperties_AbilityYrden() { compClass = typeof(CompAbilityEffect_Yrden); }
    }

    public class CompAbilityEffect_Yrden : CompAbilityEffect
    {
        public new CompProperties_AbilityYrden Props => (CompProperties_AbilityYrden)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Map map = parent.pawn.Map;
            if (map == null) return;

            float intensity = parent.pawn.GetStatValue(StatDefOf.PsychicSensitivity);

            Thing_YrdenTrap trap = (Thing_YrdenTrap)ThingMaker.MakeThing(WitcherDefCache.YrdenTrapDef);
            trap.radius = Props.radius * intensity;
            trap.ticksLeft = Mathf.RoundToInt(Props.duration * intensity);
            GenSpawn.Spawn(trap, target.Cell, map);
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            float intensity = parent.pawn?.GetStatValue(StatDefOf.PsychicSensitivity) ?? 1f;
            GenDraw.DrawRadiusRing(target.Cell, Props.radius * intensity, Color.magenta);
        }
    }
}