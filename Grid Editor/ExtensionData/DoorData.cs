using System.Xml;
using UnityEngine;

public class DoorData : ExtensionData {

    private const float speedMinimum = 20f;
    private const float speedMaximum = 1f;

    private const float sqrDstThreshold = 0.0005f;
    private const float trgDstThreshold = 2f;
    private static LayerMask mask = Metrics.entityMask;

    public DoorMode animation { get; private set; }

    private Transform modelA;
    private Vector3 localOpenedA;

    private Transform modelB;
    private Vector3 localOpenedB;

    private Vector3 center;
    private Vector3 halfExtents;

    private Collider[] collisions;

    private float sqrDstA;
    private float sqrDstB;

    private float timeTriggered;
    private float nextCycleTime;

    private DoorState state;

    private DoorActivation Activation;
    public DoorActivation activation {
        get {
            return Activation;
        }

        set {
            if (Activation == value) return;
            Activation = value;

            if (Activation == DoorActivation.Switch) {
                ObstacleData owner = GridData.GetObstacle(ownerUniqueID);
                if (owner == null) return;

                InteractableData ownerInteractableExtension = (InteractableData)owner.GetExtension(PStrings.interactable);
                if (ownerInteractableExtension == null) return;

                ownerInteractableExtension.SetTarget(ownerUniqueID);
            }
        }
    }
    public DoorKey key;
    public float closeSpeed;
    public float openSpeed;
    public float openTime;

    private float Percent(float timeStarted, float speed) {
        return (Metrics.time - timeStarted) / Mathf.Lerp(speedMinimum, speedMaximum, speed);
    }

    public override void Initialize(int ownerUniqueID) {
        this.ownerUniqueID = ownerUniqueID;
        identifier = PStrings.door;

        animation = DoorMode.Null;
        modelA = null;
        localOpenedA = Vector3.zero;
        modelB = null;
        localOpenedB = Vector3.zero;

        ObstacleData obstacleData = (ObstacleData)GridData.GetExtensible(ownerUniqueID);

        if (obstacleData.instructionSetID == PStrings.doorSingleSmall || obstacleData.instructionSetID == PStrings.doorSingleLarge) {

            animation = DoorMode.Single;

            modelA = obstacleData.gameObject.transform.GetChild(0).GetChild(1);
            localOpenedA = obstacleData.gameObject.transform.GetChild(0).GetChild(0).localPosition;

        } else if (obstacleData.instructionSetID == PStrings.doorDoubleSmall || obstacleData.instructionSetID == PStrings.doorDoubleLarge) {

            animation = DoorMode.Double;

            modelA = obstacleData.gameObject.transform.GetChild(0).GetChild(1);
            localOpenedA = obstacleData.gameObject.transform.GetChild(0).GetChild(0).localPosition;

            modelB = obstacleData.gameObject.transform.GetChild(1).GetChild(1);
            localOpenedB = obstacleData.gameObject.transform.GetChild(1).GetChild(0).localPosition;

        }

        halfExtents = new Vector3(obstacleData.orientedWidth, Metrics.wallHeight - Metrics.trimHeight, obstacleData.orientedLength) * 0.5f;
        center = obstacleData.gameObject.transform.position - (Vector3.right * 0.5f) - (Vector3.forward * 0.5f);
        if (obstacleData.orientation == Direction.S || obstacleData.orientation == Direction.E) center = new Vector3(center.x + halfExtents.x, center.y + halfExtents.y, center.z + halfExtents.z);
        else center = new Vector3((center.x - halfExtents.x) + 1, center.y + halfExtents.y, (center.z - halfExtents.z) + 1);

        halfExtents.x = (obstacleData.orientation == Direction.N || obstacleData.orientation == Direction.S) ? halfExtents.x + (trgDstThreshold * 0.5f) : halfExtents.x + trgDstThreshold;
        halfExtents.z = (obstacleData.orientation == Direction.N || obstacleData.orientation == Direction.S) ? halfExtents.z + trgDstThreshold : halfExtents.z + (trgDstThreshold * 0.5f);

        collisions = null;

        sqrDstA = 0;
        sqrDstB = 0;
        timeTriggered = 0;
        nextCycleTime = -1;

        state = DoorState.Closed;

        Activation = DoorActivation.Null;
        key = DoorKey.Null;
        closeSpeed = 10f;
        openSpeed = 10f;
        openTime = -1f;
    }

