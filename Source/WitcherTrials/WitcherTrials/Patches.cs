using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace WitcherTrials
{
    public class ModExtension_WitcherWeapon : DefModExtension
    {
        public bool isSilver = false;
        public bool isSteel = false;
        public float damageMultiplier = 1.5f;
    }

    [StaticConstructorOnStartup]
    public static class WitcherTrialsHarmony
    {
        static WitcherTrialsHarmony()
        {
            var harmony = new Harmony("RedRaven.witchertrials.harmony");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(Thing), "TakeDamage")]
    public static class Patch_WitcherWeaponDamage
    {
        public static void Prefix(Thing __instance, ref DamageInfo dinfo)
        {
            if (dinfo.Weapon != null && __instance is Pawn targetPawn)
            {
                var extension = dinfo.Weapon.GetModExtension<ModExtension_WitcherWeapon>();
                if (extension != null)
                {
                    bool isHumanlike = targetPawn.RaceProps.Humanlike;
                    if ((extension.isSilver && !isHumanlike) || (extension.isSteel && isHumanlike))
                    {
                        dinfo.SetAmount(dinfo.Amount * extension.damageMultiplier);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), "AddHediff", new System.Type[] { typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult) })]
    public static class Patch_PreventAddictionsAndOverdose
    {
        public static bool Prefix(Pawn_HealthTracker __instance, Pawn ___pawn, Hediff hediff)
        {
            if (___pawn.genes == null) return true;

            if (WitcherDefCache.MutagenicMetabolism != null && ___pawn.genes.HasActiveGene(WitcherDefCache.MutagenicMetabolism))
            {
                if (typeof(Hediff_Addiction).IsAssignableFrom(hediff.def.hediffClass)) return false;
                if (hediff.def == HediffDefOf.DrugOverdose) hediff.Severity *= 0.1f;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), "CheckForStateChange")]
    public static class Patch_CheckForStateChange
    {
        public static bool Prefix(Pawn_HealthTracker __instance, Pawn ___pawn, DamageInfo? dinfo, Hediff hediff)
        {
            if (___pawn.genes == null || ___pawn.Dead) return true;

            if (WitcherDefCache.ImmortalVitality != null && ___pawn.genes.HasActiveGene(WitcherDefCache.ImmortalVitality))
            {
                if (___pawn.health.ShouldBeDead()) return true;

                if (!___pawn.Downed && __instance.ShouldBeDowned())
                {
                    Traverse.Create(__instance).Method("MakeDowned", new object[] { dinfo, hediff }).GetValue();
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PawnGenerator), "GenerateTraits")]
    public static class Patch_GenerateTraits_ChildOfDestiny
    {
        public static void Postfix(Pawn pawn, PawnGenerationRequest request)
        {
            if (pawn.story?.traits == null) return;
            if (WitcherDefCache.ChildOfDestiny == null || pawn.story.traits.HasTrait(WitcherDefCache.ChildOfDestiny)) return;
            if (!pawn.DevelopmentalStage.HasFlag(DevelopmentalStage.Child)) return;

            bool isQuestOrSlaveType = request.KindDef == PawnKindDefOf.Slave ||
                                      request.KindDef == PawnKindDefOf.SpaceRefugee ||
                                      request.KindDef == PawnKindDefOf.Refugee ||
                                      request.KindDef == PawnKindDefOf.Villager;

            if (request.Faction != Faction.OfPlayer)
            {
                float chance = isQuestOrSlaveType ? 0.25f : 0.01f;
                if (Rand.Value < chance)
                {
                    pawn.story.traits.GainTrait(new Trait(WitcherDefCache.ChildOfDestiny));
                }
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PreApplyDamage))]
    public static class Patch_Pawn_PreApplyDamage
    {
        public static bool Prefix(Pawn __instance, ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;
            if (__instance == null || __instance.health?.hediffSet == null) return true;

            Hediff_Quen quen = (Hediff_Quen)__instance.health.hediffSet.GetFirstHediffOfDef(WitcherDefCache.QuenHediff);
            if (quen != null)
            {
                absorbed = quen.AbsorbDamage(dinfo);
                return !absorbed;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn), "DrawAt")]
    public static class Patch_Pawn_DrawAt
    {
        public static void Postfix(Pawn __instance, Vector3 drawLoc)
        {
            if (__instance.health?.hediffSet != null)
            {
                Hediff_Quen quen = (Hediff_Quen)__instance.health.hediffSet.GetFirstHediffOfDef(WitcherDefCache.QuenHediff);
                if (quen != null)
                {
                    quen.DrawShieldBubble(drawLoc);
                }
            }
        }
    }

    [HarmonyPatch(typeof(ThoughtWorker_Dark), "CurrentStateInternal")]
    public static class Patch_ThoughtWorker_Dark
    {
        public static void Postfix(Pawn p, ref ThoughtState __result)
        {
            if (__result.Active && p.genes != null && p.genes.HasActiveGene(WitcherDefCache.NightVisionGene))
            {
                __result = ThoughtState.Inactive;
            }
        }
    }
}