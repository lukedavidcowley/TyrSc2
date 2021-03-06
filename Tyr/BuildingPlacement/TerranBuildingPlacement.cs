﻿using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.BuildingPlacement
{
    /*
     * This class is used to find build locations for Terran structures.
     */
    public class TerranBuildingPlacement
    {
        public static Point2D FindPlacement(Point2D target, Point2D size, uint type)
        {
            Point2D reference = SC2Util.To2D(Tyr.Bot.MapAnalyzer.StartLocation);

            if (type == UnitTypes.MISSILE_TURRET)
                return FindPlacementSupplyDepot(reference, target, size, type);
            else if (type == UnitTypes.SUPPLY_DEPOT)
                return FindPlacementSupplyDepot(reference, target, size, type);
            else
                return FindPlacementProduction(reference, target, size, type);
        }

        public static Point2D FindPlacementSupplyDepot(Point2D reference, Point2D target, Point2D size, uint type)
        {
            Point2D result = null;
            float distance = 1000000;
            for (float x = reference.X - 31.5f; x <= reference.X + 28; x += 7)
                for (float y = reference.Y - 30.5f; y <= reference.Y + 30; y += 2)
                {
                    float newDist = SC2Util.DistanceSq(target, SC2Util.Point(x, y));

                    if (newDist > distance)
                        continue;

                    if (!RectBuildable(x - 0.5f, y - 0.5f, x + 0.5f, y + 0.5f))
                        continue;

                    bool blocked = false;
                    foreach (Unit unit in Tyr.Bot.Observation.Observation.RawData.Units)
                        if (!BuildingPlacer.CheckDistClose(x - 0.5f, y - 0.5f, x + 0.5f, y + 0.5f, SC2Util.To2D(unit.Pos), unit.UnitType))
                        {
                            blocked = true;
                            break;
                        }

                    foreach (BuildRequest request in ConstructionTask.Task.UnassignedRequests)
                        if (!BuildingPlacer.CheckDistClose(x - 0.5f, y - 0.5f, x + 0.5f, y + 0.5f, request.Pos, request.Type))
                        {
                            blocked = true;
                            break;
                        }

                    foreach (BuildRequest request in ConstructionTask.Task.BuildRequests)
                        if (!BuildingPlacer.CheckDistClose(x - 0.5f, y - 0.5f, x + 0.5f, y + 0.5f, request.Pos, request.Type))
                        {
                            blocked = true;
                            break;
                        }

                    foreach (Base b in Tyr.Bot.BaseManager.Bases)
                        if (!BuildingPlacer.CheckDistClose(x - 0.5f, y - 0.5f, x + 0.5f, y + 0.5f, b.BaseLocation.Pos, UnitTypes.COMMAND_CENTER))
                        {
                            blocked = true;
                            break;
                        }

                    if (blocked)
                        continue;

                    distance = newDist;
                    result = SC2Util.Point(x, y);
                }
            return result;
        }

        public static Point2D FindPlacementProduction(Point2D reference, Point2D target, Point2D size, uint type)
        {
            Point2D result = null;
            float distance = 1000000;
            for (float x = reference.X - 28f; x <= reference.X + 28; x += 7f)
                for (float y = reference.Y - 30f; y <= reference.Y + 30f; y += 3f)
                {
                    float newDist = SC2Util.DistanceSq(target, SC2Util.Point(x, y));

                    if (newDist > distance)
                        continue;

                    if (!RectBuildable(x - 3.5f, y - 2.5f, x + 3.5f, y + 2.5f))
                        continue;

                    bool blocked = false;
                    foreach (Unit unit in Tyr.Bot.Observation.Observation.RawData.Units)
                        if (!BuildingPlacer.CheckDistClose(x - 2.5f, y - 1.5f, x + 2.5f, y + 1.5f, SC2Util.To2D(unit.Pos), unit.UnitType))
                        {
                            blocked = true;
                            break;
                        }

                    foreach (Base b in Tyr.Bot.BaseManager.Bases)
                        if (!BuildingPlacer.CheckDistClose(x - 2.5f, y - 1.5f, x + 2.5f, y + 1.5f, b.BaseLocation.Pos, UnitTypes.COMMAND_CENTER))
                        {
                            blocked = true;
                            break;
                        }

                    if (blocked)
                        continue;

                    distance = newDist;
                    result = SC2Util.Point(x - 1, y);
                }

            return result;
        }

        public static bool RectBuildable(float x1, float y1, float x2, float y2)
        {
            for (float x = x1; x <= x2; x++)
                for (float y = y1; y <= y2; y++)
                    if (!SC2Util.GetTilePlacable((int)x, (int)y))
                        return false;
            return true;
        }
    }
}
