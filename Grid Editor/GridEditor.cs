using UnityEngine;
using UnityEngine.EventSystems;

public class GridEditor : MonoBehaviour {

    public GridSelectionUI selectionUI;
    private GridEditorUI editorUI;

    private Vector3 dragStartPosition;
    private Vector3 dragEndPosition;

    private CellData dragStartCell;
    private CellData dragEndCell;

    private bool editMesh = true;
    private bool editMaterial = true;
    private bool editColor = true;
    private bool editElevation = true;

    private InputMode inputMode;
    public EditMode editMode { get; private set; }
    public CreationMode creationMode { get; private set; }
    public SelectionMode selectionMode { get; private set; }
    private EffectMode effectMode;

    private int activeMegaMaterial;
    private int activeColor;
    private int activeElevation;
    private Direction activeDirection = Direction.S;
    private FoundationData activeFoundation;
    private StructureData activeStructure;
    private CeilingData activeCeiling;
    private ObstacleData activeObstacle;
    private ItemData activeItem;
    private EntityData activeEntity;

    private ObstacleData selectedObstacle;
    private ItemData selectedItem;
    private EntityData selectedEntity;
    private WaypointData selectedWaypoint;

    private int defaultMegaMaterial;
    private int defaultMegaColor;
    private int defaultElevation;
    private FoundationData defaultFoundation;

    public void Initialize() {
        dragStartPosition = Metrics.nullVector3;
        dragStartCell = null;

        dragEndPosition = Metrics.nullVector3;
        dragEndCell = null;

        defaultMegaMaterial = (int)MatI.Boards;
        defaultMegaColor = (int)ColI.Brown;
        defaultElevation = 1;
        defaultFoundation = GridData.GetFoundationPrototype(PStrings.ground);

        editorUI = GetComponent<GridEditorUI>();
        ToggleGrid(false);

        SetActiveDirection((int)Direction.S);
        SetActiveEffect((int)EffectMode.Grass);
        SetActiveColor((int)ColI.Grey);
        SetActiveMegaMaterial((int)MatI.Bricks);
        SetActiveElevation(defaultElevation);
        SetActiveStructure(PStrings.wall);
    }

    public void FixedInstructionCycle() {
        if (EventSystem.current.IsPointerOverGameObject() == true) return;
    }

    public void InstructionCycle() {
        if (EventSystem.current.IsPointerOverGameObject() == true) return;
        HandleInput();
    }

    public void LateInstructionCycle() {
        if (EventSystem.current.IsPointerOverGameObject() == true) return;
    }

    private void HandleInput() {
        Vector3 zeroPoint = Metrics.ZeroPointUnderMouse(Camera.main);

        if (Input.GetButtonDown(IStrings.LMB)) {
            inputMode = InputMode.Constructive;
            dragStartPosition = zeroPoint;
        }

        if (Input.GetButtonDown(IStrings.RMB)) {
            inputMode = InputMode.Destructive;
            dragStartPosition = zeroPoint;
        }

        UpdateSelectionUI(zeroPoint);

        if (Input.GetButtonUp(IStrings.LMB)) {
            inputMode = InputMode.Constructive;
            EndSelectionDrag();
        }

        if (Input.GetButtonUp(IStrings.RMB)) {
            inputMode = InputMode.Destructive;
            EndSelectionDrag();
        }
    }

    private void UpdateSelectionUI(Vector3 currentMousePosition) {
        CellData selectionStartCell = Metrics.GetCell(Metrics.ClampToGrid((dragStartPosition == Metrics.nullVector3) ? currentMousePosition : dragStartPosition));
        CellData selectionEndCell = Metrics.GetCell(Metrics.ClampToGrid(currentMousePosition));

        if (dragStartPosition != Metrics.nullVector3) dragStartCell = selectionStartCell;
        dragEndCell = selectionEndCell;

        Vector2 pivot = new Vector2(0, 0);

        int width = 1 + Mathf.Abs(selectionStartCell.coordinates.x - selectionEndCell.coordinates.x);
        int length = 1 + Mathf.Abs(selectionStartCell.coordinates.z - selectionEndCell.coordinates.z);

        int pivotX = (selectionStartCell.coordinates.x <= selectionEndCell.coordinates.x) ? 0 : 1;
        int pivotZ = (selectionStartCell.coordinates.z <= selectionEndCell.coordinates.z) ? 0 : 1;

        Coordinates coordinates = new Coordinates(selectionStartCell.coordinates.x + pivotX, selectionStartCell.coordinates.z + pivotZ);
        pivot = new Vector2(pivotX, pivotZ);

        selectionUI.SetPivot(pivot);
        selectionUI.SetSelection(width, length, coordinates);
    }

