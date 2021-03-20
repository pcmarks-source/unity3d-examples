using UnityEngine;

public class ChunkData {

    private bool enabled = true;

    private MeshGenerator grass;
    private MeshGenerator fog;
    private MeshGenerator liquid;
    private MeshGenerator foundation;
    private MeshGenerator structure;
    private MeshGenerator ceiling;
    private MeshGenerator ceilingMask;

    private CellData[] cells;

    public ChunkData(MeshGenerator grass, MeshGenerator fog, MeshGenerator liquid, MeshGenerator foundation, MeshGenerator structure, MeshGenerator ceiling, MeshGenerator ceilingMask) {
        this.grass = grass;
        this.fog = fog;
        this.liquid = liquid;
        this.foundation = foundation;
        this.structure = structure;
        this.ceiling = ceiling;
        this.ceilingMask = ceilingMask;

        cells = new CellData[Metrics.chunkWidth * Metrics.chunkLength];
    }

    public void FixedInstructionCycle() {
        if (enabled == false) return;
    }

    public void InstructionCycle() {
        if (enabled == false) return;
    }

    public void LateInstructionCycle() {
        if (enabled == false) return;

        TriangulateChunk();
        enabled = false;
    }

    private void TriangulateChunk() {
        grass.ClearMesh();
        fog.ClearMesh();
        liquid.ClearMesh();
        foundation.ClearMesh();
        structure.ClearMesh();
        ceiling.ClearMesh();
        ceilingMask.ClearMesh();

        for (int i = 0; i < cells.Length; i++) {
            TriangulateCell(cells[i]);
        }

        grass.ApplyMesh();
        fog.ApplyMesh();
        liquid.ApplyMesh();
        foundation.ApplyMesh();
        structure.ApplyMesh();
        ceiling.ApplyMesh();
        ceilingMask.ApplyMesh();
    }

    private void TriangulateCell(CellData cell) {
        if (cell.foundation != null) cell.foundation.TriangulationInstructions(foundation, grass, fog, liquid);
        if (cell.structure != null) cell.structure.TriangulationInstructions(structure);
        if (cell.ceiling != null) cell.ceiling.TriangulationInstructions(ceiling, ceilingMask);
    }

    public void AddCell(int index, CellData cell) {
        cells[index] = cell;
    }

    public void Refresh() {
        enabled = true;
    }
}
