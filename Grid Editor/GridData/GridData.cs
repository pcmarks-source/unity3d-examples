using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Collections.Generic;
using UnityEngine;

public class GridData : IXmlSerializable {

    private static Dictionary<string, GameObject> modelPrototypes;

    private static Dictionary<string, FoundationData> foundationPrototypes;
    private static Dictionary<string, StructureData> structurePrototypes;
    private static Dictionary<string, CeilingData> ceilingPrototypes;

    private static Dictionary<string, ObstacleData> obstaclePrototypes;
    private static Dictionary<string, ItemData> itemPrototypes;
    private static Dictionary<string, EntityData> entityPrototypes;

    private static ChunkData[] chunks;
    private static CellData[] cells;

    private static Dictionary<int, ObstacleData> obstacles = new Dictionary<int, ObstacleData>();
    private static Dictionary<int, ItemData> items = new Dictionary<int, ItemData>();
    private static Dictionary<int, EntityData> entities = new Dictionary<int, EntityData>();
    private static List<WaypointData> waypoints = new List<WaypointData>();

    private static Dictionary<GameObject, IDamageable> damageables = new Dictionary<GameObject, IDamageable>();

    public static int width { get; private set; }
    public static int length { get; private set; }

    public static void Initialize() {
        GeneratePrototypeLibraries();
    }

    public static void FixedInstructionCycle() {
        for (int i = 0; i < chunks.Length; i++) {
            chunks[i].FixedInstructionCycle();
        }

        foreach(ObstacleData obstacle in obstacles.Values) {
            obstacle.FixedInstructionCycle();
        }

        foreach (ItemData item in items.Values) {
            item.FixedInstructionCycle();
        }

        foreach (EntityData entity in entities.Values) {
            entity.FixedInstructionCycle();
        }

        if (Metrics.testMode == false) {
            for (int i = 0; i < waypoints.Count; i++) {
                waypoints[i].FixedInstructionCycle();
            }
        }
    }

    public static void InstructionCycle() {
        for (int i = 0; i < chunks.Length; i++) {
            chunks[i].InstructionCycle();
        }

        foreach(ObstacleData obstacle in obstacles.Values) {
            obstacle.InstructionCycle();
        }

        foreach (ItemData item in items.Values) {
            item.InstructionCycle();
        }

        foreach (EntityData entity in entities.Values) {
            entity.InstructionCycle();
        }

        if (Metrics.testMode == false) {
            for (int i = 0; i < waypoints.Count; i++) {
                waypoints[i].InstructionCycle();
            }
        }
    }

    public static void LateInstructionCycle() {
        for (int i = 0; i < chunks.Length; i++) {
            chunks[i].LateInstructionCycle();
        }

        foreach(ObstacleData obstacle in obstacles.Values) {
            obstacle.LateInstructionCycle();
        }

        foreach (ItemData item in items.Values) {
            item.LateInstructionCycle();
        }

        foreach (EntityData entity in entities.Values) {
            entity.LateInstructionCycle();
        }

        if (Metrics.testMode == false) {
            for (int i = 0; i < waypoints.Count; i++) {
                waypoints[i].LateInstructionCycle();
            }
        }
    }