    private void EndSelectionDrag() {
        int startX = (dragStartCell.coordinates.x < dragEndCell.coordinates.x) ? dragStartCell.coordinates.x : dragEndCell.coordinates.x;
        int startZ = (dragStartCell.coordinates.z < dragEndCell.coordinates.z) ? dragStartCell.coordinates.z : dragEndCell.coordinates.z;
        int endX = (dragStartCell.coordinates.x > dragEndCell.coordinates.x) ? dragStartCell.coordinates.x : dragEndCell.coordinates.x;
        int endZ = (dragStartCell.coordinates.z > dragEndCell.coordinates.z) ? dragStartCell.coordinates.z : dragEndCell.coordinates.z;

        CellData cell = null;

        for (int x = startX; x <= endX; x++) {
            for (int z = startZ; z <= endZ; z++) {

                cell = Metrics.GetCell(x, z);
                if (cell == null) continue;

                if (editMode == EditMode.Creation) CreateAt(cell);
                else if (editMode == EditMode.Selection) SelectAt(cell);
            }
        }

        dragStartPosition = Metrics.nullVector3;
        dragStartCell = null;
    }

    private void CreateAt(CellData cell) {
        if (creationMode == CreationMode.Null) {
            //////////////////////////////////////////////////////////////////////////////////////
            /// This should never happen but if it does nothing should be created or destroyed ///
            //////////////////////////////////////////////////////////////////////////////////////
        } else if (creationMode == CreationMode.Foundation) {

            if (inputMode == InputMode.Null) {
                //////////////////////////////////////////////////////////////////////////////////////
                /// This should never happen but if it does nothing should be created or destroyed ///
                //////////////////////////////////////////////////////////////////////////////////////
            } else if (inputMode == InputMode.Constructive) {

                if (activeFoundation.Validation(cell) == false) return;

                if (editMesh == true) GridData.InstantiateFoundation(activeFoundation, cell);
                if (editMaterial == true) cell.foundation.materialIndex = activeMegaMaterial;
                if (editColor == true) cell.foundation.colorIndex = activeColor;
                if (editElevation == true) cell.elevation = activeElevation;

                string activeID = cell.foundation.instructionSetID;

                if (activeID == PStrings.pit || activeID == PStrings.pitGrate) {
                    cell.foundation.effectColorIndex = (int)ColI.Red;
                } else if (activeID == PStrings.pool || activeID == PStrings.poolGrate) {
                    cell.foundation.effectColorIndex = (int)ColI.Blue;
                } else if (activeID == PStrings.groundGrass) {
                    cell.foundation.effectColorIndex = (int)ColI.Green;
                }

            } else if (inputMode == InputMode.Destructive) {

                if (editMesh == true) GridData.InstantiateFoundation(defaultFoundation, cell);
                if (editMaterial == true) cell.foundation.materialIndex = defaultMegaMaterial;
                if (editColor == true) cell.foundation.colorIndex = defaultMegaColor;
                if (editElevation == true) cell.elevation = defaultElevation;

            }

        } else if (creationMode == CreationMode.Structure) {

            if (inputMode == InputMode.Null) {
                //////////////////////////////////////////////////////////////////////////////////////
                /// This should never happen but if it does nothing should be created or destroyed ///
                //////////////////////////////////////////////////////////////////////////////////////
            } else if (inputMode == InputMode.Constructive) {

                if (activeStructure.Validation(cell) == false) return;

                if (editMesh == true) GridData.InstantiateStructure(activeStructure, cell);
                if (editMaterial == true) cell.structure.materialIndex = activeMegaMaterial;
                if (editColor == true) cell.structure.colorIndex = activeColor;
                if (editElevation == true) cell.elevation = activeElevation;

            } else if (inputMode == InputMode.Destructive) {

                if (editMesh == true) GridData.InstantiateStructure(null, cell);
                if (editElevation == true) cell.elevation = defaultElevation;

                if (cell.structure == null) return;

                if (editMaterial == true) cell.structure.materialIndex = defaultMegaMaterial;
                if (editColor == true) cell.structure.colorIndex = defaultMegaColor;

            }

        } else if (creationMode == CreationMode.Ceiling) {

            if (inputMode == InputMode.Null) {
                //////////////////////////////////////////////////////////////////////////////////////
                /// This should never happen but if it does nothing should be created or destroyed ///
                //////////////////////////////////////////////////////////////////////////////////////
            } else if (inputMode == InputMode.Constructive) {

                if (activeCeiling.Validation(cell) == false) return;

                if (editMesh == true) GridData.InstantiateCeiling(activeCeiling, cell);
                if (editMaterial == true) cell.ceiling.materialIndex = activeMegaMaterial;
                if (editColor == true) cell.ceiling.colorIndex = activeColor;
                if (editElevation == true) cell.elevation = activeElevation;

            } else if (inputMode == InputMode.Destructive) {

                if (editMesh == true) GridData.InstantiateCeiling(null, cell);
                if (editElevation == true) cell.elevation = defaultElevation;

                if (cell.ceiling == null) return;

                if (editMaterial == true) cell.ceiling.materialIndex = defaultMegaMaterial;
                if (editColor == true) cell.ceiling.colorIndex = defaultMegaColor;

            }

        } else if (creationMode == CreationMode.Obstacle) {

            if (inputMode == InputMode.Null) {
                //////////////////////////////////////////////////////////////////////////////////////
                /// This should never happen but if it does nothing should be created or destroyed ///
                //////////////////////////////////////////////////////////////////////////////////////
            } else if (inputMode == InputMode.Constructive) {

                if (activeObstacle.Validation(cell, activeDirection) == false) return;

                Metrics.uniqueID++;
                ObstacleData.Instantiate(activeObstacle, cell, activeDirection, Metrics.uniqueID);

            } else if (inputMode == InputMode.Destructive) {

                ObstacleData.Instantiate(null, cell, activeDirection, -1);

            }

        } else if (creationMode == CreationMode.Item) {

            if (inputMode == InputMode.Null) {
                //////////////////////////////////////////////////////////////////////////////////////
                /// This should never happen but if it does nothing should be created or destroyed ///
                //////////////////////////////////////////////////////////////////////////////////////
            } else if (inputMode == InputMode.Constructive) {

                if (activeItem.Validation(cell) == false) return;

                Metrics.uniqueID++;
                ItemData.Instantiate(activeItem, cell, Metrics.uniqueID);

            } else if (inputMode == InputMode.Destructive) {

                ItemData.Instantiate(null, cell, -1);

            }

        } else if (creationMode == CreationMode.Entity) {

            if (inputMode == InputMode.Null) {
                //////////////////////////////////////////////////////////////////////////////////////
                /// This should never happen but if it does nothing should be created or destroyed ///
                //////////////////////////////////////////////////////////////////////////////////////
            } else if (inputMode == InputMode.Constructive) {

                if (activeEntity.Validation(cell) == false) return;

                Metrics.uniqueID++;
                EntityData.Instantiate(activeEntity, cell, activeDirection, Metrics.uniqueID);

            } else if (inputMode == InputMode.Destructive) {

                EntityData.Instantiate(null, cell, activeDirection, -1);

            }

        }
    }

