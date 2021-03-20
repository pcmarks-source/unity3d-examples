using System.Xml;
using UnityEngine;

public class StructureData : CellConstructionData {

    public static StructureData Prototype(string instructionSetID) {
        StructureData newData = new StructureData();

        newData.instructionSetID = instructionSetID;

        return newData;
    }

    public static StructureData Instantiate(StructureData prototype, CellData cell) {
        if (prototype != null) {
            StructureData newData = new StructureData();

            newData.instructionSetID = prototype.instructionSetID;
            newData.cellData = cell;
            cell.structure = newData;

            return newData;
        }

        cell.structure = null;
        return null;
    }

    public override void TriangulationInstructions(MeshGenerator meshA, MeshGenerator meshB = null, MeshGenerator meshC = null, MeshGenerator meshD = null) {
        base.TriangulationInstructions(meshA);

        if (instructionSetID == PStrings.wall) TriangulateWall(meshA);
        if (instructionSetID == PStrings.window) TriangulateWindow(meshA);
        if (instructionSetID == PStrings.doorway) TriangulateDoorway(meshA);
        if (instructionSetID == PStrings.cage) TriangulateCage(meshA);
    }

    private void TriangulateWall(MeshGenerator mesh) {
        CellData neighbor = null;
        int cornerIndexA = -1;
        int cornerIndexB = 2;
        int height = 0;
        int offset = 0;
        int walledNeighborCount = 0;

        Vector3 groundZero = new Vector3(cellData.coordinates.x, Metrics.foundationLevel, cellData.coordinates.z);

        for (int i = 0; i < 4; i++) {

            cornerIndexA++;
            cornerIndexB++; if (cornerIndexB == 4) cornerIndexB = 0;

            neighbor = cellData.GetNeighbor(Metrics.CardinalDirections[i]);

            if (neighbor == null) {
                walledNeighborCount++;
                continue;
            }
            if (neighbor.structure != null && neighbor.structure.instructionSetID == PStrings.wall) {
                walledNeighborCount++;
                continue;
            }

            if (neighbor.structure == null || neighbor.structure.instructionSetID == PStrings.cage) {
                height = Metrics.wallHeight;
                offset = 0;
            } else if (neighbor.structure.instructionSetID == PStrings.window) {
                height = Metrics.wallHeight - (Metrics.trimHeight * 2);
                offset = Metrics.trimHeight;
            } else if (neighbor.structure.instructionSetID == PStrings.doorway) {
                height = Metrics.wallHeight - Metrics.trimHeight;
                offset = 0;
            }

            MeshGenerator.TriangulateVerticalMegaQuad(mesh, groundZero, height, offset, cellData.structure.materialIndex, cellData.structure.colorIndex, cornerIndexA, cornerIndexB);
        }

        if(walledNeighborCount == 4) {
            for (int i = 0; i < 4; i++) {

                neighbor = cellData.GetNeighbor(Metrics.CattyDirections[i]);

                if (neighbor == null) {
                    walledNeighborCount++;
                    continue;
                }

                if (neighbor.structure != null && neighbor.structure.instructionSetID == PStrings.wall) {
                    walledNeighborCount++;
                    continue;
                }

            }
        }

        int colorIndex = (walledNeighborCount == 8) ? 10 : cellData.structure.colorIndex;
        MeshGenerator.TriangulateHorizontalMegaQuad(mesh, groundZero + (Vector3.up * Metrics.wallHeight), cellData.structure.materialIndex, colorIndex);
    }

    private void TriangulateWindow(MeshGenerator mesh) {
        CellData neighbor = null;
        int cornerIndexA = -1;
        int cornerIndexB = 2;
        int height = Metrics.trimHeight;
        int offset = Metrics.wallHeight - Metrics.trimHeight;

        Vector3 groundZero = new Vector3(cellData.coordinates.x, Metrics.foundationLevel, cellData.coordinates.z);
        MeshGenerator.TriangulateHorizontalMegaQuad(mesh, groundZero + (Vector3.up * Metrics.wallHeight), cellData.structure.materialIndex, cellData.structure.colorIndex);
        MeshGenerator.TriangulateReverseHorizontalMegaQuad(mesh, groundZero + ((Vector3.up * Metrics.wallHeight) - (Vector3.up * Metrics.trimHeight)), cellData.structure.materialIndex, cellData.structure.colorIndex);
        MeshGenerator.TriangulateHorizontalMegaQuad(mesh, groundZero + (Vector3.up * Metrics.trimHeight), cellData.structure.materialIndex, cellData.structure.colorIndex);

        for (int i = 0; i < 4; i++) {

            cornerIndexA++;
            cornerIndexB++; if (cornerIndexB == 4) cornerIndexB = 0;

            neighbor = cellData.GetNeighbor(Metrics.CardinalDirections[i]);

            if (neighbor == null) continue;
            if (neighbor.structure != null && neighbor.structure.instructionSetID == PStrings.wall) continue;

            if (neighbor.structure == null) {
                MeshGenerator.TriangulateVerticalMegaQuad(mesh, groundZero, height, 0, cellData.structure.materialIndex, cellData.structure.colorIndex, cornerIndexA, cornerIndexB);
                MeshGenerator.TriangulateVerticalMegaQuad(mesh, groundZero, height, offset, cellData.structure.materialIndex, cellData.structure.colorIndex, cornerIndexA, cornerIndexB);
            } else if (neighbor.structure.instructionSetID == PStrings.doorway) {
                MeshGenerator.TriangulateVerticalMegaQuad(mesh, groundZero, height, 0, cellData.structure.materialIndex, cellData.structure.colorIndex, cornerIndexA, cornerIndexB);
            }
        }
    }

