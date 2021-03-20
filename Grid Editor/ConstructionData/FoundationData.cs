using System.Xml;
using UnityEngine;

public class FoundationData : CellConstructionData {

    protected int EffectMaterialIndex = (int)MatI.Hexes;
    public int effectMaterialIndex {
        get {
            return EffectMaterialIndex;
        }

        set {
            if (EffectMaterialIndex == value) return;

            EffectMaterialIndex = value;
            Refresh();
        }
    }

    protected int EffectColorIndex = (int)ColI.Blue;
    public int effectColorIndex {
        get {
            return EffectColorIndex;
        }

        set {
            if (EffectColorIndex == value) return;

            EffectColorIndex = value;
            Refresh();
        }
    }

    public bool isSolid {
        get {
            int hasGround = 0;
            if (cellData.foundation.instructionSetID == PStrings.ground) hasGround++;
            if (cellData.foundation.instructionSetID == PStrings.groundGrass) hasGround++;
            return (hasGround > 0);
        }
    }

    public static bool ValidateHasEffect(CellData cell) {
        int validation = 0;

        if (cell == null) return false;
        if (cell.foundation == null) return false;

        if (cell.foundation.instructionSetID == PStrings.ground || cell.foundation.instructionSetID == PStrings.groundGrass) validation = (ValidateHasGrass(cell) == true) ? 1 : -1;
        if (cell.foundation.instructionSetID == PStrings.pit || cell.foundation.instructionSetID == PStrings.pitGrate) validation = (ValidateHasFog(cell) == true) ? 1 : -1;
        if (cell.foundation.instructionSetID == PStrings.pool || cell.foundation.instructionSetID == PStrings.poolGrate) validation = (ValidateHasLiquid(cell) == true) ? 1 : -1;

        if (validation > 0) return true;
        if (validation < 0) return false;

        Debug.LogError("Effect Validation Exception!");
        return false;
    }

    public static bool ValidateHasGrass(CellData cell) {
        if (cell == null) return false;
        if (cell.foundation == null) return false;
        if (cell.structure != null) return false;

        if (cell.foundation.instructionSetID != PStrings.groundGrass) return false;

        return true;
    }

    public static bool ValidateHasFog(CellData cell) {
        if (cell == null) return false;
        if (cell.foundation == null) return false;

        int hasFog = 0;
        if (cell.foundation.instructionSetID == PStrings.pit) hasFog++;
        if (cell.foundation.instructionSetID == PStrings.pitGrate) hasFog++;
        if (hasFog <= 0) return false;

        return true;
    }

    public static bool ValidateHasLiquid(CellData cell) {
        if (cell == null) return false;
        if (cell.foundation == null) return false;

        int hasLiquid = 0;
        if (cell.foundation.instructionSetID == PStrings.pool) hasLiquid++;
        if (cell.foundation.instructionSetID == PStrings.poolGrate) hasLiquid++;
        if (hasLiquid <= 0) return false;

        return true;
    }

    public static FoundationData Prototype(string instructionSetID) {
        FoundationData newData = new FoundationData();

        newData.instructionSetID = instructionSetID;

        return newData;
    }

    public static FoundationData Instantiate(FoundationData prototype, CellData cell) {
        if (prototype != null) {
            FoundationData newData = new FoundationData();

            newData.instructionSetID = prototype.instructionSetID;
            newData.cellData = cell;
            cell.foundation = newData;

            return newData;
        }

        cell.foundation = null;
        return null;
    }

    public override void TriangulationInstructions(MeshGenerator foundationMesh, MeshGenerator grassMesh, MeshGenerator fogMesh, MeshGenerator liquidMesh) {
        base.TriangulationInstructions(foundationMesh);

        if (instructionSetID == PStrings.pit) TriangulatePit(foundationMesh, fogMesh);
        if (instructionSetID == PStrings.pitGrate) TriangulatePitGrate(foundationMesh, fogMesh);

        if (instructionSetID == PStrings.pool) TriangulatePool(foundationMesh, liquidMesh);
        if (instructionSetID == PStrings.poolGrate) TriangulatePoolGrate(foundationMesh, liquidMesh);

        if (instructionSetID == PStrings.ground) TriangulateGround(foundationMesh, grassMesh);
        if (instructionSetID == PStrings.groundGrass) TriangulateGroundGrass(foundationMesh, grassMesh);
    }

