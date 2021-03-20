using System.Xml;
using UnityEngine;

public class PathfindManagerData : ExtensionData {

    private const float msBetweenPathRequests = 250f / 1000f;
    private float nextPathRequest;

    private ExtensibleData owner;

    private bool pathRequested;
    private int index;
    private PathNode[] path;

    private PathNode nullNode {
        get {
            return PathGrid.GetNodeAt(Coordinates.FromWorldSpace(owner.gameObject.transform.position));
        }
    }

    public Coordinates intermediary {
        get {
            if (path == null) return nullNode.coordinates;
            if (path.Length <= 0) return nullNode.coordinates;

            if (path.Length == 1) return path[0].coordinates;
            if (path.Length == 1 + index) return path[path.Length - 1].coordinates;

            if (atIntermediary == false) return path[index].coordinates;

            index++;
            return path[index].coordinates;
        }
    }

    public Coordinates destination {
        get {
            if (path == null) return nullNode.coordinates;
            if (path.Length <= 0) return nullNode.coordinates;

            return path[path.Length - 1].coordinates;
        }
    }

    private bool atIntermediary {
        get {
            return Metrics.IsApproximatelyEqual(owner.gameObject.transform.position, Coordinates.ToWorldSpaceFlat(path[index].coordinates), 0.05f);
        }
    }

    public bool atDestination {
        get {
            return Metrics.IsApproximatelyEqual(owner.gameObject.transform.position, Coordinates.ToWorldSpaceFlat(destination), 0.05f);
        }
    }

    public Vector3 directionToIntermediary {
        get {
            Vector3 rawDirection = (Coordinates.ToWorldSpaceFlat(intermediary) - owner.gameObject.transform.position);
            return new Vector3(rawDirection.x, 0, rawDirection.z).normalized;
        }
    }

    public override void Initialize(int ownerUniqueID) {
        this.ownerUniqueID = ownerUniqueID;
        identifier = PStrings.pathfindManager;

        owner = GridData.GetExtensible(ownerUniqueID);

        index = 0;
        path = null;

        pathRequested = false;
        nextPathRequest = Metrics.time;
    }

    public override void FixedInstructionCycle() {

    }

    public override void InstructionCycle() {

    }

    public override void LateInstructionCycle() {

    }

    public void SetDestination(Vector3 desiredDestination) {
        Vector3 direction = (desiredDestination - owner.gameObject.transform.position).normalized;
        Vector3 destinationMinusOne = (desiredDestination - direction);

        SetDestination(Coordinates.FromWorldSpace(destinationMinusOne));
    }

    public void SetDestination(Coordinates desiredDestination) {
        if (pathRequested == true) return;
        if (Metrics.time < nextPathRequest) return;
        if (Coordinates.AreEqual(destination, desiredDestination) == true) return;

        pathRequested = true;
        nextPathRequest = Metrics.time + msBetweenPathRequests;

        Pathfinder.RequestPath(new PathRequest(Coordinates.FromWorldSpace(owner.gameObject.transform.position), desiredDestination, OnPathFound));
    }

    private void OnPathFound(PathNode[] newPath, bool pathFound) {
        pathRequested = false;
        index = 0;
        path = newPath;
    }

    public override void WriteXml(XmlWriter writer) {

    }

    public override void ReadXml(XmlReader reader) {

    }
}
