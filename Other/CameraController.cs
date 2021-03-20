using UnityEngine;

public class CameraController : MonoBehaviour {

    /// <summary>
    /// CheckCollisions is setting the probe's position to be based on the camera's position.
    /// This is incorrect as the probe's position should not be mutable based on collision.
    /// </summary>

    #region References

    [Header("Internal Information")]                    // These should be set to protected. They're only public to make debugging easier. //
    public CameraAttributes attributes;
    public CameraStateVariables states;
    public CameraSmoothingVariables cSmooth;

    private GameController core;
    public Camera mainCamera { get; private set; }
    public Camera reflectionCamera;
    public Camera refractionCamera;
    private Transform target;
    private Entity targetEntity;
    private Transform eye;

    public Transform rig { get; private set; }
    public Transform pivot { get; private set; }
    public Transform mount { get; private set; }

    private float lookPitch;
    private float lookYaw;

    #endregion

    #region Helpers

    private bool UseLeftPivot() {
        if (targetEntity.states.cover.isInCover == false && states.leftPivot == true) return true;
        if (targetEntity.states.cover.isInCover == true && targetEntity.states.cover.relativeRight == false) return true;
        return false;
    }

    #endregion

    private Transform collisionSimulator;
    private Vector3[] collisionBounds;
    private bool collided;
    private float collisionDistance;

    public void Initialize() {
        core = GameController.Instance;
        mainCamera = GetComponentInChildren<Camera>();
        target = core.playerController.transform;
        targetEntity = target.GetComponent<Entity>();

        collisionSimulator = new GameObject("Camera Collision Simulator").transform;

        rig = transform;
        pivot = rig.GetChild(0);
        mount = pivot.GetChild(0);

        attributes.InitializeDefaults();
        cSmooth.InitializeDefaults();
    }

    public void FixedTick(float deltaTime) {
        states.deltaTime = deltaTime;
        if (target == null) return;
    }

    public void Tick(float deltaTime) {
        states.deltaTime = deltaTime;
        if (target == null) return;

        if (core.gameState == GameState.InMenu) ClearInput();
        else ListenInput();
    }

    public void LateTick(float deltaTime) {
        states.deltaTime = deltaTime;
        if (target == null) return;

        CameraRotation();
        CameraMovement();
        CameraZoom();
    }

    private void ClearInput() {
        lookPitch = 0;
        lookYaw = 0;
        targetEntity.states.lookMagnitude = 0;
    }

    private void ListenInput() {
        lookPitch = Input.GetAxisRaw(InputStrings.lookY);
        lookYaw = Input.GetAxisRaw(InputStrings.lookX);
        targetEntity.states.lookMagnitude = new Vector2(lookYaw, lookPitch).magnitude;

        if (Input.GetButton(InputStrings.shoulder)) targetEntity.states.peakTimer += states.deltaTime;
        else targetEntity.states.peakTimer = 0;

        if (Input.GetButtonUp(InputStrings.shoulder)) {
            if (targetEntity.states.cover.isInCover == false && targetEntity.states.peakToggle == false) states.leftPivot = !states.leftPivot;
        }
    }

    private void CameraRotation() {
        float pitchSensitivity = (targetEntity.states.cover.isPeaking) ? attributes.pitchSensitivity * 0.5f : attributes.pitchSensitivity;
        float yawSensitivity = (targetEntity.states.cover.isPeaking) ? attributes.yawSensitivity * 0.5f : attributes.yawSensitivity;

        cSmooth.pitchTarget -= lookPitch * pitchSensitivity;
        cSmooth.pitchTarget = Mathf.Clamp(cSmooth.pitchTarget, attributes.pitchClamping.x, attributes.pitchClamping.y);
        cSmooth.yawTarget += lookYaw * yawSensitivity;

        if (cSmooth.pitchSmoothing) cSmooth.pitchCurrent = Mathf.SmoothDamp(cSmooth.pitchCurrent, cSmooth.pitchTarget, ref cSmooth.pitchVelocity, cSmooth.pitchTime);
        else cSmooth.pitchCurrent = cSmooth.pitchTarget;
        if (cSmooth.yawSmoothing) cSmooth.yawCurrent = Mathf.SmoothDamp(cSmooth.yawCurrent, cSmooth.yawTarget, ref cSmooth.yawVelocity, cSmooth.yawTime);
        else cSmooth.yawCurrent = cSmooth.yawTarget;

        rig.localRotation = Quaternion.Euler(0, cSmooth.yawCurrent, 0);
        pivot.localRotation = Quaternion.Euler(cSmooth.pitchCurrent, 0, 0);
    }

