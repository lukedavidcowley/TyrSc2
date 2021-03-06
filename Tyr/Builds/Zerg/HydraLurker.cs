﻿using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Micro;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Zerg
{
    public class HydraLurker : Build
    {
        private static int RequiredZerglings = 14;
        private bool GoingBroodlords = false;

        public override string Name()
        {
            return "HydraLurker";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            TimingAttackTask.Enable();
            WorkerScoutTask.Enable();
            QueenInjectTask.Enable();
            QueenDefenseTask.Enable();
            ArmyOverseerTask.Enable();
            QueenTumorTask.Enable();
            DefenseTask.Enable();
            HydraDefenseTask.Enable(true);
        }

        public override Build OverrideBuild()
        {
            return ZergBuildUtil.GetDefenseBuild();
        }

        public override void OnStart(Tyr tyr)
        {
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new LurkerController());
            MicroControllers.Add(new TargetFireController(GetPriorities()));

            Set += ZergBuildUtil.Overlords();
            if (tyr.EnemyRace == Race.Protoss)
                Set += Broodlords();
            Set += AntiLifting();
            Set += MainBuild();
        }

        public PriorityTargetting GetPriorities()
        {
            PriorityTargetting priorities = new PriorityTargetting();

            priorities.DefaultPriorities.MaxRange = 10;
            priorities.DefaultPriorities[UnitTypes.LARVA] = -1;
            priorities.DefaultPriorities[UnitTypes.EGG] = -1;

            foreach (uint t in UnitTypes.CombatUnitTypes)
                priorities[UnitTypes.HYDRALISK][t] = 1;

            return priorities;
        }

        private BuildList Broodlords()
        {
            BuildList result = new BuildList();
            result.If(() => { return GoingBroodlords; });
            result.Morph(UnitTypes.DRONE, 20);
            result.Building(UnitTypes.EXTRACTOR, 4);
            result.Morph(UnitTypes.HYDRALISK, 5);
            result.Morph(UnitTypes.LURKER);
            result.Morph(UnitTypes.HYDRALISK);
            result.Morph(UnitTypes.LURKER);
            result.Morph(UnitTypes.HYDRALISK);
            result.Morph(UnitTypes.LURKER);
            result.Morph(UnitTypes.HYDRALISK);
            result.Morph(UnitTypes.LURKER);
            result.Morph(UnitTypes.HYDRALISK);
            result.Morph(UnitTypes.LURKER);
            result.Morph(UnitTypes.HYDRALISK);
            result.Building(UnitTypes.INFESTATION_PIT);
            result.Building(UnitTypes.SPIRE);
            result.Morph(UnitTypes.CORRUPTOR, 5);
            result.Morph(UnitTypes.BROOD_LORD, 10);
            result.Morph(UnitTypes.HYDRALISK, 100);
            return result;
        }

        private BuildList AntiLifting()
        {
            BuildList result = new BuildList();
            result.If(() => { return Tyr.Bot.EnemyStrategyAnalyzer.LiftingDetected; });
            result.Building(UnitTypes.EXTRACTOR, 2);
            result.Building(UnitTypes.SPIRE);
            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();
            result.Building(UnitTypes.HATCHERY, 2, () => { return Tyr.Bot.EnemyRace != Race.Protoss || (Count(UnitTypes.HATCHERY) + Count(UnitTypes.LAIR) + Count(UnitTypes.HIVE) < 2); });
            result.Building(UnitTypes.SPAWNING_POOL);
            result.If(() => { return Count(UnitTypes.QUEEN) > 0; });
            result.If(() => { return Completed(UnitTypes.HATCHERY) >= 2 && Tyr.Bot.Frame >= 22.4 * 60 * 2; });
            result.Building(UnitTypes.EXTRACTOR);
            result.Building(UnitTypes.SPINE_CRAWLER, Natural, Tyr.Bot.MapAnalyzer.Walk(NaturalDefensePos, Tyr.Bot.MapAnalyzer.EnemyDistances, 5), 2);
            result.If(() => { return Count(UnitTypes.ZERGLING) >= RequiredZerglings && Count(UnitTypes.DRONE) >= 20; });
            result.Building(UnitTypes.EXTRACTOR);
            result.Building(UnitTypes.HYDRALISK_DEN);
            result.Building(UnitTypes.EXTRACTOR, 2);
            result.Building(UnitTypes.EXTRACTOR, 2, () => { return Tyr.Bot.EnemyRace != Race.Protoss; });
            result.If(() => { return Count(UnitTypes.HYDRALISK) >= 5; });
            result.Building(UnitTypes.LURKER_DEN);
            return result;
        }

        public override void OnFrame(Tyr tyr)
        {
            if (tyr.EnemyRace == Race.Protoss)
            {
                TimingAttackTask.Task.RequiredSize = 45;
                TimingAttackTask.Task.RetreatSize = 8;
            }
            else
            {
                TimingAttackTask.Task.RequiredSize = 70;
                TimingAttackTask.Task.RetreatSize = 20;
            }

            tyr.DrawText("Extractors: " + Count(UnitTypes.EXTRACTOR));

            DefenseTask.AirDefenseTask.MainDefenseRadius = 20;
            DefenseTask.AirDefenseTask.ExpandDefenseRadius = 14;
            DefenseTask.AirDefenseTask.MaxDefenseRadius = 55;

            DefenseTask.GroundDefenseTask.MainDefenseRadius = 20;
            DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 14;
            DefenseTask.GroundDefenseTask.MaxDefenseRadius = 55;

            IdleTask.Task.FearEnemies = true;

            GoingBroodlords = tyr.EnemyRace == Race.Protoss && Completed(UnitTypes.HYDRALISK_DEN) > 0 && Completed(UnitTypes.LURKER_DEN) > 0 && Count(UnitTypes.HATCHERY) + Count(UnitTypes.LAIR) + Count(UnitTypes.HIVE) >= 2;
            if (tyr.EnemyRace != Race.Protoss)
                tyr.DrawScreen("Wrong race.", 12, 0.65f, 0.18f);
            else if (Completed(UnitTypes.HYDRALISK_DEN) == 0)
                tyr.DrawScreen("No hydra den", 12, 0.65f, 0.18f);
            else if (Completed(UnitTypes.LURKER_DEN) == 0)
                tyr.DrawScreen("No lurker den", 12, 0.65f, 0.18f);
            else if (Count(UnitTypes.HATCHERY) + Count(UnitTypes.LAIR) + Count(UnitTypes.HIVE) < 2)
                tyr.DrawScreen("Not enough bases. Hatch: " + Count(UnitTypes.HATCHERY) + " Lair: " + Count(UnitTypes.LAIR) + " Hive: " + Count(UnitTypes.HIVE), 12, 0.65f, 0.18f);
            else if (GoingBroodlords)
                tyr.DrawScreen("Going broodlords", 12, 0.65f, 0.18f);
            else
                tyr.DrawScreen("Not going broodlords", 12, 0.65f, 0.18f);

            if (tyr.EnemyRace == Race.Protoss)
                RequiredZerglings = 0;

            if (Count(UnitTypes.HYDRALISK) - 10 >= (Count(UnitTypes.LURKER) + Count(UnitTypes.LURKER_BURROWED)) * 2)
                MorphingTask.Task.Morph(UnitTypes.LURKER);

            if (Completed(UnitTypes.LAIR) > 0
                && Count(UnitTypes.OVERSEER) < 2)
                MorphingTask.Task.Morph(UnitTypes.OVERSEER);

            if (FoodUsed()
                        + Tyr.Bot.UnitManager.Count(UnitTypes.HATCHERY) * 2
                        + Tyr.Bot.UnitManager.Count(UnitTypes.LAIR) * 2
                        + Tyr.Bot.UnitManager.Count(UnitTypes.HIVE) * 2
                        >= ExpectedAvailableFood() - 2)
                MorphingTask.Task.Morph(UnitTypes.OVERLORD);
            else if (Count(UnitTypes.DRONE) >= 14 && Count(UnitTypes.SPAWNING_POOL) == 0) { }
            else if (ExpectedAvailableFood() > FoodUsed() + 2
                        && Count(UnitTypes.DRONE) < 45 - Completed(UnitTypes.EXTRACTOR)
                        && (Count(UnitTypes.DRONE) < 40 - Completed(UnitTypes.EXTRACTOR) || Count(UnitTypes.HATCHERY) >= 3)
                        && (Count(UnitTypes.ZERGLING) >= RequiredZerglings || Count(UnitTypes.DRONE) <= 18)
                        && (Count(UnitTypes.LAIR) > 0 || Count(UnitTypes.DRONE) <= 24))
                MorphingTask.Task.Morph(UnitTypes.DRONE);
            else if (Completed(UnitTypes.SPAWNING_POOL) > 0
                && Count(UnitTypes.ZERGLING) < RequiredZerglings
                && ExpectedAvailableFood() > FoodUsed() + 4
                && (Count(UnitTypes.SPINE_CRAWLER) >= 2 || Minerals() >= 200))
                MorphingTask.Task.Morph(UnitTypes.ZERGLING);
            else if (Completed(UnitTypes.HYDRALISK_DEN) > 0
                        && (Count(UnitTypes.HYDRALISK) < 15 || Count(UnitTypes.LURKER_DEN) > 0)
                        && (Count(UnitTypes.HYDRALISK) - 20 < Count(UnitTypes.LURKER) * 2 || Gas() >= 150)
                        && ExpectedAvailableFood() > FoodUsed() + 4
                        && !GoingBroodlords)
                MorphingTask.Task.Morph(UnitTypes.HYDRALISK);
            
        }

        public override void Produce(Tyr tyr, Agent agent)
        {
            if (UnitTypes.ResourceCenters.Contains(agent.Unit.UnitType))
            {
                if (Minerals() >= 100
                    && (Completed(UnitTypes.QUEEN) < 2 || Count(UnitTypes.LAIR)  > 0)
                    && Completed(UnitTypes.QUEEN) < 3
                    && Completed(UnitTypes.SPAWNING_POOL) > 0)
                {
                    agent.Order(1632);
                    CollectionUtil.Increment(tyr.UnitManager.Counts, UnitTypes.QUEEN);
                }
                else if (Minerals() >= 100 && Completed(UnitTypes.QUEEN) < 5
                    && Count(UnitTypes.LAIR) > 0)
                {
                    agent.Order(1632);
                    CollectionUtil.Increment(tyr.UnitManager.Counts, UnitTypes.QUEEN);
                }
                else if (Count(UnitTypes.SPINE_CRAWLER) > 0
                    && Minerals() >= 150 && Gas() >= 100
                    && Count(UnitTypes.LAIR) + Count(UnitTypes.HIVE) == 0
                    && Count(UnitTypes.ZERGLING) >= RequiredZerglings)
                    agent.Order(1216);
                else if (agent.Unit.UnitType == UnitTypes.LAIR
                    && Completed(UnitTypes.INFESTATION_PIT) > 0
                    && Minerals() >= 200 && Gas() >= 150
                    && Count(UnitTypes.LURKER) >= 4
                    && Tyr.Bot.EnemyRace == Race.Protoss)
                    agent.Order(1218);
            }
            else if (agent.Unit.UnitType == UnitTypes.HYDRALISK_DEN)
            {
                if (!Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(134)
                    && Gas() >= 100
                    && Minerals() >= 100)
                    agent.Order(1282);
            }
            else if (agent.Unit.UnitType == UnitTypes.SPIRE)
            {
                if (Completed(UnitTypes.HIVE) > 0
                    && Minerals() >= 100
                    && Gas() >= 150)
                {
                    agent.Order(1220);
                }
            }
        }
    }
}