    private void TriangulatePit(MeshGenerator foundationMesh, MeshGenerator effectMesh) {
        Vector3 groundZero = new Vector3(cellData.coordinates.x, -Metrics.pitLevel, cellData.coordinates.z);
        MeshGenerator.TriangulateHorizontalMegaQuad(foundationMesh, groundZero, cellData.foundation.materialIndex, cellData.foundation.colorIndex);
        MeshGenerator.TriangulateHorizontalQuad(effectMesh, new Vector3(cellData.coordinates.x, -Metrics.trimHeight, cellData.coordinates.z), effectColorIndex);

        CellData neighbor = null;
        int cornerIndexA = -1;
        int cornerIndexB = 2;
        int height = Metrics.pitLevel - Metrics.trimHeight;
        int offset = -(height + Metrics.trimHeight);

        for (int i = 0; i < 4; i++) {

            cornerIndexA++;
            cornerIndexB++; if (cornerIndexB == 4) cornerIndexB = 0;

            neighbor = cellData.GetNeighbor(Metrics.CardinalDirections[i]);

            if (neighbor == null) continue;

            if (neighbor.foundation.instructionSetID == PStrings.ground) {
                MeshGenerator.TriangulateReverseVerticalMegaQuad(foundationMesh, groundZero, height, offset, cellData.foundation.materialIndex, cellData.foundation.colorIndex, cornerIndexA, cornerIndexB);
            }
        }
    }

    private void TriangulatePitGrate(MeshGenerator foundationMesh, MeshGenerator effectMesh) {
        TriangulatePit(foundationMesh, effectMesh);

        CellData neighbor;
        Vector3 groundZero = new Vector3(cellData.coordinates.x, Metrics.foundationLevel, cellData.coordinates.z);

        int cornerIndexA = -1;
        int cornerIndexB = 2;
        float height = Metrics.trimHeight * 0.5f;
        float offset = -height;

        for (int i = 0; i < 4; i++) {
            cornerIndexA++;
            cornerIndexB++; if (cornerIndexB == 4) cornerIndexB = 0;

            MeshGenerator.TriangulateGrateMegaQuad(foundationMesh, groundZero, cellData.foundation.materialIndex, cellData.foundation.colorIndex, cornerIndexA, cornerIndexB);

            neighbor = cellData.GetNeighbor(Metrics.CardinalDirections[i]);

            if (neighbor == null) {
                continue;
            }
            if (neighbor.foundation == null) {
                continue;
            }

            if (neighbor.foundation.instructionSetID == PStrings.pit || neighbor.foundation.instructionSetID == PStrings.pool) {
                MeshGenerator.TriangulateVerticalMegaQuad(foundationMesh, groundZero, height, offset, cellData.foundation.materialIndex, cellData.foundation.colorIndex, cornerIndexA, cornerIndexB);
            }
        }
    }

    private void TriangulatePool(MeshGenerator foundationMesh, MeshGenerator effectMesh) {
        Vector3 groundZero = new Vector3(cellData.coordinates.x, -Metrics.trimHeight, cellData.coordinates.z);
        MeshGenerator.TriangulateHorizontalMegaQuad(foundationMesh, groundZero, cellData.foundation.materialIndex, cellData.foundation.colorIndex);
        MeshGenerator.TriangulateHorizontalQuadFlat(effectMesh, new Vector3(cellData.coordinates.x, Metrics.liquidLevel - 0.25f, cellData.coordinates.z), effectColorIndex);
    }

    private void TriangulatePoolGrate(MeshGenerator foundationMesh, MeshGenerator effectMesh) {
        TriangulatePool(foundationMesh, effectMesh);

        CellData neighbor;
        Vector3 groundZero = new Vector3(cellData.coordinates.x, Metrics.foundationLevel, cellData.coordinates.z);

        int cornerIndexA = -1;
        int cornerIndexB = 2;
        float height = Metrics.trimHeight * 0.5f;
        float offset = -height;

        for (int i = 0; i < 4; i++) {
            cornerIndexA++;
            cornerIndexB++; if (cornerIndexB == 4) cornerIndexB = 0;

            MeshGenerator.TriangulateGrateMegaQuad(foundationMesh, groundZero, cellData.foundation.materialIndex, cellData.foundation.colorIndex, cornerIndexA, cornerIndexB);

            neighbor = cellData.GetNeighbor(Metrics.CardinalDirections[i]);

            if (neighbor == null) {
                continue;
            }
            if (neighbor.foundation == null) {
                continue;
            }

            if (neighbor.foundation.instructionSetID == PStrings.pit || neighbor.foundation.instructionSetID == PStrings.pool) {
                MeshGenerator.TriangulateVerticalMegaQuad(foundationMesh, groundZero, height, offset, cellData.foundation.materialIndex, cellData.foundation.colorIndex, cornerIndexA, cornerIndexB);
            }
        }
    }

