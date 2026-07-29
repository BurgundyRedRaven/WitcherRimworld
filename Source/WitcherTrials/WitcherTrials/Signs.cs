using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

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
                targetPawn.stances.stunner.StunFor(Mathf.RoundToInt(Props.stunDurationSeconds * 60f), parent.pawn, false, true);

                targetPawn.TakeDamage(new DamageInfo(DamageDefOf.Blunt, Props.damageAmount, 0.5f, -1f, parent.pawn));

                FleckMaker.ThrowDustPuffThick(target.Cell.ToVector3Shifted(), parent.pawn.Map, 2f, new Color(0.8f, 0.8f, 0.8f, 0.5f));
            }
        }
    }
}