    private static void GeneratePrototypeLibraries() {

        #region Models

        BuildModelPrototypes();

        #endregion

        #region Foundations

        foundationPrototypes = new Dictionary<string, FoundationData>();

        BuildFoundationPrototype(PStrings.pit);
        BuildFoundationPrototype(PStrings.pitGrate);

        BuildFoundationPrototype(PStrings.pool);
        BuildFoundationPrototype(PStrings.poolGrate);

        BuildFoundationPrototype(PStrings.ground);
        BuildFoundationPrototype(PStrings.groundGrass);

        #endregion

        #region Structures

        structurePrototypes = new Dictionary<string, StructureData>();

        BuildStructurePrototype(PStrings.wall);
        BuildStructurePrototype(PStrings.window);
        BuildStructurePrototype(PStrings.doorway);

        BuildStructurePrototype(PStrings.cage);

        #endregion

        #region Ceilings

        ceilingPrototypes = new Dictionary<string, CeilingData>();

        BuildCeilingPrototype(PStrings.ceiling);

        #endregion

        #region Obstacles

        obstaclePrototypes = new Dictionary<string, ObstacleData>();

        //////////////////// Lights ////////////////////

        BuildObstaclePrototype(PStrings.torch, 1, 1, LightMode.Burn, DoorMode.Null, false, false);
        BuildObstaclePrototype(PStrings.lamp, 1, 1, LightMode.Standard, DoorMode.Null, false, false);

        //////////////////// Doors /////////////////////

        BuildObstaclePrototype(PStrings.doorPhysicsSmall, 1, 1, LightMode.Null, DoorMode.Physics, false, false);
        BuildObstaclePrototype(PStrings.doorPhysicsLarge, 2, 1, LightMode.Null, DoorMode.Physics, false, false);

        //////////////////// Switches //////////////////

        BuildObstaclePrototype(PStrings.switchSimple, 1, 1, LightMode.Null, DoorMode.Null, true, false);

        //////////////////// Statics ///////////////////

        BuildObstaclePrototype(PStrings.bench, 2, 1, LightMode.Null, DoorMode.Null, false, false);
        BuildObstaclePrototype(PStrings.table, 2, 1, LightMode.Null, DoorMode.Null, false, false);

        #endregion

        #region Items

        itemPrototypes = new Dictionary<string, ItemData>();

        BuildItemPrototype(PStrings.shotgun, 1, LightMode.Null, true);
        BuildItemPrototype(PStrings.medLarge, 25, LightMode.Null, true);

        #endregion

        #region Entities

        entityPrototypes = new Dictionary<string, EntityData>();

        BuildEntityPrototype(PStrings.playerSpawn, LightMode.Null, false);
        BuildEntityPrototype(PStrings.zombie, LightMode.Null, false);

        #endregion

        #region UI & Debug

        GameObject.FindObjectOfType<GridEditorUI>().BuildPanels(
            foundationPrototypes,
            structurePrototypes,
            ceilingPrototypes,
            obstaclePrototypes,
            itemPrototypes,
            entityPrototypes
            );

        StatsForNerds.UpdatePrototypeCount(
            foundationPrototypes.Count +
            structurePrototypes.Count +
            ceilingPrototypes.Count +
            obstaclePrototypes.Count +
            itemPrototypes.Count +
            entityPrototypes.Count
            );

        if (Metrics.debug == true) Debug.Log("Prototype libraries generated with " + (
            foundationPrototypes.Count +
            structurePrototypes.Count +
            ceilingPrototypes.Count +
            obstaclePrototypes.Count +
            itemPrototypes.Count +
            entityPrototypes.Count
            ) + " entries.");

        #endregion

    }

    public static void GenerateGrid(int chunkCountX, int chunkCountZ) {
        if (Metrics.gridObject != null) Metrics.Destroy(Metrics.gridObject);
        Metrics.gridObject = new GameObject("Grid");
        Metrics.gridObject.AddComponent<Pathfinder>();
        PathGrid.Invalidate();

        obstacles.Clear();
        Metrics.obstacleContainer = new GameObject("Obstacles");
        Metrics.obstacleContainer.transform.SetParent(Metrics.gridObject.transform, true);

        items.Clear();
        Metrics.itemContainer = new GameObject("Items");
        Metrics.itemContainer.transform.SetParent(Metrics.gridObject.transform, true);

        entities.Clear();
        Metrics.entityContainer = new GameObject("Entities");
        Metrics.entityContainer.transform.SetParent(Metrics.gridObject.transform, true);

        waypoints.Clear();
        Metrics.waypointContainer = new GameObject("Waypoints");
        Metrics.waypointContainer.transform.SetParent(Metrics.gridObject.transform, true);

        damageables.Clear();

        Metrics.uniqueID = 0;

        Metrics.chunkCountX = chunkCountX;
        Metrics.chunkCountZ = chunkCountZ;

        width = Metrics.chunkCountX * Metrics.chunkWidth;
        length = Metrics.chunkCountZ * Metrics.chunkLength;

        GenerateChunks();
        GenerateCells();

        Metrics.CenterCamera();

        if (Metrics.debug == true) Debug.Log("Grid generated with " + (cells.Length) + " cells across " + (chunks.Length) + " chunks.");
    }

    private static void GenerateChunks() {
        GameObject chunkObject;
        chunks = new ChunkData[Metrics.chunkCountX * Metrics.chunkCountZ];
        int i = 0;

        for (int z = 0; z < Metrics.chunkCountZ; z++) {
            for (int x = 0; x < Metrics.chunkCountX; x++) {

                chunkObject = new GameObject("Chunk (" + x.ToString() + ", " + z.ToString() + ")");
                chunkObject.transform.SetParent(Metrics.gridObject.transform);

                chunks[i] = new ChunkData(
                        new MeshGenerator("Grass", chunkObject.transform, x, z, false, true, false),
                        new MeshGenerator("Fog", chunkObject.transform, x, z, true, true, false),
                        new MeshGenerator("Liquid", chunkObject.transform, x, z, true, true, false),
                        new MeshGenerator("Foundation", chunkObject.transform, x, z, true, true, true),
                        new MeshGenerator("Structure", chunkObject.transform, x, z, true, true, true),
                        new MeshGenerator("Ceiling", chunkObject.transform, x, z, true, true, true),
                        new MeshGenerator("CeilingMask", chunkObject.transform, x, z, true, true, false)
                    );

                i++;
            }
        }

        StatsForNerds.UpdateChunkCount(chunks.Length);
    }

