using System.Xml;
using UnityEngine;

public class InteractableData : ExtensionData {

    public int targetUniqueID { get; private set; }
    private ExtensibleData target;

    private bool switchedOn;
    private float unswitchTime;

    private Transform switchT;
    private MeshRenderer switchR;

    private Material offMaterial;
    private Material onMaterial;

    public override void Initialize(int ownerUniqueID) {
        this.ownerUniqueID = ownerUniqueID;
        identifier = PStrings.interactable;
        targetUniqueID = -1;

        switchedOn = false;
        unswitchTime = -1;

        ObstacleData obstacleData = (ObstacleData)GridData.GetExtensible(ownerUniqueID);
        if (obstacleData == null) return;

        if (obstacleData.instructionSetID == PStrings.switchSimple) {
            switchT = obstacleData.gameObject.transform.GetChild(0).GetChild(0);
            switchR = switchT.GetComponent<MeshRenderer>();
        }
    }

    public override void FixedInstructionCycle() {

    }

    public override void InstructionCycle() {
        if (switchedOn == false) return;
        if (Metrics.time > unswitchTime) Unswitch();
    }

    public override void LateInstructionCycle() {

    }

    public void SetTarget(int uniqueID) {
        target = GridData.GetExtensible(uniqueID);
        if (target != null) targetUniqueID = target.uniqueID;
        else {
            targetUniqueID = -1;
            return;
        }

        if (switchT == null) return;

        DoorData doorData = (DoorData)target.GetExtension(PStrings.door);
        if (doorData == null) return;

        if (doorData.key == DoorKey.Null) {
            offMaterial = Statics.green;
            onMaterial = Statics.greenLight;
        } else if (doorData.key == DoorKey.RedCard || doorData.key == DoorKey.RedSkull) {
            offMaterial = Statics.red;
            onMaterial = Statics.redLight;
        } else if (doorData.key == DoorKey.BlueCard || doorData.key == DoorKey.BlueSkull) {
            offMaterial = Statics.blue;
            onMaterial = Statics.blueLight;
        } else if (doorData.key == DoorKey.YellowCard || doorData.key == DoorKey.YellowSkull) {
            offMaterial = Statics.yellow;
            onMaterial = Statics.yellowLight;
        }
    }

    public override void Switch() {
        if (targetUniqueID == -1) return;

        switchedOn = true;
        unswitchTime = Metrics.time + 1f;

        if (switchT != null) {
            switchT.localPosition = new Vector3(0, 0, -0.075f);
            switchR.material = onMaterial;
        }

        foreach(ExtensionData extension in target.GetExtensions().Values) {
            extension.Switch();
        }
    }

    private void Unswitch() {
        switchedOn = false;

        if (switchT != null) {
            switchT.localPosition = Vector3.zero;
            switchR.material = offMaterial;
        }
    }

    public override void WriteXml(XmlWriter writer) {
        writer.WriteAttributeString("TargetUniqueID", targetUniqueID.ToString());
    }

    public override void ReadXml(XmlReader reader) {
        SetTarget(int.Parse(reader.GetAttribute("TargetUniqueID")));
    }
}
