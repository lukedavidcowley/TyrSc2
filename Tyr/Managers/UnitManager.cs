﻿using System.Collections.Generic;
using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Managers
{
    public class UnitManager : Manager
    {
        public Dictionary<ulong, Agent> Agents = new Dictionary<ulong, Agent>();

        // Counts the number of units of each type we own.
        public Dictionary<uint, int> Counts { get; internal set; }
        public Dictionary<uint, int> CompletedCounts { get; internal set; }
        public uint FoodExpected { get; internal set; }

        public Dictionary<ulong, Agent> DisappearedUnits = new Dictionary<ulong, Agent>();
        public HashSet<uint> ActiveOrders = new HashSet<uint>();

        public void OnFrame(Tyr tyr)
        {
            Counts = new Dictionary<uint, int>();
            CompletedCounts = new Dictionary<uint, int>();
            HashSet<ulong> existingUnits = new HashSet<ulong>();
            foreach (Base b in tyr.BaseManager.Bases)
            {
                b.BuildingCounts = new Dictionary<uint, int>();
                b.BuildingsCompleted = new Dictionary<uint, int>();
            }
            ActiveOrders = new HashSet<uint>();

            FoodExpected = 0;
            // Update our unit set.
            foreach (Unit unit in tyr.Observation.Observation.RawData.Units)
            {
                if (unit.Owner == tyr.PlayerId)
                {
                    // Count how many of each unitType we have.
                    CollectionUtil.Increment(Counts, unit.UnitType);
                    if (UnitTypes.EquivalentTypes.ContainsKey(unit.UnitType))
                        foreach (uint t in UnitTypes.EquivalentTypes[unit.UnitType])
                            CollectionUtil.Increment(Counts, t);
                    if (unit.BuildProgress >= 0.9999f)
                    {
                        CollectionUtil.Increment(CompletedCounts, unit.UnitType);
                        if (UnitTypes.EquivalentTypes.ContainsKey(unit.UnitType))
                            foreach (uint t in UnitTypes.EquivalentTypes[unit.UnitType])
                                CollectionUtil.Increment(CompletedCounts, t);
                    }

                    if (unit.Orders != null && unit.Orders.Count > 0 && Abilities.Creates.ContainsKey(unit.Orders[0].AbilityId))
                        CollectionUtil.Increment(Counts, Abilities.Creates[unit.Orders[0].AbilityId]);

                    if (unit.BuildProgress < 1 && unit.UnitType == UnitTypes.PYLON)
                        FoodExpected += 8;
                    if (unit.Orders != null && unit.Orders.Count > 0 && unit.Orders[0].AbilityId == 1344)
                        FoodExpected += 8;
                    if (unit.Orders != null && unit.Orders.Count > 0 && unit.Orders[0].AbilityId == 1216)
                        CollectionUtil.Increment(Counts, UnitTypes.LAIR);

                    if (unit.UnitType == UnitTypes.EGG)
                        CollectionUtil.Increment(Counts, Abilities.Creates[unit.Orders[0].AbilityId]);
                    

                    existingUnits.Add(unit.Tag);

                    if (Agents.ContainsKey(unit.Tag))
                    {
                        Agent agent = Agents[unit.Tag];
                        agent.Unit = unit;

                        if (unit.UnitType == UnitTypes.LARVA
                            && agent.LastAbility >= 0
                            && Abilities.Creates.ContainsKey((uint)agent.LastAbility))
                        {
                            CollectionUtil.Increment(Counts, Abilities.Creates[(uint)agent.LastAbility]);
                        }
                        agent.Command = null;
                        if (agent.Base != null)
                        {
                            CollectionUtil.Increment(agent.Base.BuildingCounts, unit.UnitType);
                            if (unit.BuildProgress >= 0.9999f)
                                CollectionUtil.Increment(agent.Base.BuildingsCompleted, unit.UnitType);
                        }
                    }
                    else
                    {
                        if (DisappearedUnits.ContainsKey(unit.Tag))
                        {
                            Agents.Add(unit.Tag, DisappearedUnits[unit.Tag]);
                            DisappearedUnits[unit.Tag].Unit = unit;
                        }
                        else
                        {
                            Agent agent = new Agent(unit);
                            Agents.Add(unit.Tag, agent);
                            tyr.TaskManager.NewAgent(agent);
                        }
                    }

                    if (unit.Orders != null && unit.Orders.Count > 0)
                        ActiveOrders.Add(unit.Orders[0].AbilityId);
                }
            }

            foreach (BuildRequest request in ConstructionTask.Task.BuildRequests)
            {
                // Count how many of each unitType we intend to build.
                CollectionUtil.Increment(Counts, request.Type);
                if (request.Type == UnitTypes.PYLON)
                    FoodExpected += 8;
                if (request.Base != null)
                    CollectionUtil.Increment(request.Base.BuildingCounts, request.Type);

                if (request.worker.Unit.Orders == null
                    || request.worker.Unit.Orders.Count == 0
                    || request.worker.Unit.Orders[0].AbilityId != BuildingType.LookUp[request.Type].Ability)
                {
                    tyr.ReservedMinerals += BuildingType.LookUp[request.Type].Minerals;
                    tyr.ReservedGas += BuildingType.LookUp[request.Type].Gas;
                    string workerAbility = "";
                    if (request.worker.Unit.Orders != null
                        && request.worker.Unit.Orders.Count > 0)
                        workerAbility = " Ability: " + request.worker.Unit.Orders[0].AbilityId;
                    tyr.DrawText("Reserving: " + BuildingType.LookUp[request.Type].Name + workerAbility);
                }
            }

            foreach (BuildRequest request in ConstructionTask.Task.UnassignedRequests)
            {
                // Count how many of each unitType we intend to build.
                CollectionUtil.Increment(Counts, request.Type);
                FoodExpected += 8;
                if (request.Base != null)
                    CollectionUtil.Increment(request.Base.BuildingCounts, request.Type);

                tyr.ReservedMinerals += BuildingType.LookUp[request.Type].Minerals;
                tyr.ReservedGas += BuildingType.LookUp[request.Type].Gas;
                tyr.DrawText("Reserving: " + BuildingType.LookUp[request.Type].Name);
            }

            // Remove dead units.
            if (tyr.Observation != null
                && tyr.Observation.Observation != null
                && tyr.Observation.Observation.RawData != null
                && tyr.Observation.Observation.RawData.Event != null
                && tyr.Observation.Observation.RawData.Event.DeadUnits != null)
                foreach (ulong deadUnit in tyr.Observation.Observation.RawData.Event.DeadUnits)
                    Agents.Remove(deadUnit);
        }

        public int Count(uint type)
        {
            if (Counts.ContainsKey(type))
                return Counts[type];
            else
                return 0;
        }

        public int Completed(uint type)
        {
            if (CompletedCounts.ContainsKey(type))
                return CompletedCounts[type];
            else
                return 0;
        }

        public void AddActions(List<Action> actions)
        {
            foreach (KeyValuePair<ulong, Agent> pair in Agents)
            {
                if (pair.Value.Command != null)
                {
                    Action action = new Action();
                    action.ActionRaw = new ActionRaw();
                    action.ActionRaw.UnitCommand = pair.Value.Command;
                    actions.Add(action);
                }
            }
        }
    }
}