    private static void GenerateCells() {
        cells = new CellData[width * length];
        int chunkCoordinateX, chunkCoordinateZ;
        int cellCoordinateX, cellCoordinateZ;
        int i = 0;

        for (int z = 0; z < length; z++) {
            for (int x = 0; x < width; x++) {

                chunkCoordinateX = x / Metrics.chunkWidth;
                chunkCoordinateZ = z / Metrics.chunkLength;

                cellCoordinateX = x - chunkCoordinateX * Metrics.chunkWidth;
                cellCoordinateZ = z - chunkCoordinateZ * Metrics.chunkLength;

                ChunkData chunk = chunks[chunkCoordinateX + chunkCoordinateZ * Metrics.chunkCountX];
                Coordinates coordinates = new Coordinates(x, z);
                CellData cell = new CellData(chunk, coordinates);

                chunk.AddCell(cellCoordinateX + cellCoordinateZ * Metrics.chunkWidth, cell);
                cells[i] = cell;

                if (x > 0) cell.SetNeighbor(Direction.W, cells[i - 1]);
                if (z > 0) cell.SetNeighbor(Direction.S, cells[i - width]);
                if (x > 0 && z > 0) cell.SetNeighbor(Direction.SW, cells[i - width - 1]);
                if (x < width && z > 0) cell.SetNeighbor(Direction.SE, cells[i - width + 1]);

                InstantiateFoundation(GetFoundationPrototype(PStrings.ground), cell);
                cell.foundation.materialIndex = (int)MatI.Boards;
                cell.foundation.colorIndex = (int)ColI.Brown;

                i++;
            }
        }

        StatsForNerds.UpdateCellCount(cells.Length);
    }

    public static CellData GetCellAt(Coordinates coordinates) {
        if (cells == null || cells.Length <= 0) return null;
        if (coordinates.x < 0 || coordinates.x >= width) return null;
        if (coordinates.z < 0 || coordinates.z >= length) return null;

        return cells[coordinates.x + coordinates.z * width];
    }

    public static ExtensibleData GetExtensible(int uniqueID) {
        if (obstacles.ContainsKey(uniqueID) == true) return obstacles[uniqueID];
        else if (items.ContainsKey(uniqueID) == true) return items[uniqueID];
        else if (entities.ContainsKey(uniqueID) == true) return entities[uniqueID];
        else return null;
    }

    private static void BuildModelPrototypes() {
        modelPrototypes = new Dictionary<string, GameObject>();

        GameObject[] models;
        GameObject model;
        string id;
        int i;

        models = Resources.LoadAll<GameObject>("Models/Obstacles/");

        for (i = 0; i < models.Length; i++) {
            model = models[i];
            id = model.name;
            modelPrototypes.Add(id, model);
        }

        models = Resources.LoadAll<GameObject>("Models/Items/");

        for (i = 0; i < models.Length; i++) {
            model = models[i];
            id = model.name;
            modelPrototypes.Add(id, model);
        }

        models = Resources.LoadAll<GameObject>("Models/Entities/");

        for (i = 0; i < models.Length; i++) {
            model = models[i];
            id = model.name;
            modelPrototypes.Add(id, model);
        }
    }

    private static GameObject GetModelPrototype(string id) {
        if (string.IsNullOrEmpty(id)) return null;
        if (modelPrototypes == null || modelPrototypes.Count <= 0) return null;
        if (modelPrototypes.ContainsKey(id) == false) return null;

        return modelPrototypes[id];
    }

    private static void BuildFoundationPrototype(string id) {
        foundationPrototypes.Add(id, FoundationData.Prototype(id));
    }

    public static FoundationData GetFoundationPrototype(string id) {
        if (string.IsNullOrEmpty(id)) return null;
        if (foundationPrototypes == null || foundationPrototypes.Count <= 0) return null;
        if (foundationPrototypes.ContainsKey(id) == false) return null;

        return foundationPrototypes[id];
    }