    private void SelectAt(CellData cell) {
        if (selectionMode == SelectionMode.Null) {
            //////////////////////////////////////////////////////////////////////////////////////
            /// This should never happen but if it does nothing should be created or destroyed ///
            //////////////////////////////////////////////////////////////////////////////////////
        } else if (selectionMode == SelectionMode.Obstacle) {

            if (inputMode == InputMode.Null) {
                //////////////////////////////////////////////////////////////////////////////////////
                /// This should never happen but if it does nothing should be created or destroyed ///
                //////////////////////////////////////////////////////////////////////////////////////
            } else if (inputMode == InputMode.Constructive) {

                SetSelectedObstacle(cell.obstacle);

            } else if (inputMode == InputMode.Destructive) {

                SetSelectedObstacle(null);

            }

        } else if (selectionMode == SelectionMode.Item) {

            if (inputMode == InputMode.Null) {
                //////////////////////////////////////////////////////////////////////////////////////
                /// This should never happen but if it does nothing should be created or destroyed ///
                //////////////////////////////////////////////////////////////////////////////////////
            } else if (inputMode == InputMode.Constructive) {

                SetSelectedItem(cell.item);

            } else if (inputMode == InputMode.Destructive) {

                SetSelectedItem(null);

            }

        } else if (selectionMode == SelectionMode.Entity) {

            if (inputMode == InputMode.Null) {
                //////////////////////////////////////////////////////////////////////////////////////
                /// This should never happen but if it does nothing should be created or destroyed ///
                //////////////////////////////////////////////////////////////////////////////////////
            } else if (inputMode == InputMode.Constructive) {

                SetSelectedEntity(cell.entity);

            } else if (inputMode == InputMode.Destructive) {

                SetSelectedEntity(null);

            }

        } else if (selectionMode == SelectionMode.Effect) {

            if (inputMode == InputMode.Null) {
                //////////////////////////////////////////////////////////////////////////////////////
                /// This should never happen but if it does nothing should be created or destroyed ///
                //////////////////////////////////////////////////////////////////////////////////////
            } else if (inputMode == InputMode.Constructive) {

                if (effectMode == EffectMode.Null) {
                    /////////////////////////////////////////////////////////////////////////
                    /// This should never happen but if it does nothing should be changed ///
                    /////////////////////////////////////////////////////////////////////////
                } else if (effectMode == EffectMode.Grass && FoundationData.ValidateHasGrass(cell) == true) {

                    if (editColor == true) cell.foundation.effectColorIndex = activeColor;

                } else if (effectMode == EffectMode.Fog && FoundationData.ValidateHasFog(cell) == true) {

                    if (editColor == true) cell.foundation.effectColorIndex = activeColor;

                } else if (effectMode == EffectMode.Liquid && FoundationData.ValidateHasLiquid(cell) == true) {

                    if (editColor == true) cell.foundation.effectColorIndex = activeColor;

                }

            } else if (inputMode == InputMode.Destructive) {

                if (effectMode == EffectMode.Null) {
                    /////////////////////////////////////////////////////////////////////////
                    /// This should never happen but if it does nothing should be changed ///
                    /////////////////////////////////////////////////////////////////////////
                } else if (effectMode == EffectMode.Grass && FoundationData.ValidateHasGrass(cell) == true) {

                    if (editColor == true) cell.foundation.effectColorIndex = (int)ColI.Green;

                } else if (effectMode == EffectMode.Fog && FoundationData.ValidateHasFog(cell) == true) {

                    if (editColor == true) cell.foundation.effectColorIndex = (int)ColI.Red;

                } else if (effectMode == EffectMode.Liquid && FoundationData.ValidateHasLiquid(cell) == true) {

                    if (editColor == true) cell.foundation.effectColorIndex = (int)ColI.Blue;

                }

            }

        } else if (selectionMode == SelectionMode.Waypoint) {

            if (inputMode == InputMode.Null) {
                //////////////////////////////////////////////////////////////////////////////////////
                /// This should never happen but if it does nothing should be created or destroyed ///
                //////////////////////////////////////////////////////////////////////////////////////
            } else if (inputMode == InputMode.Constructive) {

                SetSelectedWaypoint(cell);

            } else if (inputMode == InputMode.Destructive) {

                SetSelectedWaypoint(cell);

                selectedWaypoint = null;

            }

        }
    }

