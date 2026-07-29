using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace WitcherTrials
{
    public class Gene_Focus : Gene_Resource
    {
        public override float InitialResourceMax => 1.0f;
        public override float MinLevelForAlert => 0f;
        public override float MaxLevelOffset => 0f;

        protected override Color BarColor => new ColorInt(247, 203, 21).ToColor;
        protected override Color BarHighlightColor => new ColorInt(251, 232, 157).ToColor;

        private GeneGizmo_Focus gizmo;

        public override void Tick()
        {
            base.Tick();
            if (pawn.IsHashIntervalTick(150))
            {
                Value += pawn.GetStatValue(WitcherDefCache.FocusRegen);
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (pawn.Faction == Faction.OfPlayer)
            {
                if (gizmo == null)
                {
                    gizmo = new GeneGizmo_Focus(this, new List<IGeneResourceDrain>(), BarColor, BarHighlightColor);
                }
                yield return gizmo;
            }
        }
    }

    public class GeneGizmo_Focus : GeneGizmo_Resource
    {
        private Gene_Focus focusGene;
        private bool draggingBar;
        private Color customBarColor;

        public GeneGizmo_Focus(Gene_Resource gene, List<IGeneResourceDrain> drainGenes, Color barColor, Color barHighlightColor)
            : base(gene, drainGenes, barColor, barHighlightColor)
        {
            this.focusGene = (Gene_Focus)gene;
            this.customBarColor = barColor;
            this.Order = -100f;
        }

        protected override bool DraggingBar
        {
            get => draggingBar;
            set => draggingBar = value;
        }

        protected override string GetTooltip()
        {
            return $"{focusGene.ResourceLabel.CapitalizeFirst()}\n\n{focusGene.def.resourceDescription}\n\nRegeneration: +{focusGene.pawn.GetStatValue(WitcherDefCache.FocusRegen):F2} per 150 ticks.";
        }

        protected override IEnumerable<float> GetBarThresholds()
        {
            yield break;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Widgets.DrawWindowBackground(rect);

            Rect headerRect = new Rect(rect.x + 6f, rect.y + 6f, rect.width - 12f, 20f);
            Text.Font = GameFont.Small;
            Widgets.Label(headerRect, focusGene.ResourceLabel.CapitalizeFirst());

            Rect barRect = new Rect(rect.x + 6f, rect.y + 28f, rect.width - 12f, 24f);

            Widgets.FillableBar(barRect, focusGene.Value / focusGene.Max, SolidColorMaterials.NewSolidColorTexture(customBarColor), BaseContent.BlackTex, false);

            ITargetingSource targetingSource = Find.Targeter.targetingSource;
            if (targetingSource != null && targetingSource.Caster == focusGene.pawn)
            {
                if (targetingSource.GetVerb is Verb_CastAbility verbCast && verbCast.ability != null)
                {
                    CompAbilityEffect_FocusCost costComp = verbCast.ability.CompOfType<CompAbilityEffect_FocusCost>();
                    if (costComp != null)
                    {
                        float highlightWidth = (costComp.Props.focusCost / focusGene.Max) * barRect.width;
                        float startX = barRect.x + (focusGene.Value / focusGene.Max) * barRect.width - highlightWidth;
                        if (startX < barRect.x) startX = barRect.x;

                        Rect highlightRect = new Rect(startX, barRect.y, highlightWidth, barRect.height);

                        Widgets.DrawBoxSolid(highlightRect, new Color(1f, 1f, 1f, 0.4f));
                    }
                }
            }

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(barRect, $"{focusGene.Value * 100f:F0} / {focusGene.Max * 100f:F0}");
            Text.Anchor = TextAnchor.UpperLeft;

            if (Mouse.IsOver(rect))
            {
                TooltipHandler.TipRegion(rect, GetTooltip());
            }

            return new GizmoResult(GizmoState.Clear);
        }
    }

    public class CompProperties_AbilityFocusCost : CompProperties_AbilityEffect
    {
        public float focusCost = 0.25f;
        public CompProperties_AbilityFocusCost() { compClass = typeof(CompAbilityEffect_FocusCost); }
    }

    public class CompAbilityEffect_FocusCost : CompAbilityEffect
    {
        public new CompProperties_AbilityFocusCost Props => (CompProperties_AbilityFocusCost)props;

        public override bool GizmoDisabled(out string reason)
        {
            Gene_Focus focusGene = parent.pawn.genes?.GetFirstGeneOfType<Gene_Focus>();
            if (focusGene == null || focusGene.Value < Props.focusCost)
            {
                reason = "Not enough Focus.";
                return true;
            }
            reason = null;
            return false;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Gene_Focus focusGene = parent.pawn.genes?.GetFirstGeneOfType<Gene_Focus>();
            if (focusGene != null)
            {
                focusGene.Value -= Props.focusCost;
            }
        }

        public override string ExtraTooltipPart()
        {
            return $"Focus cost: {Mathf.RoundToInt(Props.focusCost * 100f)}";
        }
    }
}