using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleData : ExtensibleData, IXmlSerializable {

    public int width { get; private set; }
    public int length { get; private set; }

    public int orientedWidth {
        get {
            return (orientation == Direction.N || orientation == Direction.S) ? width : length;
        }
    }
    public int orientedLength {
        get {
            return (orientation == Direction.N || orientation == Direction.S) ? length : width;
        }
    }

    public List<CellData> cells { get; private set; }

    public static ObstacleData Prototype(string instructionSetID, GameObject prefab, int width, int length, LightMode lightMode, DoorMode doorMode, bool isInteractable, bool useVFX) {
        ObstacleData prototype = new ObstacleData();

        prototype.instructionSetID = instructionSetID;
        prototype.prefab = prefab;
        prototype.width = width;
        prototype.length = length;
        prototype.lightMode = lightMode;
        prototype.doorMode = doorMode;
        prototype.isInteractable = isInteractable;
        prototype.useVFX = useVFX;

        return prototype;
    }

    public static ObstacleData Instantiate(ObstacleData prototype, CellData cell, Direction orientation, int uniqueID) {
        if (prototype != null) {
            ObstacleData instance = new ObstacleData();
            instance.cells = new List<CellData>();

            instance.uniqueID = uniqueID;

            instance.instructionSetID = prototype.instructionSetID;
            instance.prefab = prototype.prefab;
            instance.width = prototype.width;
            instance.length = prototype.length;
            instance.lightMode = prototype.lightMode;
            instance.doorMode = prototype.doorMode;
            instance.isInteractable = prototype.isInteractable;
            instance.useVFX = prototype.useVFX;

            instance.cellData = cell;
            instance.orientation = orientation;

            CellData occupiedCell;

            int zStart = Metrics.zFirstCoordinate(instance.cellData, instance.orientation, instance.width, instance.length);
            int zEnd = Metrics.zLastCoordinate(instance.cellData, instance.orientation, instance.width, instance.length);
            int xStart = Metrics.xFirstCoordinate(instance.cellData, instance.orientation, instance.width, instance.length);
            int xEnd = Metrics.xLastCoordinate(instance.cellData, instance.orientation, instance.width, instance.length);

            for (int z = zStart; z < zEnd; z++) {
                for (int x = xStart; x < xEnd; x++) {
                    occupiedCell = Metrics.GetCell(x, z);
                    occupiedCell.obstacle = instance;
                    instance.cells.Add(occupiedCell);
                }
            }

            instance.gameObject = GameObject.Instantiate(instance.prefab);
            instance.gameObject.name = instance.instructionSetID;
            instance.gameObject.transform.SetParent(Metrics.obstacleContainer.transform, true);
            instance.gameObject.transform.eulerAngles = new Vector3(-90, 0, Metrics.OrientationFromDirection[(int)instance.orientation]);
            instance.gameObject.transform.position = Coordinates.ToWorldSpace(instance.cellData.coordinates);

            instance.extensions = new Dictionary<string, ExtensionData>();

            instance.extensions.Add(PStrings.waypointManager, new WaypointManagerData());
            instance.extensions.Add(PStrings.pathfindManager, new PathfindManagerData());

            if (instance.lightMode != LightMode.Null) instance.extensions.Add(PStrings.light, new LightData());
            if (instance.doorMode != DoorMode.Null) instance.extensions.Add(PStrings.door, new DoorData());
            if (instance.isInteractable == true) instance.extensions.Add(PStrings.interactable, new InteractableData());
            if (instance.useVFX == true) instance.extensions.Add(PStrings.visualEffects, new VisualEffectData());

            GridData.ObstacleInstanciated(instance);

            foreach(ExtensionData extension in instance.extensions.Values) {
                extension.Initialize(uniqueID);
            }

            return instance;
        }

        if(cell.obstacle != null) {
            ObstacleData instance = cell.obstacle;

            for (int i = 0; i < instance.cells.Count; i++) {
                instance.cells[i].obstacle = null;
            }

            instance.cells.Clear();
            GameObject.Destroy(instance.gameObject);

            WaypointManagerData waypointManager = (WaypointManagerData)instance.extensions[PStrings.waypointManager];
            waypointManager.Purge();

            GridData.ObstacleDestroyed(instance);

            return instance;
        }

        return null;
    }

    public bool Validation(CellData cell, Direction desiredOrientation) {
        int validation = 0;

        validation = (ValidateBasic(cell, desiredOrientation) == true) ? 1 : -1;

        if (validation > 0) return true;
        if (validation < 0) return false;

        return false;
    }

    private bool ValidateBasic(CellData cell, Direction desiredOrientation) {
        int zStart = Metrics.zFirstCoordinate(cell, desiredOrientation, width, length);
        int zEnd = Metrics.zLastCoordinate(cell, desiredOrientation, width, length);
        int xStart = Metrics.xFirstCoordinate(cell, desiredOrientation, width, length);
        int xEnd = Metrics.xLastCoordinate(cell, desiredOrientation, width, length);

        for (int z = zStart; z < zEnd; z++) {
            for (int x = xStart; x < xEnd; x++) {

                CellData cellUnderObstacle = Metrics.GetCell(x, z);

                if (cellUnderObstacle == null) return false;
                if (cellUnderObstacle.foundation.isSolid == false) return false;
                if (cellUnderObstacle.structure != null) {
                    if (cellUnderObstacle.structure.instructionSetID != PStrings.doorway) {
                        return false;
                    }
                }
                if (cellUnderObstacle.obstacle != null && cellUnderObstacle.obstacle != this) return false;
                if (cellUnderObstacle.item != null) return false;
                if (cellUnderObstacle.entity != null) return false;

            }
        }

        return true;
    }

    public void SetOrientation(Direction direction) {
        CellData occupiedCell;

        for (int i = 0; i < cells.Count; i++) {
            cells[i].obstacle = null;
        }
        cells.Clear();

        orientation = direction;

        int zStart = Metrics.zFirstCoordinate(cellData, orientation, width, length);
        int zEnd = Metrics.zLastCoordinate(cellData, orientation, width, length);
        int xStart = Metrics.xFirstCoordinate(cellData, orientation, width, length);
        int xEnd = Metrics.xLastCoordinate(cellData, orientation, width, length);

        for (int z = zStart; z < zEnd; z++) {
            for (int x = xStart; x < xEnd; x++) {
                occupiedCell = Metrics.GetCell(x, z);
                occupiedCell.obstacle = this;
                cells.Add(occupiedCell);
            }
        }

        gameObject.transform.eulerAngles = new Vector3(-90, 0, Metrics.OrientationFromDirection[(int)orientation]);
        gameObject.transform.position = Metrics.Perturb(new Vector3(cellData.coordinates.x, 0, cellData.coordinates.z));
    }

    public void FixedInstructionCycle() {
        foreach(ExtensionData extension in extensions.Values) {
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

    public ObstacleData() {

    }

    public XmlSchema GetSchema() {
        return null;
    }

    public void WriteXml(XmlWriter writer) {

    }

    public void ReadXml(XmlReader reader) {

    }
}