    private void CameraMovement() {
        if (targetEntity == null) { Debug.LogError("CameraController: Not programmed to move with null entity."); return; }

        if (eye == null) eye = targetEntity.GetEye();
        SetTargetsDefault();
        SetTargetsSpecial();
        CheckCollisions();
        if (collided) states.targetDistance = collisionDistance;
        //if (collided) {
        //    states.targetDistance = collisionDistance;
        //    cSmooth.mountTarget = new Vector3(mount.localPosition.x, mount.localPosition.y, -states.targetDistance);
        //    cSmooth.mountCurrent = cSmooth.mountTarget;
        //}
        if (UseLeftPivot() == true) states.targetOffset = -states.targetOffset;

        // Cover Special //
        cSmooth.specialTarget = Vector3.zero;
        if (targetEntity.states.cover.isInCover && targetEntity.states.action != CharacterAction.Aiming) {
            cSmooth.specialTarget += targetEntity.states.cover.probe.forward * (attributes.standDistance * 0.5f);
            if (targetEntity.states.cover.isAtEdge) cSmooth.specialTarget += target.forward * attributes.edgeDistance;
        }
        if (cSmooth.specialSmoothing) cSmooth.specialCurrent = Vector3.SmoothDamp(cSmooth.specialCurrent, cSmooth.specialTarget, ref cSmooth.specialVelocity, cSmooth.specialTime);
        else cSmooth.specialCurrent = cSmooth.specialTarget;

        // Rig //
        cSmooth.rigTarget = target.position;
        if (cSmooth.rigSmoothing) cSmooth.rigCurrent = Vector3.SmoothDamp(cSmooth.rigCurrent, cSmooth.rigTarget, ref cSmooth.rigVelocity, cSmooth.rigTime);
        else cSmooth.rigCurrent = cSmooth.rigTarget;
        rig.position = cSmooth.rigCurrent + cSmooth.specialCurrent;

        // Pivot //
        cSmooth.pivotTarget = new Vector3(states.targetOffset, states.targetHeight, pivot.localPosition.z);
        if (cSmooth.pivotSmoothing) cSmooth.pivotCurrent = Vector3.SmoothDamp(cSmooth.pivotCurrent, cSmooth.pivotTarget, ref cSmooth.pivotVelocity, cSmooth.pivotTime);
        else cSmooth.pivotCurrent = cSmooth.pivotTarget;
        pivot.localPosition = cSmooth.pivotCurrent;

        // Mount //
        cSmooth.mountTarget = new Vector3(mount.localPosition.x, mount.localPosition.y, -states.targetDistance);
        if (cSmooth.mountSmoothing) cSmooth.mountCurrent = Vector3.SmoothDamp(cSmooth.mountCurrent, cSmooth.mountTarget, ref cSmooth.mountVelocity, cSmooth.mountTime);
        else cSmooth.mountCurrent = cSmooth.mountTarget;
        mount.localPosition = cSmooth.mountCurrent;

        #region Reflection / Refraction Pass (Prep)

        refractionCamera.transform.position = mainCamera.transform.position;
        refractionCamera.transform.rotation = mainCamera.transform.rotation;

        float distanceOverWater = (mainCamera.transform.position.y - (Statics.maximumGlobalHeight * Statics.seaLevel));

        reflectionCamera.transform.position = mainCamera.transform.position - (Vector3.up * distanceOverWater);
        reflectionCamera.transform.eulerAngles = new Vector3(-pivot.eulerAngles.x, mainCamera.transform.eulerAngles.y, 0);

        #endregion

    }