    public static void InstantiateFoundation(FoundationData prototype, CellData cell) {
        FoundationData.Instantiate(prototype, cell);
        PathGrid.Invalidate();
    }

    private static void BuildStructurePrototype(string id) {
        structurePrototypes.Add(id, StructureData.Prototype(id));
    }

    public static StructureData GetStructurePrototype(string id) {
        if (string.IsNullOrEmpty(id)) return null;
        if (structurePrototypes == null || structurePrototypes.Count <= 0) return null;
        if (structurePrototypes.ContainsKey(id) == false) return null;

        return structurePrototypes[id];
    }

    public static void InstantiateStructure(StructureData prototype, CellData cell) {
        StructureData.Instantiate(prototype, cell);
        PathGrid.Invalidate();
    }

    private static void BuildCeilingPrototype(string id) {
        ceilingPrototypes.Add(id, CeilingData.Prototype(id));
    }

    public static CeilingData GetCeilingPrototype(string id) {
        if (string.IsNullOrEmpty(id)) return null;
        if (ceilingPrototypes == null || ceilingPrototypes.Count <= 0) return null;
        if (ceilingPrototypes.ContainsKey(id) == false) return null;

        return ceilingPrototypes[id];
    }

    public static void InstantiateCeiling(CeilingData prototype, CellData cell) {
        CeilingData.Instantiate(prototype, cell);
        PathGrid.Invalidate();
    }

    private static void BuildObstaclePrototype(string instructionSetID, int width, int length, LightMode lightMode, DoorMode doorMode, bool isInteractable, bool useVFX) {
        obstaclePrototypes.Add(instructionSetID, ObstacleData.Prototype(instructionSetID, GetModelPrototype(instructionSetID), width, length, lightMode, doorMode, isInteractable, useVFX));
    }

    public static ObstacleData GetObstaclePrototype(string id) {
        if (string.IsNullOrEmpty(id)) return null;
        if (obstaclePrototypes == null || obstaclePrototypes.Count <= 0) return null;
        if (obstaclePrototypes.ContainsKey(id) == false) return null;

        return obstaclePrototypes[id];
    }

    public static void ObstacleInstanciated(ObstacleData instance) {
        obstacles.Add(instance.uniqueID, instance);
        PathGrid.Invalidate();
        StatsForNerds.UpdateObstacleCount(obstacles.Count);
    }

    public static void ObstacleDestroyed(ObstacleData instance) {
        obstacles.Remove(instance.uniqueID);
        PathGrid.Invalidate();
        StatsForNerds.UpdateObstacleCount(obstacles.Count);
    }

    public static ObstacleData GetObstacle(int uniqueID) {
        if (obstacles == null || obstacles.Count <= 0) return null;
        if (obstacles.ContainsKey(uniqueID) == false) return null;

        return obstacles[uniqueID];
    }

    private static void BuildItemPrototype(string id, int amount, LightMode lightMode, bool useVFX) {
        itemPrototypes.Add(id, ItemData.Prototype(id, GetModelPrototype(id), amount, lightMode, useVFX));
    }

    public static ItemData GetItemPrototype(string id) {
        if (string.IsNullOrEmpty(id)) return null;
        if (itemPrototypes == null || itemPrototypes.Count <= 0) return null;
        if (itemPrototypes.ContainsKey(id) == false) return null;

        return itemPrototypes[id];
    }

    public static void ItemInstanciated(ItemData instance) {
        items.Add(instance.uniqueID, instance);
        PathGrid.Invalidate();
        StatsForNerds.UpdateItemCount(items.Count);
    }

    public static void ItemDestroyed(ItemData instance) {
        items.Remove(instance.uniqueID);
        PathGrid.Invalidate();
        StatsForNerds.UpdateItemCount(items.Count);
    }

    public static ItemData GetItem(int uniqueID) {
        if (items == null || items.Count <= 0) return null;
        if (items.ContainsKey(uniqueID) == false) return null;

        return items[uniqueID];
    }

    private static void BuildEntityPrototype(string id, LightMode lightMode, bool useVFX) {
        entityPrototypes.Add(id, EntityData.Prototype(id, GetModelPrototype(id), lightMode, useVFX));
    }

    public static EntityData GetEntityPrototype(string id) {
        if (string.IsNullOrEmpty(id)) return null;
        if (entityPrototypes == null || entityPrototypes.Count <= 0) return null;
        if (entityPrototypes.ContainsKey(id) == false) return null;

        return entityPrototypes[id];
    }

    public static void EntityInstanciated(EntityData instance) {
        entities.Add(instance.uniqueID, instance);
        StatsForNerds.UpdateEntityCount(entities.Count);
    }