    private void TriangulateGround(MeshGenerator foundationMesh, MeshGenerator effectMesh) {
        Vector3 groundZero = new Vector3(cellData.coordinates.x, Metrics.foundationLevel, cellData.coordinates.z);
        MeshGenerator.TriangulateHorizontalMegaQuad(foundationMesh, groundZero, cellData.foundation.materialIndex, cellData.foundation.colorIndex);

        CellData neighbor = null;
        int cornerIndexA = -1;
        int cornerIndexB = 2;
        int height = Metrics.trimHeight;
        int offset = -Metrics.trimHeight;

        for (int i = 0; i < 4; i++) {
            cornerIndexA++;
            cornerIndexB++; if (cornerIndexB == 4) cornerIndexB = 0;

            neighbor = cellData.GetNeighbor(Metrics.CardinalDirections[i]);

            if (neighbor == null) continue;
            if (neighbor.foundation == null) continue;

            if (neighbor.foundation.instructionSetID == PStrings.pit || 
                neighbor.foundation.instructionSetID == PStrings.pitGrate ||
                neighbor.foundation.instructionSetID == PStrings.pool || 
                neighbor.foundation.instructionSetID == PStrings.poolGrate) {

                MeshGenerator.TriangulateVerticalMegaQuad(foundationMesh, groundZero, height, offset, cellData.foundation.materialIndex, cellData.foundation.colorIndex, cornerIndexA, cornerIndexB);

            }
        }
    }

    private void TriangulateGroundGrass(MeshGenerator foundationMesh, MeshGenerator effectMesh) {
        TriangulateGround(foundationMesh, effectMesh);
        MeshGenerator.TriangulateGrassPoints(effectMesh, new Vector3(cellData.coordinates.x, Metrics.foundationLevel, cellData.coordinates.z), effectColorIndex);
    }

    public override bool Validation(CellData cell) {
        int validation = 0;

        if (instructionSetID == PStrings.pit) validation = (ValidatePit(cell) == true) ? 1 : -1;
        if (instructionSetID == PStrings.pitGrate) validation = (ValidatePitGrate(cell) == true) ? 1 : -1;

        if (instructionSetID == PStrings.pool) validation = (ValidatePool(cell) == true) ? 1 : -1;
        if (instructionSetID == PStrings.poolGrate) validation = (ValidatePoolGrate(cell) == true) ? 1 : -1;

        if (instructionSetID == PStrings.ground) validation = (ValidateGround(cell) == true) ? 1 : -1;
        if (instructionSetID == PStrings.groundGrass) validation = (ValidateGroundGrass(cell) == true) ? 1 : -1;

        if (validation > 0) return true;
        if (validation < 0) return false;

        return base.Validation(cell);
    }

    private bool ValidatePit(CellData cell) {

        return true;
    }

    private bool ValidatePitGrate(CellData cell) {

        return true;
    }

    private bool ValidatePool(CellData cell) {

        return true;
    }

    private bool ValidatePoolGrate(CellData cell) {

        return true;
    }

    private bool ValidateGround(CellData cell) {

        return true;
    }

    private bool ValidateGroundGrass(CellData cell) {

        return true;
    }

    public FoundationData() {

    }

    public void WriteXml(XmlWriter writer) {
        writer.WriteAttributeString(PStrings.materialIndex, materialIndex.ToString());
        writer.WriteAttributeString(PStrings.colorIndex, colorIndex.ToString());
        writer.WriteAttributeString("SpecialColorIndex", effectColorIndex.ToString());
    }

    public void ReadXml(XmlReader reader) {
        materialIndex = int.Parse(reader.GetAttribute(PStrings.materialIndex));
        colorIndex = int.Parse(reader.GetAttribute(PStrings.colorIndex));
        effectColorIndex = int.Parse(reader.GetAttribute("SpecialColorIndex"));
    }
}