    public override void FixedInstructionCycle() {

    }

    public override void InstructionCycle() {
        if (animation == DoorMode.Null) return;
        if (animation == DoorMode.Physics) return;
        if (Metrics.time < nextCycleTime) return;

        StateMachine();
        Closing();
        Opening();
    }

    public override void LateInstructionCycle() {

    }

    private void StateMachine() {
        nextCycleTime = Metrics.time + Metrics.frameTime60;

        ProximityScan();
        // activation.Time function //
    }

    private void ProximityScan() {
        if (activation == DoorActivation.Switch || activation == DoorActivation.Time) return;

        collisions = Physics.OverlapBox(center, halfExtents, Quaternion.identity, mask);

        if (collisions.Length > 0) {
            Open();
        } else {
            Close();
        }
    }

    public override void Switch() {
        if (activation != DoorActivation.Switch) return;

        if (state == DoorState.Opened || state == DoorState.Opening) Close();
        else if (state == DoorState.Closed || state == DoorState.Closing) Open();
    }

    private void Close() {
        if (state == DoorState.Closed || state == DoorState.Closing) return;

        timeTriggered = Metrics.time;
        state = DoorState.Closing;
    }

    private void Closing() {
        if (state != DoorState.Closing) return;

        modelA.localPosition = Vector3.Lerp(modelA.localPosition, Vector3.zero, Percent(timeTriggered, closeSpeed));

        sqrDstA = modelA.localPosition.sqrMagnitude;
        if (sqrDstA <= sqrDstThreshold) {
            modelA.localPosition = Vector3.zero;
        }

        if (animation == DoorMode.Single) {
            if (sqrDstA <= sqrDstThreshold) state = DoorState.Closed;
            return;
        }

        modelB.localPosition = Vector3.Lerp(modelB.localPosition, Vector3.zero, Percent(timeTriggered, closeSpeed));

        sqrDstB = modelB.localPosition.sqrMagnitude;
        if (sqrDstB <= sqrDstThreshold) {
            modelB.localPosition = Vector3.zero;
        }

        if (sqrDstA <= sqrDstThreshold && sqrDstB == sqrDstThreshold) state = DoorState.Closed;
    }

    private void Open() {
        if (state == DoorState.Opened || state == DoorState.Opening) return;

        timeTriggered = Metrics.time;
        state = DoorState.Opening;
    }

    private void Opening() {
        if (state != DoorState.Opening) return;

        modelA.localPosition = Vector3.Lerp(modelA.localPosition, localOpenedA, Percent(timeTriggered, openSpeed));

        sqrDstA = (localOpenedA - modelA.localPosition).sqrMagnitude;
        if (sqrDstA <= sqrDstThreshold) {
            modelA.localPosition = localOpenedA;
        }

        if (animation == DoorMode.Single) {
            if (sqrDstA <= sqrDstThreshold) state = DoorState.Opened;
            return;
        }

        modelB.localPosition = Vector3.Lerp(modelB.localPosition, localOpenedB, Percent(timeTriggered, openSpeed));

        sqrDstB = (localOpenedB - modelB.localPosition).sqrMagnitude;
        if (sqrDstB <= sqrDstThreshold) {
            modelB.localPosition = localOpenedB;
        }

        if (sqrDstA <= sqrDstThreshold && sqrDstB == sqrDstThreshold) state = DoorState.Opened;
    }

    public override void WriteXml(XmlWriter writer) {
        writer.WriteAttributeString("Activation", ((int)activation).ToString());
        writer.WriteAttributeString("Key", ((int)key).ToString());

        writer.WriteAttributeString("CloseSpeed", closeSpeed.ToString());
        writer.WriteAttributeString("OpenSpeed", openSpeed.ToString());
        writer.WriteAttributeString("OpenTime", openTime.ToString());
    }

    public override void ReadXml(XmlReader reader) {
        activation = (DoorActivation)int.Parse(reader.GetAttribute("Activation"));
        key = (DoorKey)int.Parse(reader.GetAttribute("Key"));

        closeSpeed = float.Parse(reader.GetAttribute("CloseSpeed"));
        openSpeed = float.Parse(reader.GetAttribute("OpenSpeed"));
        openTime = float.Parse(reader.GetAttribute("OpenTime"));
    }
}