    private void CameraZoom() {
        cSmooth.peakTarget = (targetEntity.states.peakToggle) ? attributes.fieldOfViewPeaking : attributes.fieldOfViewNormal;
        if (cSmooth.peakSmoothing) cSmooth.peakCurrent = Mathf.SmoothDamp(cSmooth.peakCurrent, cSmooth.peakTarget, ref cSmooth.peakVelocity, cSmooth.peakTime);
        else cSmooth.peakCurrent = cSmooth.peakTarget;

        mainCamera.fieldOfView = cSmooth.peakCurrent;
        reflectionCamera.fieldOfView = cSmooth.peakCurrent;
        refractionCamera.fieldOfView = cSmooth.peakCurrent;
    }

    private void CheckCollisionBounds(Transform camTran, ref Vector3[] rayTargets) {
        Vector3 rayboxSize = new Vector3(1.6f, 0.9f, 1f); // Half-Extents (x, y) Full Overshoot (z)
        rayTargets = new Vector3[9];

        rayTargets[0] = camTran.position - (camTran.forward * rayboxSize.z);    // Behind Camera

        rayTargets[1] = camTran.position + (camTran.up * rayboxSize.y);         // Over Camera
        rayTargets[2] = camTran.position - (camTran.up * rayboxSize.y);         // Under Camera
        rayTargets[3] = camTran.position + (camTran.right * rayboxSize.x);      // Right of Camera
        rayTargets[4] = camTran.position - (camTran.right * rayboxSize.x);      // Left of Camera

        rayTargets[5] = camTran.position + (camTran.right * rayboxSize.x) + (camTran.up * rayboxSize.y);    // Top Right of Camera
        rayTargets[6] = camTran.position - (camTran.right * rayboxSize.x) + (camTran.up * rayboxSize.y);    // Top Left of Camera
        rayTargets[7] = camTran.position + (camTran.right * rayboxSize.x) - (camTran.up * rayboxSize.y);    // Bottom Right of Camera
        rayTargets[8] = camTran.position - (camTran.right * rayboxSize.x) - (camTran.up * rayboxSize.y);    // Bottom Left of Camera
    }

    private void CheckCollisions() {
        collisionSimulator.position = mainCamera.transform.position + (-mainCamera.transform.forward * states.targetDistance);
        CheckCollisionBounds(collisionSimulator, ref collisionBounds);

        collided = false;
        float closestCollision = -1;

        for (int i = 0; i < collisionBounds.Length; i++) {

            if (targetEntity.states.stance == CharacterStance.Crawling) continue;

            Ray ray = new Ray(eye.position, collisionBounds[i] - eye.position);
            RaycastHit hit;
            float targetDistance = Vector3.Distance(eye.position, collisionBounds[i]);

            if (Physics.Raycast(ray, out hit, targetDistance, core.cameraCollisionLayers, QueryTriggerInteraction.Ignore)) {
                collided = true;
                if (closestCollision == -1) {
                    closestCollision = hit.distance - 0.5f;
                    closestCollision = Mathf.Clamp(closestCollision, 0.2f, states.targetDistance);
                } else {
                    if (hit.distance < closestCollision) {
                        closestCollision = hit.distance - 0.5f;
                        closestCollision = Mathf.Clamp(closestCollision, 0.2f, states.targetDistance);
                    }
                }
            }
            Debug.DrawLine(eye.position, collisionBounds[i], (collided) ? Color.red : Color.green);
        }

        collisionDistance = closestCollision;
    }

    private void SetTargetsDefault() {

        if (targetEntity.states.stance == CharacterStance.Standing) {
            states.targetHeight = attributes.standHeight;
            states.targetOffset = attributes.standOffset;
            states.targetDistance = attributes.standDistance;
        } else if (targetEntity.states.stance == CharacterStance.Crouching) {
            states.targetHeight = attributes.crouchHeight;
            states.targetOffset = attributes.crouchOffset;
            states.targetDistance = attributes.crouchDistance;
        } else if (targetEntity.states.stance == CharacterStance.Crawling) {
            states.targetHeight = attributes.crawlHeight;
            states.targetOffset = attributes.crawlOffset;
            states.targetDistance = attributes.crawlDistance;
        }

    }