    private void SetSelectedObstacle(ObstacleData obstacle) {
        Metrics.WaypointVisuals(false);

        selectedObstacle = obstacle;
        selectedItem = null;
        selectedEntity = null;
        selectedWaypoint = null;

        if (selectedObstacle != null) editorUI.obstacleSelectionUI.SetUIBasedOn(selectedObstacle);
    }

    private void SetSelectedItem(ItemData item) {
        selectedObstacle = null;
        selectedItem = item;
        selectedEntity = null;
        selectedWaypoint = null;

        if (selectedItem != null) editorUI.itemSelectionUI.SetUIBasedOn(selectedItem);
    }

    private void SetSelectedEntity(EntityData entity) {
        Metrics.WaypointVisuals(false);

        selectedObstacle = null;
        selectedItem = null;
        selectedEntity = entity;
        selectedWaypoint = null;

        if (selectedEntity != null) editorUI.entitySelectionUI.SetUIBasedOn(selectedEntity);
    }

    private void SetSelectedWaypoint(CellData cellData) {
        Metrics.WaypointVisuals(false);
        WaypointManagerData waypointManager = null;
        bool isValidPlacement = false;

        if (selectedObstacle != null) waypointManager = (WaypointManagerData)selectedObstacle.GetExtension(PStrings.waypointManager);
        else if (selectedItem != null) waypointManager = (WaypointManagerData)selectedItem.GetExtension(PStrings.waypointManager);
        else if (selectedEntity != null) waypointManager = (WaypointManagerData)selectedEntity.GetExtension(PStrings.waypointManager);

        if (waypointManager == null) return;
        
        if (inputMode == InputMode.Constructive) {

            selectedWaypoint = waypointManager.GetWaypointAt(cellData);

            if (selectedWaypoint == null) {

                if (selectedObstacle != null) isValidPlacement = selectedObstacle.Validation(cellData, activeDirection);
                else if (selectedItem != null) isValidPlacement = selectedItem.Validation(cellData);
                else if (selectedEntity != null) isValidPlacement = selectedEntity.Validation(cellData);

                if (isValidPlacement == true) selectedWaypoint = WaypointData.Instantiate(cellData, waypointManager);

            } else editorUI.SetSelectWaypoint();

            if (selectedWaypoint != null) editorUI.waypointSelectionUI.SetUIBasedOn(waypointManager, selectedWaypoint);

        } else if (inputMode == InputMode.Destructive) WaypointData.Destroy(cellData, waypointManager);
    }

