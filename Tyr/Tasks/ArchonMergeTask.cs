﻿using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Tasks
{
    public class ArchonMergeTask : Task
    {
        public ArchonMergeTask() : base(12)
        { }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.DARK_TEMPLAR && SC2Util.DistanceGrid(agent.Unit.Pos, Tyr.Bot.MapAnalyzer.StartLocation) <= 30;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Tyr tyr)
        {
            if (units.Count >= 2)
                units[0].ArchonMerge(units[1]);

            for (int i = units.Count - 1; i >= 0; i--)
            {
                if (units[i].Unit.UnitType != UnitTypes.DARK_TEMPLAR
                    && units[i].Unit.UnitType != UnitTypes.HIGH_TEMPLAR)
                {
                    IdleTask.Task.Add(units[i]);
                    units.RemoveAt(i);
                }
            }
        }
    }
}
