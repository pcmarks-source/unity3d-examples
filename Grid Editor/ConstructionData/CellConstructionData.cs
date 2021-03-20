using UnityEngine;

public class CellConstructionData {

    public CellData cellData { get; protected set; }

    public string instructionSetID { get; protected set; }

    protected int MaterialIndex = int.MinValue;
    public int materialIndex {
        get {
            return MaterialIndex;
        }

        set {
            if (MaterialIndex == value) return;

            MaterialIndex = value;
            Refresh();
        }
    }

    protected int ColorIndex = int.MinValue;
    public int colorIndex {
        get {
            return ColorIndex;
        }

        set {
            if (ColorIndex == value) return;

            ColorIndex = value;
            Refresh();
        }
    }

    public CellConstructionData() {

    }

    public virtual void TriangulationInstructions(MeshGenerator meshA, MeshGenerator meshB = null, MeshGenerator meshC = null, MeshGenerator meshD = null) {

    }

    public virtual bool Validation(CellData cell) {
        Debug.LogError("Unhandled Validation Case");
        return false;
    }

    protected void Refresh() {
        if (cellData == null) return;

        cellData.Refresh();
    }
}