    #region Set Local Variables From UI

    public void ToggleTestMode(bool toggle) {
        if (toggle == true) {

            editorUI.SaveFile(editorUI.GetPath("autosave"));
            Metrics.testMode = true;
            editorUI.LoadFile(editorUI.GetPath("autosave"));
            UpdateSelectionUI(Metrics.nullVector3);

        } else {

            Metrics.testMode = false;
            editorUI.LoadFile(editorUI.GetPath("autosave"));

        }
    }

    public void ToggleLabels(bool visible) {
        Debug.Log("Nice try. This isn't a thing.");
    }

    public void ToggleGrid(bool visible) {
        if (visible) Metrics.megaSurface.material.EnableKeyword("GRID_ON");
        else Metrics.megaSurface.material.DisableKeyword("GRID_ON");
    }

    public void SetEditMode(int index) {
        editMode = (EditMode)index;
    }

    public void SetCreationMode(int index) {
        creationMode = (CreationMode)index;
        Debug.Log("SetCreationMode called indirectly.");
    }

    public void SetSelectionMode(int index) {
        selectionMode = (SelectionMode)index;
        if (selectionMode != SelectionMode.Waypoint) selectedWaypoint = null;
    }

    public void SetActiveElevation(float value) {
        activeElevation = Mathf.RoundToInt(value);
    }

    public void SetActiveDirection(int index) {
        activeDirection = (Direction)index;
    }

    public void SetActiveEffect(int index) {
        effectMode = (EffectMode)index;
        selectionMode = SelectionMode.Effect;

        UpdateToolInfo(effectMode.ToString());
    }

    public void SetActiveMegaMaterial(int index) {
        activeMegaMaterial = index;
        ToolInfo.UpdateMaterialLabel(((MatI)index).ToString());
    }

    public void SetActiveColor(int index) {
        activeColor = index;
        ToolInfo.UpdateColorLabel(((ColI)index).ToString());
    }

    public void SetActiveFoundation(string prototype) {
        activeFoundation = GridData.GetFoundationPrototype(prototype);
        creationMode = CreationMode.Foundation;

        UpdateToolInfo(activeFoundation.instructionSetID);
    }