    public static void EntityDestroyed(EntityData instance) {
        entities.Remove(instance.uniqueID);
        StatsForNerds.UpdateEntityCount(entities.Count);
    }

    public static EntityData GetEntity(int uniqueID) {
        if (entities == null || entities.Count <= 0) return null;
        if (entities.ContainsKey(uniqueID) == false) return null;

        return entities[uniqueID];
    }

    public static void WaypointVisuals(bool toggle) {
        if (waypoints == null || waypoints.Count <= 0) return;

        for (int i = 0; i < waypoints.Count; i++) {
            waypoints[i].ShowPath(toggle);
        }
    }

    public static void WaypointInstantiated(WaypointData waypoint) {
        if (waypoints == null) return;
        if (waypoints.Contains(waypoint) == true) return;

        waypoints.Add(waypoint);
    }

    public static void WaypointDestroyed(WaypointData waypoint) {
        if (waypoints == null || waypoints.Count <= 0) return;
        if (waypoints.Contains(waypoint) == false) return;

        waypoints.Remove(waypoint);
    }

    public static void DamageableInstantiated(GameObject gameObject, IDamageable damageable) {
        if (gameObject == null) return;
        if (damageable == null) return;
        if (damageables == null) return;
        if (damageables.ContainsKey(gameObject) == true) return;
        if (damageables.ContainsValue(damageable) == true) return;

        damageables.Add(gameObject, damageable);
    }

    public static void DamageableDestroyed(GameObject gameObject, IDamageable damageable) {
        if (gameObject == null) return;
        if (damageable == null) return;
        if (damageables == null || damageables.Count <= 0) return;
        if (damageables.ContainsKey(gameObject) == false) return;
        if (damageables.ContainsValue(damageable) == false) return;

        damageables.Remove(gameObject);
    }

    public static IDamageable GetDamageable(GameObject gameObject) {
        if (gameObject == null) return null;
        if (damageables == null || damageables.Count <= 0) return null;
        if (damageables.ContainsKey(gameObject) == false) return null;

        return damageables[gameObject];
    }

    public GridData() {

    }

    public XmlSchema GetSchema() {
        return null;
    }

