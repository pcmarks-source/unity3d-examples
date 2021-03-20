using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Collections.Generic;
using UnityEngine;

public class EntityData : ExtensibleData, IDamageable, IXmlSerializable {

    public event System.Action OnDeath;
    private EntityController controller;

    public bool immortal = false;
    public bool respawns = false;
    public bool despawns = false;
    public bool blind = false;
    public bool deaf = false;

    public float respawnsAfter = -1f;
    public float despawnsAfter = -1f;

    public static EntityData Prototype(string instructionSetID, GameObject prefab, LightMode lightMode, bool useVFX) {
        EntityData prototype = new EntityData();

        prototype.instructionSetID = instructionSetID;
        prototype.prefab = prefab;

        prototype.lightMode = lightMode;
        prototype.useVFX = useVFX;

        return prototype;
    }

    public static EntityData Instantiate(EntityData prototype, CellData cell, Direction orientation, int uniqueID) {
        if (prototype != null) {
            EntityData instance = new EntityData();

            instance.uniqueID = uniqueID;

            instance.instructionSetID = prototype.instructionSetID;
            instance.prefab = prototype.prefab;
            instance.orientation = orientation;
            instance.cellData = cell;
            instance.cellData.entity = instance;

            instance.lightMode = prototype.lightMode;
            instance.useVFX = prototype.useVFX;

            instance.gameObject = GameObject.Instantiate(instance.prefab);
            instance.gameObject.name = instance.instructionSetID;
            instance.gameObject.transform.SetParent(Metrics.entityContainer.transform, true);
            instance.gameObject.transform.eulerAngles = new Vector3(0, Metrics.OrientationFromDirection[(int)instance.orientation], 0);
            instance.gameObject.transform.position = Coordinates.ToWorldSpace(instance.cellData.coordinates);

            GridData.DamageableInstantiated(instance.gameObject, instance);

            instance.extensions = new Dictionary<string, ExtensionData>();

            if (instance.instructionSetID == PStrings.playerSpawn) instance.extensions.Add(PStrings.entityController, new PlayerController());
            else if (instance.instructionSetID == PStrings.zombie) instance.extensions.Add(PStrings.entityController, new ZombieController());
            else if (instance.instructionSetID == PStrings.imp) instance.extensions.Add(PStrings.entityController, new ImpController());

            instance.controller = (EntityController)instance.GetExtension(PStrings.entityController);
            instance.extensions.Add(PStrings.waypointManager, new WaypointManagerData());
            instance.extensions.Add(PStrings.pathfindManager, new PathfindManagerData());

            if (instance.lightMode != LightMode.Null) instance.extensions.Add(PStrings.light, new LightData());
            if (instance.useVFX == true) instance.extensions.Add(PStrings.visualEffects, new VisualEffectData());

            GridData.EntityInstanciated(instance);

            foreach (ExtensionData extension in instance.extensions.Values) {
                extension.Initialize(uniqueID);
            }

            return instance;
        }

        if (cell.entity != null) {
            EntityData instance = cell.entity;

            cell.entity = null;
            GridData.DamageableDestroyed(instance.gameObject, instance);

            WaypointManagerData waypointManager = (WaypointManagerData)instance.GetExtension(PStrings.waypointManager);
            waypointManager.Purge();

            GameObject.Destroy(instance.gameObject);

            GridData.EntityDestroyed(instance);

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
        if (cell.item != null) return false;
        if (cell.entity != null && cell.entity != this) return false;

        return true;
    }

    public void SetOrientation(Direction direction) {
        orientation = direction;
        gameObject.transform.eulerAngles = new Vector3(0, Metrics.OrientationFromDirection[(int)orientation], 0);
    }

    public void FixedInstructionCycle() {
        if (controller.state.isAlive == false) return;

        foreach (ExtensionData extension in extensions.Values) {
            extension.FixedInstructionCycle();
        }
    }

    public void InstructionCycle() {
        if (controller.state.isAlive == false) return;

        foreach (ExtensionData extension in extensions.Values) {
            extension.InstructionCycle();
        }
    }

    public void LateInstructionCycle() {
        if (controller.state.isAlive == false) return;

        foreach (ExtensionData extension in extensions.Values) {
            extension.LateInstructionCycle();
        }
    }

    public void TakeDamage(float damage, Vector3 location, Vector3 direction, EntityData source) {
        if (controller.state.isAlive == false) return;

        controller.state.health -= Mathf.RoundToInt(damage);

        ParticleSystem newBloodSpurt = GameObject.Instantiate(Metrics.bloodSpurt, location, Quaternion.FromToRotation(Vector3.forward, direction)) as ParticleSystem;
        newBloodSpurt.name = gameObject.name + "'s " + Metrics.bloodSpurt.name;
        newBloodSpurt.transform.parent = Metrics.entityContainer.transform;
        GameObject.Destroy(newBloodSpurt.gameObject, Metrics.bloodSpurt.main.startLifetimeMultiplier);

        if (controller.state.health <= 0) Die();
    }

    protected void Die() {
        controller.state.isAlive = false;
        controller.state.health = 0;
        if (OnDeath != null) OnDeath();
        //GridData.InstantiateEntity(null, cellData, Direction.S, uniqueID);
        gameObject.SetActive(false);
    }

    public EntityData() {

    }

    public XmlSchema GetSchema() {
        return null;
    }

    public void WriteXml(XmlWriter writer) {
        writer.WriteAttributeString("Immortal", (immortal == true) ? 1.ToString() : 0.ToString());
        writer.WriteAttributeString("Respawns", (respawns == true) ? 1.ToString() : 0.ToString());
        writer.WriteAttributeString("Despawns", (despawns == true) ? 1.ToString() : 0.ToString());
        writer.WriteAttributeString("Blind", (blind == true) ? 1.ToString() : 0.ToString());
        writer.WriteAttributeString("Deaf", (deaf == true) ? 1.ToString() : 0.ToString());

        writer.WriteAttributeString("RespawnsAfter", respawnsAfter.ToString());
        writer.WriteAttributeString("DespawnsAfter", despawnsAfter.ToString());
    }

    public void ReadXml(XmlReader reader) {
        immortal = (int.Parse(reader.GetAttribute("Immortal")) == 1);
        respawns = (int.Parse(reader.GetAttribute("Respawns")) == 1);
        despawns = (int.Parse(reader.GetAttribute("Despawns")) == 1);
        blind = (int.Parse(reader.GetAttribute("Blind")) == 1);
        deaf = (int.Parse(reader.GetAttribute("Deaf")) == 1);

        respawnsAfter = float.Parse(reader.GetAttribute("RespawnsAfter"));
        despawnsAfter = float.Parse(reader.GetAttribute("DespawnsAfter"));
    }
}