    public void SetActiveStructure(string prototype) {
        activeStructure = GridData.GetStructurePrototype(prototype);
        creationMode = CreationMode.Structure;

        UpdateToolInfo(activeStructure.instructionSetID);
    }

    public void SetActiveCeiling(string prototype) {
        activeCeiling = GridData.GetCeilingPrototype(prototype);
        creationMode = CreationMode.Ceiling;

        UpdateToolInfo(activeCeiling.instructionSetID);
    }

    public void SetActiveObstacle(string prototype) {
        activeObstacle = GridData.GetObstaclePrototype(prototype);
        creationMode = CreationMode.Obstacle;

        UpdateToolInfo(activeObstacle.instructionSetID);
    }

    public void SetActiveItem(string prototype) {
        activeItem = GridData.GetItemPrototype(prototype);
        creationMode = CreationMode.Item;

        UpdateToolInfo(activeItem.instructionSetID);
    }

    public void SetActiveEntity(string prototype) {
        activeEntity = GridData.GetEntityPrototype(prototype);
        creationMode = CreationMode.Entity;

        UpdateToolInfo(activeEntity.instructionSetID);
    }

    #endregion

    #region SelectionMode Calls From UI

    private ExtensionData GetSelectedExtension(string identifier) {
        ExtensionData data = null;

        if (selectedObstacle != null) data = selectedObstacle.GetExtension(identifier);
        else if (selectedItem != null) data = selectedItem.GetExtension(identifier);
        else if (selectedEntity != null) data = selectedEntity.GetExtension(identifier);

        return data;
    }

    public void SetOrientation(int direction) {
        SetActiveDirection(direction);

        Vector3 uiOrientation = ((Direction)direction).ToUIEuler();
        editorUI.obstacleSelectionUI.orientation.rectTransform.eulerAngles = uiOrientation;
        editorUI.entitySelectionUI.orientation.rectTransform.eulerAngles = uiOrientation;
        editorUI.waypointSelectionUI.orientation.rectTransform.eulerAngles = uiOrientation;

        if (selectionMode == SelectionMode.Waypoint) {

            if (selectedWaypoint == null) return;
            selectedWaypoint.SetOrientation((Direction)direction);

        } else if (selectionMode == SelectionMode.Obstacle) {

            if (selectedObstacle == null) return;
            Direction newOrientation = (Direction)direction;
            if (newOrientation.IsCardinal() == false) newOrientation = newOrientation.Next();

            if (selectedObstacle.Validation(selectedObstacle.cellData, newOrientation) == true) {
                selectedObstacle.SetOrientation(newOrientation);
            }

        } else if (selectionMode == SelectionMode.Entity) {

            if (selectedEntity == null) return;
            selectedEntity.SetOrientation((Direction)direction);

        }
    }

    public void MoveThing(int direction) {
        if (selectionMode == SelectionMode.Waypoint) {
            if (selectedWaypoint == null) return;
            if (selectedObstacle == null && selectedEntity == null) return;

            Coordinates offset = ((Direction)direction).ToCoordinateOffset();
            CellData newCell = Metrics.GetCell(selectedWaypoint.cellData.coordinates.x + offset.x, selectedWaypoint.cellData.coordinates.z + offset.z);

            if (selectedObstacle != null && selectedObstacle.Validation(newCell, selectedWaypoint.orientation) == true) {
                selectedWaypoint.ChangeCell(newCell);
            }

            if (selectedEntity != null && selectedEntity.Validation(newCell) == true) {
                selectedWaypoint.ChangeCell(newCell);
            }
        }
    }



    public void SetTargetUniqueID(string targetUniqueID) {
        InteractableData data = (InteractableData)GetSelectedExtension(PStrings.interactable);
        if (data == null) return;

        data.SetTarget(int.Parse(targetUniqueID));
    }



    public void SetLightMode(int index) {
        LightData data = (LightData)GetSelectedExtension(PStrings.light);
        if (data == null) return;

        data.mode = (LightMode)index;
        editorUI.lightingSelectionUI.SetIntensitySliders(data);
    }