    public void WriteXml(XmlWriter writer) {
        List<FoundationData> foundations = new List<FoundationData>();
        List<StructureData> structures = new List<StructureData>();
        List<CeilingData> ceilings = new List<CeilingData>();
        List<ExtensionData> extensions = new List<ExtensionData>();

        #region GridData

        writer.WriteAttributeString("VersionHeader", Metrics.version.ToString());

        writer.WriteAttributeString(PStrings.gridWidth, Metrics.chunkCountX.ToString());
        writer.WriteAttributeString(PStrings.gridLength, Metrics.chunkCountZ.ToString());

        #endregion

        #region CellDatas

        writer.WriteStartElement(PStrings.cellDatas);

        for (int i = 0; i < cells.Length; i++) {
            writer.WriteStartElement(PStrings.cellData);

            writer.WriteAttributeString(PStrings.identifier, i.ToString());
            cells[i].WriteXml(writer);

            if (cells[i].foundation != null) foundations.Add(cells[i].foundation);
            if (cells[i].structure != null) structures.Add(cells[i].structure);
            if (cells[i].ceiling != null) ceilings.Add(cells[i].ceiling);

            writer.WriteEndElement();
        }

        writer.WriteEndElement();

        #endregion

        #region FoundationDatas

        writer.WriteStartElement(PStrings.foundationDatas);

        for (int i = 0; i < foundations.Count; i++) {
            writer.WriteStartElement(PStrings.foundationData);

            writer.WriteAttributeString(PStrings.xCoordinate, foundations[i].cellData.coordinates.x.ToString());
            writer.WriteAttributeString(PStrings.zCoordinate, foundations[i].cellData.coordinates.z.ToString());

            writer.WriteAttributeString(PStrings.identifier, foundations[i].instructionSetID);

            foundations[i].WriteXml(writer);

            writer.WriteEndElement();
        }

        writer.WriteEndElement();

        #endregion

        #region StructureDatas

        writer.WriteStartElement(PStrings.structureDatas);

        for (int i = 0; i < structures.Count; i++) {
            writer.WriteStartElement(PStrings.structureData);

            writer.WriteAttributeString(PStrings.xCoordinate, structures[i].cellData.coordinates.x.ToString());
            writer.WriteAttributeString(PStrings.zCoordinate, structures[i].cellData.coordinates.z.ToString());

            writer.WriteAttributeString(PStrings.identifier, structures[i].instructionSetID);

            structures[i].WriteXml(writer);

            writer.WriteEndElement();
        }

        writer.WriteEndElement();

        #endregion

        #region CeilingDatas

        writer.WriteStartElement(PStrings.ceilingDatas);

        for (int i = 0; i < ceilings.Count; i++) {
            writer.WriteStartElement(PStrings.ceilingdata);

            writer.WriteAttributeString(PStrings.xCoordinate, ceilings[i].cellData.coordinates.x.ToString());
            writer.WriteAttributeString(PStrings.zCoordinate, ceilings[i].cellData.coordinates.z.ToString());

            writer.WriteAttributeString(PStrings.identifier, ceilings[i].instructionSetID);

            ceilings[i].WriteXml(writer);

            writer.WriteEndElement();
        }

        writer.WriteEndElement();

        #endregion

        #region ObstacleDatas

        writer.WriteStartElement(PStrings.obstacleDatas);

        foreach(ObstacleData obstacle in obstacles.Values) {
            writer.WriteStartElement(PStrings.obstacleData);

            writer.WriteAttributeString(PStrings.xCoordinate, obstacle.cellData.coordinates.x.ToString());
            writer.WriteAttributeString(PStrings.zCoordinate, obstacle.cellData.coordinates.z.ToString());
            writer.WriteAttributeString(PStrings.orientation, ((int)obstacle.orientation).ToString());

            writer.WriteAttributeString(PStrings.identifier, obstacle.instructionSetID);

            writer.WriteAttributeString(PStrings.uniqueID, obstacle.uniqueID.ToString());

            obstacle.WriteXml(writer);

            foreach(ExtensionData extension in obstacle.GetExtensions().Values) {
                extensions.Add(extension);
            }

            writer.WriteEndElement();
        }

        writer.WriteEndElement();

        #endregion

        #region ItemDatas

        writer.WriteStartElement(PStrings.itemDatas);

        foreach(ItemData item in items.Values) {
            writer.WriteStartElement(PStrings.itemData);

            writer.WriteAttributeString(PStrings.xCoordinate, item.cellData.coordinates.x.ToString());
            writer.WriteAttributeString(PStrings.zCoordinate, item.cellData.coordinates.z.ToString());

            writer.WriteAttributeString(PStrings.identifier, item.instructionSetID);

            writer.WriteAttributeString(PStrings.uniqueID, item.uniqueID.ToString());

            item.WriteXml(writer);

            foreach (ExtensionData extension in item.GetExtensions().Values) {
                extensions.Add(extension);
            }

            writer.WriteEndElement();
        }

        writer.WriteEndElement();

        #endregion

        #region EntityDatas

        writer.WriteStartElement(PStrings.entityDatas);

        foreach(EntityData entity in entities.Values) {
            writer.WriteStartElement(PStrings.entityData);


            writer.WriteAttributeString(PStrings.xCoordinate, entity.cellData.coordinates.x.ToString());
            writer.WriteAttributeString(PStrings.zCoordinate, entity.cellData.coordinates.z.ToString());
            writer.WriteAttributeString(PStrings.orientation, ((int)entity.orientation).ToString());

            writer.WriteAttributeString(PStrings.identifier, entity.instructionSetID);

            writer.WriteAttributeString(PStrings.uniqueID, entity.uniqueID.ToString());

            entity.WriteXml(writer);

            foreach (ExtensionData extension in entity.GetExtensions().Values) {
                extensions.Add(extension);
            }

            writer.WriteEndElement();
        }

        writer.WriteEndElement();

        #endregion

        #region Extensions

        writer.WriteStartElement(PStrings.extensionDatas);

        for (int i = 0; i < extensions.Count; i++) {
            writer.WriteStartElement(PStrings.extensionData);

            writer.WriteAttributeString(PStrings.uniqueID, extensions[i].ownerUniqueID.ToString());
            writer.WriteAttributeString(PStrings.identifier, extensions[i].identifier);

            extensions[i].WriteXml(writer);

            writer.WriteEndElement();
        }

        writer.WriteEndElement();

        #endregion

        #region Waypoints

        writer.WriteStartElement(PStrings.waypointDatas);

        for (int i = 0; i < waypoints.Count; i++) {
            writer.WriteStartElement(PStrings.waypointData);

            writer.WriteAttributeString(PStrings.xCoordinate, waypoints[i].cellData.coordinates.x.ToString());
            writer.WriteAttributeString(PStrings.zCoordinate, waypoints[i].cellData.coordinates.z.ToString());
            writer.WriteAttributeString(PStrings.orientation, ((int)waypoints[i].orientation).ToString());

            writer.WriteAttributeString(PStrings.uniqueID, waypoints[i].ownerUniqueID.ToString());

            writer.WriteEndElement();
        }

        writer.WriteEndElement();

        #endregion

    }

