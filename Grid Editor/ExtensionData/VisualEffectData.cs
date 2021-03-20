using System.Xml;
using UnityEngine;

public class VisualEffectData : ExtensionData {
    private const float bobIntensity = 0.25f;

    private const float bobSpeedMinimum = 1f;
    private const float bobSpeedMaximum = 10f;

    private const float spinSpeedMinimum = 5f;
    private const float spinSpeedMaximum = 200f;

    private float nextCycleTime;
    private const float msBetweenCycles = Metrics.frameTime60;

    private Transform modelT;
    private Vector3 localEulerZero;
    private Vector3 localPositionZero;

    private bool Spinning;
    public bool spinning {
        get {
            return Spinning;
        }

        set {
            if (Spinning == value) return;
            Spinning = value;

            modelT.localEulerAngles = localEulerZero;
        }
    }

    private bool Bobbing;
    public bool bobbing {
        get {
            return Bobbing;
        }

        set {
            if (Bobbing == value) return;
            Bobbing = value;

            modelT.localPosition = localPositionZero;
        }
    }

    public float bobbingSpeed;
    public float spinningSpeed;

    public override void Initialize(int ownerUniqueID) {
        this.ownerUniqueID = ownerUniqueID;
        identifier = PStrings.visualEffects;

        nextCycleTime = -1f;

        ExtensibleData owner = GridData.GetExtensible(ownerUniqueID);
        if (owner != null && owner.gameObject != null) modelT = owner.gameObject.transform.GetChild(0);

        localEulerZero = (modelT != null) ? modelT.localEulerAngles : Vector3.zero;
        localPositionZero = (modelT != null) ? modelT.localPosition : Vector3.zero;

        Bobbing = true;
        Spinning = true;

        bobbingSpeed = 0.1f;
        spinningSpeed = 0.25f;
    }

    public override void FixedInstructionCycle() {

    }

    public override void InstructionCycle() {
        if (modelT == null) return;

        if (Metrics.time < nextCycleTime) return;
        nextCycleTime = Metrics.time + msBetweenCycles;

        if (spinning == true) Spin();
        if (bobbing == true) Bob();
    }

    public override void LateInstructionCycle() {

    }

    private void Spin() {
        modelT.localEulerAngles = modelT.localEulerAngles + (Vector3.up * (Metrics.deltaTime * Mathf.Lerp(spinSpeedMinimum, spinSpeedMaximum, spinningSpeed)));
    }

    private void Bob() {
        modelT.localPosition = localPositionZero + (Vector3.up * (Mathf.Sin(Metrics.time * Mathf.Lerp(bobSpeedMinimum, bobSpeedMaximum, bobbingSpeed)) * bobIntensity));
    }

    public override void WriteXml(XmlWriter writer) {
        writer.WriteAttributeString("Bobbing", (bobbing == true) ? 1.ToString() : 0.ToString());
        writer.WriteAttributeString("Spinning", (spinning == true) ? 1.ToString() : 0.ToString());

        writer.WriteAttributeString("BobbingSpeed", bobbingSpeed.ToString());
        writer.WriteAttributeString("SpinningSpeed", spinningSpeed.ToString());
    }

    public override void ReadXml(XmlReader reader) {
        bobbing = (int.Parse(reader.GetAttribute("Bobbing")) == 1);
        spinning = (int.Parse(reader.GetAttribute("Spinning")) == 1);

        bobbingSpeed = float.Parse(reader.GetAttribute("BobbingSpeed"));
        spinningSpeed = float.Parse(reader.GetAttribute("SpinningSpeed"));
    }
}