    public void SetLightMinimum(float value) {
        LightData data = (LightData)GetSelectedExtension(PStrings.light);
        if (data == null) return;

        if (data.mode == LightMode.Standard) {
            data.intensity = value;
            editorUI.lightingSelectionUI.SetIntensitySliders(data);
        } else data.intensityMinimum = value;
    }

    public void SetLightMaximum(float value) {
        LightData data = (LightData)GetSelectedExtension(PStrings.light);
        if (data == null) return;

        if (data.mode == LightMode.Standard) {
            data.intensity = value;
            editorUI.lightingSelectionUI.SetIntensitySliders(data);
        } else data.intensityMaximum = value;
    }

    public void SetLightPulseSpeed(float value) {
        LightData data = (LightData)GetSelectedExtension(PStrings.light);
        if (data == null) return;

        data.pulseSpeed = value;
    }

    public void SetLightStrobeSpeed(float value) {
        LightData data = (LightData)GetSelectedExtension(PStrings.light);
        if (data == null) return;

        data.strobeSpeed = value;
    }

    public void SetLightFlickerBias(float value) {
        LightData data = (LightData)GetSelectedExtension(PStrings.light);
        if (data == null) return;

        data.flickerBias = value;
    }



    public void SetDoorActivation(int index) {
        if (selectedObstacle == null) return;
        if (selectedObstacle.doorMode == DoorMode.Null) return;

        DoorData data = (DoorData)GetSelectedExtension(PStrings.door);
        if (data == null) return;

        data.activation = (DoorActivation)index;
        editorUI.doorSelectionUI.SetKeyPanelUsed(data);
    }

    public void SetDoorKey(int index) {
        if (selectedObstacle == null) return;
        if (selectedObstacle.doorMode == DoorMode.Null) return;

        DoorData data = (DoorData)GetSelectedExtension(PStrings.door);
        if (data == null) return;

        data.key = (DoorKey)index;
    }

    public void SetDoorOpenSpeed(float value) {
        if (selectedObstacle == null) return;
        if (selectedObstacle.doorMode == DoorMode.Null) return;

        DoorData data = (DoorData)GetSelectedExtension(PStrings.door);
        if (data == null) return;

        data.openSpeed = value;
    }

    public void SetDoorCloseSpeed(float value) {
        if (selectedObstacle == null) return;
        if (selectedObstacle.doorMode == DoorMode.Null) return;

        DoorData data = (DoorData)GetSelectedExtension(PStrings.door);
        if (data == null) return;

        data.closeSpeed = value;
    }

    public void SetDoorOpenTime(float value) {
        if (selectedObstacle == null) return;
        if (selectedObstacle.doorMode == DoorMode.Null) return;

        DoorData data = (DoorData)GetSelectedExtension(PStrings.door);
        if (data == null) return;

        data.openTime = value;
    }



    public void SetVFXBobbing(bool toggle) {
        VisualEffectData data = (VisualEffectData)GetSelectedExtension(PStrings.visualEffects);
        if (data == null) return;

        data.bobbing = toggle;
    }

    public void SetVFXSpinning(bool toggle) {
        VisualEffectData data = (VisualEffectData)GetSelectedExtension(PStrings.visualEffects);
        if (data == null) return;

        data.spinning = toggle;
    }

    public void SetVFXBobbingSpeed(float value) {
        VisualEffectData data = (VisualEffectData)GetSelectedExtension(PStrings.visualEffects);
        if (data == null) return;

        data.bobbingSpeed = value;
    }

    public void SetVFXSpinningSpeed(float value) {
        VisualEffectData data = (VisualEffectData)GetSelectedExtension(PStrings.visualEffects);
        if (data == null) return;

        data.spinningSpeed = value;
    }



    public void SetWaypointsCircular(bool toggle) {
        Metrics.WaypointVisuals(false);
        WaypointManagerData data = (WaypointManagerData)GetSelectedExtension(PStrings.waypointManager);
        if (data == null) return;

        data.SetCircular(toggle);
    }

    public void SetWaypointsPointHold(bool toggle) {
        WaypointManagerData data = (WaypointManagerData)GetSelectedExtension(PStrings.waypointManager);
        if (data == null) return;

        data.pointHold = toggle;
    }

