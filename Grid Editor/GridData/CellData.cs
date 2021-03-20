using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using UnityEngine;

public class CellData : IXmlSerializable {

    private ChunkData chunk;
    public Coordinates coordinates { get; private set; }
    private CellData[] neighbors = new CellData[8];

    private int Elevation;
    public int elevation {
        get {
            return Elevation;
        }

        set {
            if (Elevation == value) return;
            Elevation = value;
        }
    }

    private FoundationData Foundation;
    public FoundationData foundation {
        get {
            return Foundation;
        }

        set {
            if (Foundation == value) return;
            Foundation = value;

            Refresh();
        }
    }

    private StructureData Structure;
    public StructureData structure {
        get {
            return Structure;
        }

        set {
            if (Structure == value) return;
            Structure = value;

            Refresh();
        }
    }

    private CeilingData Ceiling;
    public CeilingData ceiling {
        get {
            return Ceiling;
        }

        set {
            if (Ceiling == value) return;
            Ceiling = value;

            Refresh();
        }
    }

    private ObstacleData Obstacle;
    public ObstacleData obstacle {
        get {
            return Obstacle;
        }

        set {
            if (Obstacle == value) return;
            Obstacle = value;
        }
    }

    private ItemData Item;
    public ItemData item {
        get {
            return Item;
        }

        set {
            if (Item == value) return;
            Item = value;
        }
    }

    private EntityData Entity;
    public EntityData entity {
        get {
            return Entity;
        }

        set {
            if (Entity == value) return;
            Entity = value;
        }
    }

    public CellData(ChunkData chunk, Coordinates coordinates) {
        this.chunk = chunk;
        this.coordinates = coordinates;
    }

    public CellData GetNeighbor(Direction direction) {
        return neighbors[(int)direction];
    }

    public void SetNeighbor(Direction direction, CellData cell) {
        neighbors[(int)direction] = cell;
        cell.neighbors[(int)direction.Opposite()] = this;
    }

    public void Refresh() {
        if (chunk == null) return;
        chunk.Refresh();

        for (int i = 0; i < neighbors.Length; i++) {
            CellData neighbor = neighbors[i];
            if (neighbor != null && neighbor.chunk != null) {
                neighbor.chunk.Refresh();
            }
        }
    }

    public CellData() {

    }

    public XmlSchema GetSchema() {
        return null;
    }

    public void WriteXml(XmlWriter writer) {

    }

    public void ReadXml(XmlReader reader) {

    }
}