    public void ReadXml(XmlReader reader) {
        int version = int.Parse(reader.GetAttribute("VersionHeader"));

        int chunkCountX = int.Parse(reader.GetAttribute(PStrings.gridWidth));
        int chunkCountZ = int.Parse(reader.GetAttribute(PStrings.gridLength));

        GenerateGrid(chunkCountX, chunkCountZ);

        while (reader.Read()) {
            switch (reader.Name) {
                case PStrings.cellDatas:
                    ReadXmlCellDatas(reader);
                    break;
                case PStrings.foundationDatas:
                    ReadXmlFoundationDatas(reader);
                    break;
                case PStrings.structureDatas:
                    ReadXmlStructureDatas(reader);
                    break;
                case PStrings.ceilingDatas:
                    ReadXmlCeilingDatas(reader);
                    break;
                case PStrings.obstacleDatas:
                    ReadXmlObstacleDatas(reader);
                    break;
                case PStrings.itemDatas:
                    ReadXmlItemDatas(reader);
                    break;
                case PStrings.entityDatas:
                    ReadXmlEntityDatas(reader);
                    break;
                case PStrings.extensionDatas:
                    ReadXmlExtensionDatas(reader);
                    break;
                case PStrings.waypointDatas:
                    ReadXmlWaypointDatas(reader);
                    break;
            }
        }
    }

    private static void ReadXmlCellDatas(XmlReader reader) {
        int i;
        CellData cellData;

        if (reader.ReadToDescendant(PStrings.cellData) == true) {

            do {

                i = int.Parse(reader.GetAttribute(PStrings.identifier));
                cellData = cells[i];
                cellData.ReadXml(reader);

            } while (reader.ReadToNextSibling(PStrings.cellData));

        }

    }

    private static void ReadXmlFoundationDatas(XmlReader reader) {
        int x, z;
        string identifier;
        CellData cellData;

        if (reader.ReadToDescendant(PStrings.foundationData) == true) {

            do {

                
                x = int.Parse(reader.GetAttribute(PStrings.xCoordinate));
                z = int.Parse(reader.GetAttribute(PStrings.zCoordinate));
                identifier = reader.GetAttribute(PStrings.identifier);

                cellData = GetCellAt(new Coordinates(x, z));

                InstantiateFoundation(GetFoundationPrototype(identifier), cellData);
                cellData.foundation.ReadXml(reader);


            } while (reader.ReadToNextSibling(PStrings.foundationData));

        }
    }

    private static void ReadXmlStructureDatas(XmlReader reader) {
        int x, z;
        string identifier;
        CellData cellData;

        if (reader.ReadToDescendant(PStrings.structureData) == true) {

            do {


                x = int.Parse(reader.GetAttribute(PStrings.xCoordinate));
                z = int.Parse(reader.GetAttribute(PStrings.zCoordinate));
                identifier = reader.GetAttribute(PStrings.identifier);

                cellData = GetCellAt(new Coordinates(x, z));

                InstantiateStructure(GetStructurePrototype(identifier), cellData);
                cellData.structure.ReadXml(reader);


            } while (reader.ReadToNextSibling(PStrings.structureData));

        }
    }

    private static void ReadXmlCeilingDatas(XmlReader reader) {
        int x, z;
        string identifier;
        CellData cellData;

        if (reader.ReadToDescendant(PStrings.ceilingdata) == true) {

            do {


                x = int.Parse(reader.GetAttribute(PStrings.xCoordinate));
                z = int.Parse(reader.GetAttribute(PStrings.zCoordinate));
                identifier = reader.GetAttribute(PStrings.identifier);

                cellData = GetCellAt(new Coordinates(x, z));

                InstantiateCeiling(GetCeilingPrototype(identifier), cellData);
                cellData.ceiling.ReadXml(reader);


            } while (reader.ReadToNextSibling(PStrings.ceilingdata));

        }
    }

