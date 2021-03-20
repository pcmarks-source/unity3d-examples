using UnityEngine;

public class PlayerController : EntityController {

    private Camera mainCamera;
    private GameCameraController cameraController;

    public override void Initialize(int ownerUniqueID) {
        base.Initialize(ownerUniqueID);

        mainCamera = Camera.main;

        attributes.healthMaximum = 100;

        attributes.speedStandingSlow = 10;
        attributes.speedStandingFast = 10;

        attributes.speedAiming = 8;
        attributes.speedSprinting = 15;

        state.velocityTime = 0.15f;

        state.health = attributes.healthMaximum;

        cameraController = GameObject.FindObjectOfType<GameCameraController>();
    }

    public override void FixedInstructionCycle() {
        base.FixedInstructionCycle();
    }

    public override void InstructionCycle() {
        ListenForInput();

        base.InstructionCycle();
    }

    public override void LateInstructionCycle() {
        base.LateInstructionCycle();
        cameraController.InstructionCycle(entityData.gameObject.transform.position);
    }

    private void ListenForInput() {
        if (Metrics.testMode == false) return;

        input.aimRequest = (Input.GetButton(IStrings.aim));
        input.attackStartRequest = (input.aimRequest == true) ? (Input.GetButton(IStrings.attack)) : false;
        input.attackStopRequest = (Input.GetButtonUp(IStrings.attack));
        if (weaponController.CanReload() && Input.GetButtonUp(IStrings.reload)) input.reloadRequest = true;

        if (input.aimRequest == false) input.interactRequest = (Input.GetButtonDown(IStrings.attack));

        input.lookPosition = GetPointUnderMouse(PointUnderMouseStyle.LookPoint);
        input.aimPosition = GetPointUnderMouse(PointUnderMouseStyle.AimPoint);
        Statics.mouseOverPosition = GetPointUnderMouse(PointUnderMouseStyle.CrosshairPoint);

        input.interactPosition = (input.interactRequest) ? GetPointUnderMouse(PointUnderMouseStyle.InteractPoint) : rigidbody.transform.forward;

        input.moveDirection = new Vector3(Input.GetAxisRaw(IStrings.moveX), 0, Input.GetAxisRaw(IStrings.moveZ)).normalized;
        input.moveMagnitude = input.moveDirection.magnitude;

        input.lookDirection = (input.aimRequest) ? input.lookPosition - rigidbody.position : (input.moveDirection != Vector3.zero) ? input.moveDirection : input.lookDirection;
    }

    private Vector3 GetPointUnderMouse(PointUnderMouseStyle style, float distance = Mathf.Infinity) {

        bool failed = false;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Vector3 point = Vector3.zero;

        if (style == PointUnderMouseStyle.LookPoint) {

            Plane plane = new Plane(Vector3.up, rigidbody.position);
            plane.Raycast(ray, out distance);
            point = ray.GetPoint(distance);

        } else if (style == PointUnderMouseStyle.AimPoint) {

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, distance, Statics.mouseOverAimMask, QueryTriggerInteraction.Ignore))
                point = hit.point + (Vector3.up * weaponController.GetHandOffset());
            else failed = true;

        } else if (style == PointUnderMouseStyle.CrosshairPoint) {

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, distance, Statics.mouseOverAimMask, QueryTriggerInteraction.Ignore))
                point = hit.point + (Vector3.up * (weaponController.GetHandOffset() + weaponController.GetMuzzleOffset()));
            else failed = true;

        } else if (style == PointUnderMouseStyle.InteractPoint) {

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, distance, Statics.mouseOverInteractionMask, QueryTriggerInteraction.Ignore))
                point = hit.point + (Vector3.up * (weaponController.GetHandOffset() + weaponController.GetMuzzleOffset()));
            else failed = true;

        }

        if (failed == true || style == PointUnderMouseStyle.FailSafe) {

            Plane plane = new Plane(Vector3.up, rigidbody.position);
            plane.Raycast(ray, out distance);
            point = ray.GetPoint(distance);
            if (failed == true) point.y += weaponController.GetHandOffset();

        }

        return point;
    }
}
