using System.Xml;
using UnityEngine;

public class EntityController : ExtensionData {

    protected EntityData entityData;
    protected Rigidbody rigidbody;

    protected WeaponController weaponController;
    protected WaypointManagerData waypointManager;
    protected PathfindManagerData pathfindManager;

    public EntityInput input;
    public EntityAttributes attributes;
    public EntityState state;

    private bool CanAim(Vector3 point) {
        if (Statics.canSuicide == true) return true;
        if ((new Vector2(point.x, point.z) - new Vector2(rigidbody.position.x, rigidbody.position.z)).sqrMagnitude > 1) return true;
        return false;
    }

    public override void Initialize(int ownerUniqueID) {
        this.ownerUniqueID = ownerUniqueID;
        identifier = PStrings.entityController;

        input = new EntityInput();
        input.InitializeDefaults();
        attributes = new EntityAttributes();
        attributes.InitializeDefaults();
        state = new EntityState();
        state.InitializeDefaults();

        entityData = (EntityData)GridData.GetExtensible(ownerUniqueID);
        rigidbody = entityData.gameObject.GetComponent<Rigidbody>();
        weaponController = entityData.gameObject.GetComponent<WeaponController>();
        if (weaponController != null) weaponController.Initialize(entityData);
        waypointManager = (WaypointManagerData)entityData.GetExtension(PStrings.waypointManager);
        pathfindManager = (PathfindManagerData)entityData.GetExtension(PStrings.pathfindManager);

        state.rotationCurrent = Metrics.OrientationFromDirection[(int)entityData.orientation];
    }

    public override void FixedInstructionCycle() {
        if (weaponController != null) weaponController.FixedTick();

        rigidbody.transform.eulerAngles = Vector3.up * state.rotationCurrent;
        rigidbody.MovePosition(rigidbody.position + state.velocityCurrent * Metrics.deltaTime);
    }

    public override void InstructionCycle() {
        if (weaponController != null) weaponController.Tick();
        StateMachine();

        if (weaponController != null) {
            if (state.action == EntityAction.Aiming && CanAim(state.aimCurrent) == true) weaponController.Aim(state.aimCurrent);
            else weaponController.AtEase();

            if (input.attackStartRequest == true) weaponController.OnTriggerPull();
            if (input.attackStopRequest == true) weaponController.OnTriggerRelease();
            if (input.reloadRequest == true) weaponController.Reload();
        }

        Interact();
    }

    public override void LateInstructionCycle() {
        if (weaponController != null) weaponController.LateTick();
    }

    private void Interact() {
        if (input.interactRequest == true) {

            CellData interactCell = GridData.GetCellAt(Coordinates.FromWorldSpace(input.interactPosition));

            if (interactCell != null) {

                ExtensibleData extensible = null;

                if (interactCell.obstacle != null) extensible = interactCell.obstacle;
                else if (interactCell.item != null) extensible = interactCell.item;
                else if (interactCell.entity != null) extensible = interactCell.entity;

                if (extensible != null) {

                    InteractableData interactable = (InteractableData)extensible.GetExtension(PStrings.interactable);

                    if (interactable != null) {

                        float sqrDistanceThreshold = 2.0f;
                        Vector3 interactablePosition = new Vector3(extensible.gameObject.transform.position.x, 0, extensible.gameObject.transform.position.z);
                        Vector3 interactorPosition = new Vector3(rigidbody.position.x, 0, rigidbody.position.z);
                        if ((interactablePosition - interactorPosition).sqrMagnitude <= (sqrDistanceThreshold * sqrDistanceThreshold)) {
                            interactable.Switch();
                        }
                    }
                }
            }
        }
    }

    protected virtual void StateMachine() {
        state.action = (input.aimRequest == true || input.reloadRequest == true) ? EntityAction.Aiming : EntityAction.Idle;
        float rotationTime = (state.action == EntityAction.Aiming) ? state.rotationTime : state.rotationTime * 2f;

        state.aimTarget = input.aimPosition;
        if (state.aimSmoothing) state.aimCurrent = Vector3.SmoothDamp(state.aimCurrent, state.aimTarget, ref state.aimVelocity, state.aimTime);
        else state.aimCurrent = state.aimTarget;

        state.rotationTarget = Mathf.Atan2(input.lookDirection.x, input.lookDirection.z) * Mathf.Rad2Deg;
        if (state.rotationSmoothing) state.rotationCurrent = Mathf.SmoothDampAngle(state.rotationCurrent, state.rotationTarget, ref state.rotationVelocity, rotationTime);
        else state.rotationCurrent = state.rotationTarget;

        if (state.action != EntityAction.Aiming && state.pace != EntityPace.Sprinting) {
            bool walking = (state.pace == EntityPace.Walking);

            if (state.stance == EntityStance.Standing) state.speedTarget = (walking) ? attributes.speedStandingSlow : attributes.speedStandingFast;
            else if (state.stance == EntityStance.Crawling) state.speedTarget = (walking) ? attributes.speedCrawlingSlow : attributes.speedCrawlingFast;
            else if (state.stance == EntityStance.Crouching) state.speedTarget = (walking) ? attributes.speedCrouchingSlow : attributes.speedCrouchingFast;

        } else if (state.pace == EntityPace.Sprinting) state.speedTarget = attributes.speedSprinting;
        else state.speedTarget = attributes.speedAiming;

        state.speedTarget = state.speedTarget * input.moveMagnitude;
        if (state.speedSmoothing) state.speedCurrent = Mathf.SmoothDamp(state.speedCurrent, state.speedTarget, ref state.speedVelocity, state.speedTime);
        else state.speedCurrent = state.speedTarget;

        state.velocityTarget = input.moveDirection * state.speedCurrent;
        if (state.velocitySmoothing) state.velocityCurrent = Vector3.SmoothDamp(state.velocityCurrent, state.velocityTarget, ref state.velocityVelocityLol, state.velocityTime);
        else state.velocityCurrent = state.velocityTarget;
    }

