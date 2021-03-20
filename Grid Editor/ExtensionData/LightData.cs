using System.Xml;
using UnityEngine;

public class LightData : ExtensionData {

    private const float timeVariance = 33.3333f;
    private const float spaceVariance = 0.01f;

    private LightMode Mode;
    public LightMode mode {

        get {
            return Mode;
        }

        set {
            if (Mode == value) return;
            Mode = value;

            if (Mode == LightMode.Standard) {

                msBetweenCycles = 66.6666f;

                intensityMinimum = 2f;
                intensityMaximum = 2f;

            } else if (Mode == LightMode.Pulse) {

                msBetweenCycles = 16.6666f;

                intensityMinimum = 0f;
                intensityMaximum = 3f;

            } else if (Mode == LightMode.Strobe) {

                msBetweenCycles = 66.6666f;

                intensityMinimum = 0f;
                intensityMaximum = 3f;

            } else if (Mode == LightMode.Flicker) {

                msBetweenCycles = 33.3333f;

                intensityMinimum = 0f;
                intensityMaximum = 3f;

            } else if (Mode == LightMode.Burn) {

                msBetweenCycles = 33.3333f;

                intensityMinimum = 1.5f;
                intensityMaximum = 3f;

            }

        }

    }

    private Light lightComponent;
    private Vector3 localZero;

    private float nextCycleTime;
    private float msBetweenCycles;

    public float intensityMinimum;
    public float intensityMaximum;

    public float pulseSpeed;
    public float strobeSpeed;
    public float flickerBias;

    public float intensity {
        get {
            return intensityMaximum;
        }

        set {
            if (intensityMaximum == value) return;

            intensityMinimum = value;
            intensityMaximum = value;

            lightComponent.intensity = value;
        }
    }

    public override void Initialize(int ownerUniqueID) {
        this.ownerUniqueID = ownerUniqueID;
        identifier = PStrings.light;

        ExtensibleData owner = GridData.GetExtensible(ownerUniqueID);
        Mode = owner.lightMode;

        lightComponent = GridData.GetExtensible(ownerUniqueID).gameObject.GetComponentInChildren<Light>();
        localZero = lightComponent.transform.localPosition;

        nextCycleTime = -1f;
        msBetweenCycles = 66.6666f;

        intensityMinimum = 2f;
        intensityMaximum = 2f;

        pulseSpeed = 2f;
        strobeSpeed = 0f;
        flickerBias = 0.1f;
    }

    public override void FixedInstructionCycle() {

    }

    public override void InstructionCycle() {
        if (mode == LightMode.Null || mode == LightMode.Standard) return;
        if (Metrics.time < nextCycleTime) return;

        switch (mode) {
            case LightMode.Pulse:
                Pulse();
                break;
            case LightMode.Strobe:
                Strobe();
                break;
            case LightMode.Flicker:
                Flicker();
                break;
            case LightMode.Burn:
                Burn();
                break;
        }
    }

    public override void LateInstructionCycle() {

    }

    private void Pulse() {
        nextCycleTime = Metrics.time + msBetweenCycles / 1000f;
        lightComponent.intensity = Mathf.Lerp(intensityMinimum, intensityMaximum, ((Mathf.Sin(Metrics.time * pulseSpeed) + 1) * 0.5f));
    }

    private void Strobe() {
        nextCycleTime = Metrics.time + (msBetweenCycles + strobeSpeed) / 1000f;
        lightComponent.intensity = (lightComponent.intensity == intensityMinimum) ? intensityMaximum : intensityMinimum;
    }

    private void Flicker() {
        nextCycleTime = Metrics.time + (msBetweenCycles + Random.Range(-timeVariance, timeVariance)) / 1000f;
        lightComponent.intensity = Random.value < flickerBias ? intensityMaximum : intensityMinimum;
    }

    private void Burn() {
        nextCycleTime = Metrics.time + (msBetweenCycles + Random.Range(-timeVariance, timeVariance)) / 1000f;

        lightComponent.transform.localPosition = new Vector3(
                localZero.x + Random.Range(-spaceVariance, spaceVariance),
                localZero.y + Random.Range(-spaceVariance, spaceVariance),
                localZero.z + Random.Range(-spaceVariance, spaceVariance)
            );

        lightComponent.intensity = Random.Range(intensityMinimum, intensityMaximum);
    }

    public override void WriteXml(XmlWriter writer) {
        writer.WriteAttributeString("Mode", ((int)mode).ToString());

        writer.WriteAttributeString("IntensityMinimum", intensityMinimum.ToString());
        writer.WriteAttributeString("IntensityMaximum", intensityMaximum.ToString());

        writer.WriteAttributeString("PulseSpeed", pulseSpeed.ToString());
        writer.WriteAttributeString("StrobeSpeed", strobeSpeed.ToString());
        writer.WriteAttributeString("FlickerBias", flickerBias.ToString());
    }

    public override void ReadXml(XmlReader reader) {
        mode = (LightMode)int.Parse(reader.GetAttribute("Mode"));

        intensityMinimum = float.Parse(reader.GetAttribute("IntensityMinimum"));
        intensityMaximum = float.Parse(reader.GetAttribute("IntensityMaximum"));

        pulseSpeed = float.Parse(reader.GetAttribute("PulseSpeed"));
        strobeSpeed = float.Parse(reader.GetAttribute("StrobeSpeed"));
        flickerBias = float.Parse(reader.GetAttribute("FlickerBias"));
    }
}