    private static void ReadXmlObstacleDatas(XmlReader reader) {
        int uniqueID;
        string identifier;
        int x, z;
        Direction orientation;
        CellData cellData;

        if (reader.ReadToDescendant(PStrings.obstacleData) == true) {

            do {

                x = int.Parse(reader.GetAttribute(PStrings.xCoordinate));
                z = int.Parse(reader.GetAttribute(PStrings.zCoordinate));
                orientation = (Direction)int.Parse(reader.GetAttribute(PStrings.orientation));

                identifier = reader.GetAttribute(PStrings.identifier);

                uniqueID = int.Parse(reader.GetAttribute(PStrings.uniqueID));

                cellData = GetCellAt(new Coordinates(x, z));

                ObstacleData.Instantiate(GetObstaclePrototype(identifier), cellData, orientation, uniqueID);
                cellData.obstacle.ReadXml(reader);


            } while (reader.ReadToNextSibling(PStrings.obstacleData));

        }
    }

    private static void ReadXmlItemDatas(XmlReader reader) {
        int uniqueID;
        string identifier;
        int x, z;
        CellData cellData;

        if (reader.ReadToDescendant(PStrings.itemData) == true) {

            do {

                x = int.Parse(reader.GetAttribute(PStrings.xCoordinate));
                z = int.Parse(reader.GetAttribute(PStrings.zCoordinate));

                identifier = reader.GetAttribute(PStrings.identifier);

                uniqueID = int.Parse(reader.GetAttribute(PStrings.uniqueID));

                cellData = GetCellAt(new Coordinates(x, z));

                ItemData.Instantiate(GetItemPrototype(identifier), cellData, uniqueID);
                cellData.item.ReadXml(reader);


            } while (reader.ReadToNextSibling(PStrings.itemData));

        }
    }

    private static void ReadXmlEntityDatas(XmlReader reader) {
        int uniqueID;
        string identifier;
        int x, z;
        Direction orientation;
        CellData cellData;

        if (reader.ReadToDescendant(PStrings.entityData) == true) {

            do {

                x = int.Parse(reader.GetAttribute(PStrings.xCoordinate));
                z = int.Parse(reader.GetAttribute(PStrings.zCoordinate));
                orientation = (Direction)int.Parse(reader.GetAttribute(PStrings.orientation));

                identifier = reader.GetAttribute(PStrings.identifier);

                uniqueID = int.Parse(reader.GetAttribute(PStrings.uniqueID));

                cellData = GetCellAt(new Coordinates(x, z));

                EntityData.Instantiate(GetEntityPrototype(identifier), cellData, orientation, uniqueID);
                cellData.entity.ReadXml(reader);


            } while (reader.ReadToNextSibling(PStrings.entityData));

        }
    }

    private static void ReadXmlExtensionDatas(XmlReader reader) {
        int ownerUniqueID;
        string identifier;

        if (reader.ReadToDescendant(PStrings.extensionData) == true) {

            do {

                ownerUniqueID = int.Parse(reader.GetAttribute(PStrings.uniqueID));
                identifier = reader.GetAttribute(PStrings.identifier);

                if (obstacles.ContainsKey(ownerUniqueID) == true) obstacles[ownerUniqueID].GetExtension(identifier).ReadXml(reader);
                if (items.ContainsKey(ownerUniqueID) == true) items[ownerUniqueID].GetExtension(identifier).ReadXml(reader);
                if (entities.ContainsKey(ownerUniqueID) == true) entities[ownerUniqueID].GetExtension(identifier).ReadXml(reader);

            } while (reader.ReadToNextSibling(PStrings.extensionData));

        }
    }

    private static void ReadXmlWaypointDatas(XmlReader reader) {
        int x, z;
        int ownerUniqueID;
        Direction orientation;
        WaypointManagerData manager = null;
        WaypointData waypoint;

        if (reader.ReadToDescendant(PStrings.waypointData) == true) {

            do {

                x = int.Parse(reader.GetAttribute(PStrings.xCoordinate));
                z = int.Parse(reader.GetAttribute(PStrings.zCoordinate));
                orientation = (Direction)int.Parse(reader.GetAttribute(PStrings.orientation));

                ownerUniqueID = int.Parse(reader.GetAttribute(PStrings.uniqueID));

                if (obstacles.ContainsKey(ownerUniqueID) == true) manager = (WaypointManagerData)obstacles[ownerUniqueID].GetExtension(PStrings.waypointManager);
                if (items.ContainsKey(ownerUniqueID) == true) manager = (WaypointManagerData)items[ownerUniqueID].GetExtension(PStrings.waypointManager);
                if (entities.ContainsKey(ownerUniqueID) == true) manager = (WaypointManagerData)entities[ownerUniqueID].GetExtension(PStrings.waypointManager);

                waypoint = WaypointData.Instantiate(Metrics.GetCell(new Coordinates(x, z)), manager);
                waypoint.SetOrientation(orientation);

            } while (reader.ReadToNextSibling(PStrings.waypointData));

        }
    }
}
