using RimWorld;
using Verse;

namespace WitcherTrials
{
    // Base Hediff for Witcher Trials
    public abstract class Hediff_WitcherTrialBase : HediffWithComps
    {
        protected bool isResolved = false;
        protected abstract XenotypeDef TargetXenotype { get; }
        protected abstract string SuccessMessage { get; }

        public override void Tick()
        {
            base.Tick();
            if (isResolved) return;

            HediffComp_Immunizable immunizableComp = this.TryGetComp<HediffComp_Immunizable>();
            if (immunizableComp != null && immunizableComp.Immunity >= 1.0f)
            {
                isResolved = true;
                ResolveTransformation();
                return;
            }

            HandleSeverityTick();
        }

        protected virtual void HandleSeverityTick() { }

        protected virtual void ResolveTransformation()
        {
            if (TargetXenotype != null && pawn.genes != null)
            {
                ApplyGenes(TargetXenotype);
                pawn.genes.SetXenotype(TargetXenotype);
                Messages.Message(SuccessMessage, pawn, MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                Log.Error($"[WitcherTrials] Could not resolve transformation. Target Xenotype missing or genes null.");
            }
            pawn.health.RemoveHediff(this);
        }

        protected virtual void ApplyGenes(XenotypeDef def) { }
    }

    // Base Recipe for Witcher Surgeries
    public abstract class Recipe_WitcherSurgeryBase : Recipe_InstallImplant
    {
        protected abstract bool IsValidForPawn(Pawn pawn);

        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            if (!base.AvailableOnNow(thing, part)) return false;
            if (!(thing is Pawn pawn) || pawn.genes == null) return false;
            return IsValidForPawn(pawn);
        }
    }
}