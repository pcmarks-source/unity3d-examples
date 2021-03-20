using System.Xml;
using System.Collections.Generic;
using UnityEngine;

public class WaypointManagerData : ExtensionData {

    private ExtensibleData owner;

    private int index;
    private List<WaypointData> waypoints;

    public bool circular { get; private set; }
    public bool pointHold;
    public bool endHold;
    public float pointHoldFor;
    public float endHoldFor;

    private WaypointData nullWaypoint {
        get {
            return WaypointData.Null(GridData.GetCellAt(Coordinates.FromWorldSpace(owner.gameObject.transform.position)), owner.orientation);
        }
    }

    public WaypointData currentWaypoint {
        get {
            if (waypoints == null) return nullWaypoint;
            if (waypoints.Count <= 0) return nullWaypoint;
            if (waypoints.Count == 1) return waypoints[0];

            if (Metrics.IsApproximatelyEqual(owner.gameObject.transform.position, Coordinates.ToWorldSpaceFlat(waypoints[index].cellData.coordinates), 0.05f) == false) return waypoints[index];

            index++;
            if (index == waypoints.Count) {
                index = 0;
                if (circular == false) {
                    waypoints.Reverse();
                    RebuildPath();
                }
            }

            return waypoints[index];
        }
    }

    public override void Initialize(int ownerUniqueID) {
        this.ownerUniqueID = ownerUniqueID;
        identifier = PStrings.waypointManager;

        owner = GridData.GetExtensible(ownerUniqueID);

        index = 0;
        waypoints = new List<WaypointData>();

        circular = false;
        pointHold = true;
        endHold = false;
        pointHoldFor = 5f;
        endHoldFor = 5f;
    }

    public override void FixedInstructionCycle() {

    }

    public override void InstructionCycle() {

    }

    public override void LateInstructionCycle() {

    }

    public void SetCircular(bool toggle) {
        circular = toggle;
        RebuildPath();
    }

    public void AddWaypoint(WaypointData waypoint) {
        waypoints.Add(waypoint);
        RebuildPath();
    }

    public void RemoveWaypoint(WaypointData waypoint) {
        waypoints.Remove(waypoint);
        RebuildPath();
    }

    public WaypointData GetWaypointAt(CellData cellData) {
        for (int i = 0; i < waypoints.Count; i++) {
            if (waypoints[i].cellData == cellData) {
                return waypoints[i];
            }
        }

        return null;
    }

    public void ShowPath(bool toggle) {
        for (int i = 0; i < waypoints.Count; i++) {
            waypoints[i].ShowPath(toggle);
        }
    }

    public void RebuildPath() {
        if (waypoints.Count <= 0) return;

        WaypointData nextWaypoint, previousWaypoint;

        previousWaypoint = waypoints[0];
        previousWaypoint.SetIndex(0);

        if (waypoints.Count > 1) {
            for (int i = 1; i < waypoints.Count; i++) {
                nextWaypoint = waypoints[i];
                nextWaypoint.SetIndex(i);

                previousWaypoint.SetNextWaypoint(nextWaypoint);
                previousWaypoint = nextWaypoint;
            }
        }

        previousWaypoint.SetNextWaypoint((previousWaypoint != waypoints[0] && circular == true) ? waypoints[0] : null);
    }

    public void Purge() {
        if (waypoints.Count <= 0) return;

        WaypointData[] waypointsToDestroy = new WaypointData[waypoints.Count];
        WaypointData waypointToDestroy;

        for (int i = 0; i < waypoints.Count; i++) {
            waypointsToDestroy[i] = waypoints[i];
        }

        for (int i = 0; i < waypointsToDestroy.Length; i++) {
            waypointToDestroy = waypointsToDestroy[i];
            WaypointData.Destroy(waypointToDestroy.cellData, this);
        }
    }

    public override void WriteXml(XmlWriter writer) {
        writer.WriteAttributeString("Circular", (circular == true) ? 1.ToString() : 0.ToString());
        writer.WriteAttributeString("PointHold", (pointHold == true) ? 1.ToString() : 0.ToString());
        writer.WriteAttributeString("EndHold", (endHold == true) ? 1.ToString() : 0.ToString());

        writer.WriteAttributeString("PointHoldFor", pointHoldFor.ToString());
        writer.WriteAttributeString("EndHoldFor", endHoldFor.ToString());
    }

    public override void ReadXml(XmlReader reader) {
        circular = (int.Parse(reader.GetAttribute("Circular")) == 1);
        pointHold = (int.Parse(reader.GetAttribute("PointHold")) == 1);
        endHold = (int.Parse(reader.GetAttribute("EndHold")) == 1);

        pointHoldFor = float.Parse(reader.GetAttribute("PointHoldFor"));
        endHoldFor = float.Parse(reader.GetAttribute("EndHoldFor"));
    }
}