    private void SetTargetsSpecial() {

        if (targetEntity.states.action == CharacterAction.Aiming) {
            states.targetHeight = eye.position.y - targetEntity.transform.position.y;
            states.targetOffset = attributes.aimOffset;
            states.targetDistance = attributes.aimDistance;
        } else if (targetEntity.states.action == CharacterAction.Sprinting) {
            states.targetHeight = attributes.sprintHeight;
            states.targetOffset = attributes.sprintOffset;
            states.targetDistance = attributes.sprintDistance;
        } else if (targetEntity.states.cover.isInCover) {
            states.targetHeight = attributes.standHeight;
            states.targetOffset = attributes.standOffset;
            states.targetDistance = attributes.standDistance * 0.5f;
        }

    }

}

[System.Serializable]
public class CameraAttributes {
    public float fieldOfViewNormal;
    public float fieldOfViewPeaking;
    [Space(12)]
    public Vector2 pitchClamping;
    [Space(12)]
    public float pitchSensitivity;
    public float yawSensitivity;
    [Space(12)]
    public float aimOffset;
    public float aimDistance;
    [Space(12)]
    public float standHeight;
    public float standOffset;
    public float standDistance;
    [Space(12)]
    public float crouchHeight;
    public float crouchOffset;
    public float crouchDistance;
    [Space(12)]
    public float crawlHeight;
    public float crawlOffset;
    public float crawlDistance;
    [Space(12)]
    public float sprintHeight;
    public float sprintOffset;
    public float sprintDistance;
    [Space(12)]
    public float edgeDistance;

    public void InitializeDefaults() {

        float scale = 0.9f;

        fieldOfViewNormal = 60;
        fieldOfViewPeaking = fieldOfViewNormal * 0.5f;

        pitchClamping = new Vector2(-60, 60);

        pitchSensitivity = 6;
        yawSensitivity = 6;

        aimOffset = 0.75f;
        aimDistance = 1.0f;

        standHeight = 1.75f * scale;
        standOffset = 0.5f;
        standDistance = 1.5f; // 3.0f;

        crouchHeight = 1.25f * scale;
        crouchOffset = 0.5f;
        crouchDistance = 1.25f; // 2.25f;

        crawlHeight = 0.5f * scale;
        crawlOffset = 0.5f;
        crawlDistance = 1.75f;

        sprintHeight = 1.5f * scale;
        sprintOffset = 0.0f;
        sprintDistance = 2.0f;

        edgeDistance = 1;

    }

}

[System.Serializable]
public class CameraStateVariables {
    public bool leftPivot;
    [Space(12)]
    public float targetOffset;
    public float targetHeight;
    public float targetDistance;
    [Space(12)]
    public float deltaTime;
}

[System.Serializable]
public class CameraSmoothingVariables {
    public bool pitchSmoothing = true;
    public float pitchCurrent;
    public float pitchTarget;
    public float pitchVelocity;
    public float pitchTime;
    [Space(12)]
    public bool yawSmoothing = true;
    public float yawCurrent;
    public float yawTarget;
    public float yawVelocity;
    public float yawTime;
    [Space(12)]
    public bool rigSmoothing = false;
    public Vector3 rigCurrent;
    public Vector3 rigTarget;
    public Vector3 rigVelocity;
    public float rigTime;
    [Space(12)]
    public bool pivotSmoothing = true;
    public Vector3 pivotCurrent;
    public Vector3 pivotTarget;
    public Vector3 pivotVelocity;
    public float pivotTime;
    [Space(12)]
    public bool mountSmoothing = true;
    public Vector3 mountCurrent;
    public Vector3 mountTarget;
    public Vector3 mountVelocity;
    public float mountTime;
    [Space(12)]
    public bool specialSmoothing = true;
    public Vector3 specialCurrent;
    public Vector3 specialTarget;
    public Vector3 specialVelocity;
    public float specialTime;
    [Space(12)]
    public bool peakSmoothing = true;
    public float peakCurrent;
    public float peakTarget;
    public float peakVelocity;
    public float peakTime;

    public void InitializeDefaults() {

        pitchTime = 0.1f;
        yawTime = 0.1f;
        rigTime = 0.1f;
        pivotTime = 0.2f;
        mountTime = 0.1f;
        specialTime = 0.2f;
        peakTime = 0.1f;

    }

}