    public void SetWaypointsEndHold(bool toggle) {
        WaypointManagerData data = (WaypointManagerData)GetSelectedExtension(PStrings.waypointManager);
        if (data == null) return;

        data.endHold = toggle;
    }

    public void SetWaypointsPointHoldFor(float value) {
        WaypointManagerData data = (WaypointManagerData)GetSelectedExtension(PStrings.waypointManager);
        if (data == null) return;

        data.pointHoldFor = value;
    }

    public void SetWaypointsEndHoldFor(float value) {
        WaypointManagerData data = (WaypointManagerData)GetSelectedExtension(PStrings.waypointManager);
        if (data == null) return;

        data.endHoldFor = value;
    }



    public void SetTag1(bool toggle) {
        //if (selectedObstacle != null) selectedObstacle.tag1 = toggle;
        if (selectedItem != null) selectedItem.infinite = toggle;
        if (selectedEntity != null) selectedEntity.immortal = toggle;
    }

    public void SetTag2(bool toggle) {
        //if (selectedObstacle != null) selectedObstacle.tag2 = toggle;
        if (selectedItem != null) selectedItem.respawns = toggle;
        if (selectedEntity != null) selectedEntity.respawns = toggle;
    }

    public void SetTag3(bool toggle) {
        //if (selectedObstacle != null) selectedObstacle.tag3 = toggle;
        if (selectedItem != null) selectedItem.despawns = toggle;
        if (selectedEntity != null) selectedEntity.despawns = toggle;
    }

    public void SetTag4(bool toggle) {
        //if (selectedObstacle != null) selectedObstacle.tag4 = toggle;
        //if (selectedItem != null) selectedItem.tag4 = toggle;
        if (selectedEntity != null) selectedEntity.blind = toggle;
    }

    public void SetTag5(bool toggle) {
        //if (selectedObstacle != null) selectedObstacle.tag5 = toggle;
        //if (selectedItem != null) selectedItem.tag5 = toggle;
        if (selectedEntity != null) selectedEntity.deaf = toggle;
    }



    public void SetVariable1(float value) {
        //if (selectedObstacle != null) selectedObstacle.variable1 = value;
        //if (selectedItem != null) selectedItem.variable1 = value;
        //if (selectedEntity != null) selectedEntity.variable1 = value;
    }

    public void SetVariable2(float value) {
        //if (selectedObstacle != null) selectedObstacle.variable2 = value;
        if (selectedItem != null) selectedItem.respawnsAfter = value;
        if (selectedEntity != null) selectedEntity.respawnsAfter = value;
    }

    public void SetVariable3(float value) {
        //if (selectedObstacle != null) selectedObstacle.variable3 = value;
        if (selectedItem != null) selectedItem.despawnsAfter = value;
        if (selectedEntity != null) selectedEntity.despawnsAfter = value;
    }

    public void SetVariable4(float value) {
        //if (selectedObstacle != null) selectedObstacle.variable4 = value;
        //if (selectedItem != null) selectedItem.variable4 = value;
        //if (selectedEntity != null) selectedEntity.variable4 = value;
    }

    public void SetVariable5(float value) {
        //if (selectedObstacle != null) selectedObstacle.variable5 = value;
        //if (selectedItem != null) selectedItem.variable5 = value;
        //if (selectedEntity != null) selectedEntity.variable5 = value;
    }

    #endregion

    private void UpdateToolInfo(string identifier) {
        string mode = editMode.ToString();
        string category = (editMode == EditMode.Creation) ? creationMode.ToString() : (editMode == EditMode.Selection) ? selectionMode.ToString() : "NULL";
        string color = ((ColI)activeColor).ToString();
        string material = ((MatI)activeMegaMaterial).ToString();

        if (selectionMode == SelectionMode.Effect) {
            material = "NULL";
        } 
        
        if (creationMode == CreationMode.Obstacle || creationMode == CreationMode.Item || creationMode == CreationMode.Entity) {
            color = "NULL";
            material = "NULL";
        }

        ToolInfo.UpdateModeLabel(mode);
        ToolInfo.UpdateCategoryLabel(category);
        ToolInfo.UpdateIdentifierLabel(identifier);
        ToolInfo.UpdateColorLabel(color);
        ToolInfo.UpdateMaterialLabel(material);
    }
}
