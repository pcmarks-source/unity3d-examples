using System.Xml;
using UnityEngine;

public class CeilingData : CellConstructionData {

    public static CeilingData Prototype(string instructionSetID) {
        CeilingData newData = new CeilingData();

        newData.instructionSetID = instructionSetID;

        return newData;
    }

    public static CeilingData Instantiate(CeilingData prototype, CellData cell) {
        if (prototype != null) {
            CeilingData newData = new CeilingData();

            newData.instructionSetID = prototype.instructionSetID;
            newData.cellData = cell;
            cell.ceiling = newData;

            return newData;
        }

        cell.ceiling = null;
        return null;
    }

    public override void TriangulationInstructions(MeshGenerator ceilingMesh, MeshGenerator ceilingMask, MeshGenerator meshC = null, MeshGenerator meshD = null) {
        base.TriangulationInstructions(ceilingMesh, ceilingMask);

        if (cellData.ceiling.instructionSetID == PStrings.ceiling) TriangulateCeiling(ceilingMesh, ceilingMask);
    }

    private void TriangulateCeiling(MeshGenerator ceilingMesh, MeshGenerator ceilingMask) {
        Vector3 groundZero = new Vector3(cellData.coordinates.x, Metrics.wallHeight, cellData.coordinates.z);
        MeshGenerator.TriangulateReverseHorizontalMegaQuad(ceilingMesh, groundZero, cellData.ceiling.materialIndex, cellData.ceiling.colorIndex);
        MeshGenerator.TriangulateHorizontalMegaQuad(ceilingMask, groundZero, cellData.ceiling.materialIndex, cellData.ceiling.colorIndex);
    }

    public override bool Validation(CellData cell) {
        int validation = 0;

        if (instructionSetID == PStrings.ceiling) validation = (ValidateCeiling(cell) == true) ? 1 : -1;

        if (validation > 0) return true;
        if (validation < 0) return false;

        return base.Validation(cell);
    }

    private bool ValidateCeiling(CellData cell) {

        return true;
    }

    public void WriteXml(XmlWriter writer) {
        writer.WriteAttributeString(PStrings.materialIndex, materialIndex.ToString());
        writer.WriteAttributeString(PStrings.colorIndex, colorIndex.ToString());
    }

    public void ReadXml(XmlReader reader) {
        materialIndex = int.Parse(reader.GetAttribute(PStrings.materialIndex));
        colorIndex = int.Parse(reader.GetAttribute(PStrings.colorIndex));
    }
}
