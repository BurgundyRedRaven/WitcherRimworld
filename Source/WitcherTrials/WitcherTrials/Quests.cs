using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;

namespace WitcherTrials
{
    public class GenStep_WolfTrial : GenStep
    {
        public override int SeedPart => 19384756;

        public override void Generate(Map map, GenStepParams parms)
        {
            PawnKindDef bossDef = DefDatabase<PawnKindDef>.GetNamedSilentFail("DankPyon_Direwolf")
                               ?? DefDatabase<PawnKindDef>.GetNamedSilentFail("Wolf_Great")
                               ?? PawnKindDef.Named("Warg");

            if (CellFinder.TryFindRandomCellNear(map.Center, map, 15, (IntVec3 c) => c.Standable(map), out IntVec3 spawnCell))
            {
                Pawn boss = PawnGenerator.GeneratePawn(bossDef, null);

                Hediff_TrialTarget trigger = (Hediff_TrialTarget)HediffMaker.MakeHediff(WitcherDefCache.TrialTargetHediff, boss);
                trigger.DormantAmuletDef = ThingDef.Named("Witcher_WolfAmulet_Dormant");
                trigger.ActiveAmuletDef = ThingDef.Named("Witcher_WolfAmulet_Active");

                boss.health.AddHediff(trigger);
                boss.health.AddHediff(WitcherDefCache.TrialBossBuff);

                GenSpawn.Spawn(boss, spawnCell, map);
                boss.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent);

                int packSize = Rand.RangeInclusive(5, 8);
                for (int i = 0; i < packSize; i++)
                {
                    if (CellFinder.TryFindRandomCellNear(spawnCell, map, 5, (IntVec3 c) => c.Standable(map), out IntVec3 packCell))
                    {
                        Pawn wolf = PawnGenerator.GeneratePawn(PawnKindDef.Named("Wolf_Timber"), null);
                        GenSpawn.Spawn(wolf, packCell, map);
                        wolf.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent);
                    }
                }
            }
        }
    }

    public class GenStep_CatTarget : GenStep
    {
        public override int SeedPart => 28475930;

        public override void Generate(Map map, GenStepParams parms)
        {
            Faction enemyFaction = Find.FactionManager.RandomEnemyFaction(false, false, false, TechLevel.Undefined)
                                ?? Find.FactionManager.AllFactions.FirstOrDefault(f => f.HostileTo(Faction.OfPlayer));

            if (CellFinder.TryFindRandomCellNear(map.Center, map, 15, (IntVec3 c) => c.Standable(map), out IntVec3 spawnCell))
            {
                Pawn target = PawnGenerator.GeneratePawn(PawnKindDef.Named("Mercenary_Slasher"), enemyFaction);

                Hediff_TrialTarget trigger = (Hediff_TrialTarget)HediffMaker.MakeHediff(WitcherDefCache.TrialTargetHediff, target);
                trigger.DormantAmuletDef = ThingDef.Named("Witcher_CatAmulet_Dormant");
                trigger.ActiveAmuletDef = ThingDef.Named("Witcher_CatAmulet_Active");

                target.health.AddHediff(trigger);
                target.health.AddHediff(WitcherDefCache.TrialBossBuff);

                GenSpawn.Spawn(target, spawnCell, map);
                List<Pawn> group = new List<Pawn> { target };

                for (int i = 0; i < 3; i++)
                {
                    if (CellFinder.TryFindRandomCellNear(spawnCell, map, 5, (IntVec3 c) => c.Standable(map), out IntVec3 guardCell))
                    {
                        Pawn guard = PawnGenerator.GeneratePawn(PawnKindDef.Named("Mercenary_Gunner"), enemyFaction);
                        GenSpawn.Spawn(guard, guardCell, map);
                        group.Add(guard);
                    }
                }
                LordMaker.MakeNewLord(enemyFaction, new LordJob_DefendPoint(spawnCell), map, group);
            }
        }
    }

    public class GenStep_BearTrial : GenStep
    {
        public override int SeedPart => 38475019;

        public override void Generate(Map map, GenStepParams parms)
        {
            MapComponent_BearTrial comp = map.GetComponent<MapComponent_BearTrial>();
            if (comp == null)
            {
                comp = new MapComponent_BearTrial(map);
                map.components.Add(comp);
            }
            comp.StartTrial();
        }
    }

    public class GameCondition_InstantTemperature : GameCondition
    {
        public override float TemperatureOffset() => this.def.temperatureOffset;
    }

    public class MapComponent_BearTrial : MapComponent
    {
        public bool isTrialActive = false;
        private int bossSpawnTimer = 25000;

        public MapComponent_BearTrial(Map map) : base(map) { }

        public void StartTrial()
        {
            isTrialActive = true;
            GameCondition cond = GameConditionMaker.MakeCondition(DefDatabase<GameConditionDef>.GetNamed("Witcher_ExtremeCold"), 60000);
            map.gameConditionManager.RegisterCondition(cond);
            map.weatherManager.TransitionTo(WeatherDef.Named("SnowHard"));
            Messages.Message("The Trial of the Bear has begun. Survive the magical blizzard.", MessageTypeDefOf.NeutralEvent);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (!isTrialActive) return;

            if (bossSpawnTimer > 0)
            {
                bossSpawnTimer--;
                if (bossSpawnTimer <= 0) SpawnBoss();
            }
        }

        private void SpawnBoss()
        {
            if (RCellFinder.TryFindRandomPawnEntryCell(out IntVec3 spawnCell, map, CellFinder.EdgeRoadChance_Animal))
            {
                Pawn boss = PawnGenerator.GeneratePawn(PawnKindDef.Named("Bear_Grizzly"), null);
                boss.health.AddHediff(WitcherDefCache.TrialBossBuff);

                Hediff_TrialTarget trigger = (Hediff_TrialTarget)HediffMaker.MakeHediff(WitcherDefCache.TrialTargetHediff, boss);
                trigger.DormantAmuletDef = ThingDef.Named("Witcher_BearAmulet_Dormant");
                trigger.ActiveAmuletDef = ThingDef.Named("Witcher_BearAmulet_Active");

                boss.health.AddHediff(trigger);
                GenSpawn.Spawn(boss, spawnCell, map);
                boss.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent);

                Messages.Message("An ancient beast emerges from the blizzard!", new TargetInfo(spawnCell, map), MessageTypeDefOf.ThreatBig);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref isTrialActive, "isTrialActive", false);
            Scribe_Values.Look(ref bossSpawnTimer, "bossSpawnTimer", 7500);
        }
    }
}