    private void TriangulateDoorway(MeshGenerator mesh) {
        CellData neighbor = null;
        int cornerIndexA = -1;
        int cornerIndexB = 2;
        int height = Metrics.trimHeight;
        int offset = Metrics.wallHeight - Metrics.trimHeight;

        Vector3 groundZero = new Vector3(cellData.coordinates.x, Metrics.foundationLevel, cellData.coordinates.z);
        MeshGenerator.TriangulateHorizontalMegaQuad(mesh, groundZero + (Vector3.up * Metrics.wallHeight), cellData.structure.materialIndex, cellData.structure.colorIndex);
        MeshGenerator.TriangulateReverseHorizontalMegaQuad(mesh, groundZero + ((Vector3.up * Metrics.wallHeight) - (Vector3.up * Metrics.trimHeight)), cellData.structure.materialIndex, cellData.structure.colorIndex);

        for (int i = 0; i < 4; i++) {

            cornerIndexA++;
            cornerIndexB++; if (cornerIndexB == 4) cornerIndexB = 0;

            neighbor = cellData.GetNeighbor(Metrics.CardinalDirections[i]);

            if (neighbor == null) continue;
            if (neighbor.structure != null && neighbor.structure.instructionSetID == PStrings.wall) continue;

            if (neighbor.structure != null) continue;

            MeshGenerator.TriangulateVerticalMegaQuad(mesh, groundZero, height, offset, cellData.structure.materialIndex, cellData.structure.colorIndex, cornerIndexA, cornerIndexB);
        }
    }

    private void TriangulateCage(MeshGenerator mesh) {
        CellData neighbor = null;
        int cornerIndexA = -1;
        int cornerIndexB = 2;
        float height = Metrics.trimHeight * 0.25f;
        float offset = Metrics.wallHeight - height;

        Vector3 groundZero = new Vector3(cellData.coordinates.x, Metrics.foundationLevel, cellData.coordinates.z);
        MeshGenerator.TriangulateHorizontalMegaQuad(mesh, groundZero + (Vector3.up * Metrics.wallHeight), cellData.structure.materialIndex, cellData.structure.colorIndex);
        MeshGenerator.TriangulateReverseHorizontalMegaQuad(mesh, groundZero + ((Vector3.up * Metrics.wallHeight) - (Vector3.up * height)), cellData.structure.materialIndex, cellData.structure.colorIndex);
        MeshGenerator.TriangulateHorizontalMegaQuad(mesh, groundZero + (Vector3.up * height), cellData.structure.materialIndex, cellData.structure.colorIndex);

        for (int i = 0; i < 4; i++) {

            cornerIndexA++;
            cornerIndexB++; if (cornerIndexB == 4) cornerIndexB = 0;

            neighbor = cellData.GetNeighbor(Metrics.CardinalDirections[i]);

            if (neighbor == null) continue;
            if (neighbor.structure != null && neighbor.structure.instructionSetID == PStrings.wall) continue;

            if (neighbor.structure == null) {
                MeshGenerator.TriangulateVerticalMegaQuad(mesh, groundZero, height, 0, cellData.structure.materialIndex, cellData.structure.colorIndex, cornerIndexA, cornerIndexB);
                MeshGenerator.TriangulateVerticalMegaQuad(mesh, groundZero, height, offset, cellData.structure.materialIndex, cellData.structure.colorIndex, cornerIndexA, cornerIndexB);
            } else if (neighbor.structure.instructionSetID == PStrings.doorway) {
                MeshGenerator.TriangulateVerticalMegaQuad(mesh, groundZero, height, 0, cellData.structure.materialIndex, cellData.structure.colorIndex, cornerIndexA, cornerIndexB);
            }

            MeshGenerator.TriangulateBarMegaQuad(mesh, groundZero, offset, height, cornerIndexA, cornerIndexB);
        }
    }

    public override bool Validation(CellData cell) {
        int validation = 0;

        if (instructionSetID == PStrings.wall) validation = (ValidateWall(cell) == true) ? 1 : -1;
        if (instructionSetID == PStrings.window) validation = (ValidateWindow(cell) == true) ? 1 : -1;
        if (instructionSetID == PStrings.doorway) validation = (ValidateDoorway(cell) == true) ? 1 : -1;
        if (instructionSetID == PStrings.cage) validation = (ValidateCage(cell) == true) ? 1 : -1;

        if (validation > 0) return true;
        if (validation < 0) return false;

        return base.Validation(cell);
    }

    private bool ValidateWall(CellData cell) {
        if (cell.foundation.instructionSetID != PStrings.ground) return false;
        return true;
    }

    private bool ValidateWindow(CellData cell) {
        if (cell.foundation.instructionSetID != PStrings.ground) return false;

        int supportStructureCount = 0;
        for (int i = 0; i < 4; i++) {
            if (cell.GetNeighbor(Metrics.CardinalDirections[i]).structure != null) supportStructureCount++;
        }
        if (supportStructureCount > 0) return true;

        return true;
    }

    private bool ValidateDoorway(CellData cell) {
        if (cell.foundation.instructionSetID != PStrings.ground) return false;

        int supportStructureCount = 0;
        for (int i = 0; i < 4; i++) {
            if (cell.GetNeighbor(Metrics.CardinalDirections[i]).structure != null) supportStructureCount++;
        }
        if (supportStructureCount > 0) return true;

        return true;
    }

    private bool ValidateCage(CellData cell) {
        if (cell.foundation.instructionSetID != PStrings.ground) return false;
        return true;
    }

    public StructureData() {

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