    protected virtual void Animation() {

    }

    public override void WriteXml(XmlWriter writer) {

    }

    public override void ReadXml(XmlReader reader) {

    }
}

public enum AIState { Normal, Investigation, Caution, Evasion, Alert }
public enum EntityPace { Walking, Jogging, Sprinting }
public enum EntityStance { Standing, Crouching, Crawling }
public enum EntityAction { Idle, Aiming, Interacting }

[System.Serializable]
public class EntityState {
    [Header("Flags")]
    public bool isAlive;
    public bool isGrounded;
    [Header("Stats")]
    public int health;
    [Header("States")]
    public EntityPace pace;
    public EntityStance stance;
    public EntityAction action;
    public EntityAction actionCached;
    [Header("Smoothing")]
    public bool aimSmoothing;
    public Vector3 aimCurrent;
    public Vector3 aimTarget;
    public Vector3 aimVelocity;
    public float aimTime;
    [Space(12)]
    public bool rotationSmoothing;
    public float rotationCurrent;
    public float rotationTarget;
    public float rotationVelocity;
    public float rotationTime;
    [Space(12)]
    public bool speedSmoothing;
    public float speedCurrent;
    public float speedTarget;
    public float speedVelocity;
    public float speedTime;
    [Space(12)]
    public bool velocitySmoothing;
    public Vector3 velocityCurrent;
    public Vector3 velocityTarget;
    public Vector3 velocityVelocityLol;
    public float velocityTime;

    public void InitializeDefaults() {

        isAlive = true;
        isGrounded = true;

        health = 100;

        pace = EntityPace.Jogging;
        stance = EntityStance.Standing;
        action = EntityAction.Idle;
        actionCached = EntityAction.Idle;

        aimSmoothing = true;
        aimTime = 0.05f;

        rotationSmoothing = true;
        rotationTime = 0.05f;

        speedSmoothing = false;
        speedTime = 0.15f;

        velocitySmoothing = true;
        velocityTime = 0.15f;
    }
}

[System.Serializable]
public class EntityAttributes {
    [Header("Stats")]
    public int healthMaximum;
    public float senseRadius;
    public float fieldOfView;
    [Header("Speed")]
    public float speedStandingSlow;
    public float speedStandingFast;
    [Space(12)]
    public float speedCrouchingSlow;
    public float speedCrouchingFast;
    [Space(12)]
    public float speedCrawlingSlow;
    public float speedCrawlingFast;
    [Space(12)]
    public float speedAiming;
    public float speedSprinting;

    public void InitializeDefaults() {

        if (Statics.stealthStyle == true) {
            healthMaximum = 100;

            senseRadius = 10f;
            fieldOfView = 90f;

            speedStandingSlow = 2;
            speedStandingFast = 5;

            speedCrouchingSlow = 2;
            speedCrouchingFast = 3;

            speedCrawlingSlow = 1;
            speedCrawlingFast = 2;

            speedAiming = 2;
            speedSprinting = 8;
        } else {
            healthMaximum = 100;

            senseRadius = 20f;
            fieldOfView = 160f;

            speedStandingSlow = 5;
            speedStandingFast = 5;

            speedCrouchingSlow = 3;
            speedCrouchingFast = 3;

            speedCrawlingSlow = 2;
            speedCrawlingFast = 2;

            speedAiming = 5;
            speedSprinting = 8;
        }

    }
}

[System.Serializable]
public class EntityInput {
    public Vector3 aimPosition;
    public Vector3 lookPosition;

    public Vector3 interactPosition;

    public Vector3 lookDirection;
    public Vector3 moveDirection;
    public float moveMagnitude;

    public bool aimRequest;
    public bool attackStartRequest;
    public bool attackStopRequest;
    public bool reloadRequest;

    public bool interactRequest;

    public void InitializeDefaults() {
        aimPosition = Vector3.zero;
        lookPosition = Vector3.zero;

        interactPosition = Vector3.zero;

        lookDirection = Vector3.zero;
        moveDirection = Vector3.zero;
        moveMagnitude = 0;

        aimRequest = false;
        attackStartRequest = false;
        attackStopRequest = false;
        reloadRequest = false;

        interactRequest = false;
    }
}
