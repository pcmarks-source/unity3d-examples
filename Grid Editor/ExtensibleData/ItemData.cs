using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Collections.Generic;
using UnityEngine;

public class ItemData : ExtensibleData, IXmlSerializable {

    public bool infinite = false;
    public bool respawns = false;
    public bool despawns = false;

    public float respawnsAfter = -1f;
    public float despawnsAfter = -1f;

    public int amount;

    public static ItemData Prototype(string instructionSetID, GameObject prefab, int amount, LightMode lightMode, bool useVFX) {
        ItemData prototype = new ItemData();

        prototype.instructionSetID = instructionSetID;
        prototype.prefab = prefab;
        prototype.amount = amount;

        prototype.lightMode = lightMode;
        prototype.useVFX = useVFX;

        return prototype;
    }

    public static ItemData Instantiate(ItemData prototype, CellData cell, int uniqueID) {
        if (prototype != null) {
            ItemData instance = new ItemData();

            instance.uniqueID = uniqueID;

            instance.instructionSetID = prototype.instructionSetID;
            instance.prefab = prototype.prefab;
            instance.amount = prototype.amount;
            instance.cellData = cell;
            instance.cellData.item = instance;

            instance.lightMode = prototype.lightMode;
            instance.useVFX = prototype.useVFX;

            instance.gameObject = GameObject.Instantiate(instance.prefab);
            instance.gameObject.name = instance.instructionSetID;
            instance.gameObject.transform.SetParent(Metrics.itemContainer.transform, true);
            instance.gameObject.transform.eulerAngles = new Vector3(0, 180, 0);
            instance.gameObject.transform.position = Coordinates.ToWorldSpace(instance.cellData.coordinates);

            instance.extensions = new Dictionary<string, ExtensionData>();

            instance.extensions.Add(PStrings.waypointManager, new WaypointManagerData());
            instance.extensions.Add(PStrings.pathfindManager, new PathfindManagerData());

            if (instance.lightMode != LightMode.Null) instance.extensions.Add(PStrings.light, new LightData());
            if (instance.useVFX == true) instance.extensions.Add(PStrings.visualEffects, new VisualEffectData());

            GridData.ItemInstanciated(instance);

            foreach (ExtensionData extension in instance.extensions.Values) {
                extension.Initialize(uniqueID);
            }

            return instance;
        }

        if (cell.item != null) {
            ItemData instance = cell.item;

            cell.item = null;
            GameObject.Destroy(instance.gameObject);

            WaypointManagerData waypointManager = (WaypointManagerData)instance.extensions[PStrings.waypointManager];
            waypointManager.Purge();

            GridData.ItemDestroyed(instance);

            return instance;
        }

        return null;
    }

    public bool Validation(CellData cell) {
        int validation = 0;

        validation = (ValidateBasic(cell) == true) ? 1 : -1;

        if (validation > 0) return true;
        if (validation < 0) return false;

        return false;
    }

    private bool ValidateBasic(CellData cell) {
        if (cell == null) return false;
        if (cell.foundation.isSolid == false) return false;
        if (cell.structure != null) {
            if (cell.structure.instructionSetID != PStrings.doorway) {
                return false;
            }
        }
        if (cell.obstacle != null) return false;
        if (cell.item != null && cell.item != this) return false;
        if (cell.entity != null) return false;

        return true;
    }

    public void FixedInstructionCycle() {
        foreach (ExtensionData extension in extensions.Values) {
            extension.FixedInstructionCycle();
        }
    }

    public void InstructionCycle() {
        foreach (ExtensionData extension in extensions.Values) {
            extension.InstructionCycle();
        }
    }

    public void LateInstructionCycle() {
        foreach (ExtensionData extension in extensions.Values) {
            extension.LateInstructionCycle();
        }
    }

    public ItemData() {

    }

    public XmlSchema GetSchema() {
        return null;
    }

    public void WriteXml(XmlWriter writer) {
        writer.WriteAttributeString("Infinite", (infinite == true) ? 1.ToString() : 0.ToString());
        writer.WriteAttributeString("Respawns", (respawns == true) ? 1.ToString() : 0.ToString());
        writer.WriteAttributeString("Despawns", (despawns == true) ? 1.ToString() : 0.ToString());

        writer.WriteAttributeString("RespawnsAfter", respawnsAfter.ToString());
        writer.WriteAttributeString("DespawnsAfter", despawnsAfter.ToString());
    }

    public void ReadXml(XmlReader reader) {
        infinite = (int.Parse(reader.GetAttribute("Infinite")) == 1);
        respawns = (int.Parse(reader.GetAttribute("Respawns")) == 1);
        despawns = (int.Parse(reader.GetAttribute("Despawns")) == 1);

        respawnsAfter = float.Parse(reader.GetAttribute("RespawnsAfter"));
        despawnsAfter = float.Parse(reader.GetAttribute("DespawnsAfter"));
    }
}
