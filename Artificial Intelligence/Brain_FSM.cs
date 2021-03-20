using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class Brain_FSM : MonoBehaviour {

    // Self Assigned //
    GameController gameController;
    Unit_FSM unit;
    public Entity entity { get; protected set; }
    Transform eyes;
    GunController gunController;
    NavMeshAgent pathfinder;
    AIStates currentState;

    [Header("Senses")]
    [Range(0, 50)] [SerializeField] float senseRadius = 20;
    float senseFringe;
    [Range(0, 360)] [SerializeField] float fieldOfView = 110;
    [SerializeField] [Range(0, 1)] float scanRefreshRate = 0.1f;
    LayerMask visibleToAI;

    [Header("Traits")]
    [SerializeField] bool patrolIsCircuit = false;
    [SerializeField] float timeToWait = 4;
    [SerializeField] bool coverClosestToEnemy = true;

    [Header("Flavor")]
    [SerializeField] Text expressionText;

    // System Variables //
    bool busy = false;
    bool anomalyDetected = false;
    bool hostileDetected = false;

    Entity hostile;
    Vector3 anomalyLocation;
    Vector3 previousAnomalyLocation;

    Cover_FSM cover;

    [HideInInspector] public Transform patrol;
    int patrolIndex;
    Vector3[] waypoints;
    float timeWaited;

    float currentSpeed;
    float aSpeedPercent;
    Vector3 aVelocity;

    float shotTimer;

    // Dynamic Variables // -------------------------
    bool isAlive(Entity other) {
        return (other != null && !other.dead);
    }

    bool canHear(Entity other) {
        if (other.currentSpeed < 1.1f) return false;
        if (CalculatePathLength(other.transform.position) > senseRadius) return false;
        return true;
    }

    bool canSee(Entity other, float distanceToOther) {
        Vector3 directionToOther = (other.transform.position - eyes.position).normalized;
        if (!EntityIsInFieldOfView(directionToOther)) return false;
        if (EntityIsBehindObject(directionToOther, distanceToOther)) return false;
        return true;
    }

    bool EntityIsInFieldOfView(Vector3 directionToOther) {
        return Vector3.Angle(eyes.forward, directionToOther) < fieldOfView / 2;
    }

    bool EntityIsBehindObject(Vector3 directionToOther, float distanceToOther) {
        return Physics.Raycast(eyes.position, directionToOther, distanceToOther, gameController.obscuresVision);
    }

    bool CanShoot(Vector3 otherLocation) {
        if (shotTimer > Time.time) return false;
        if (entity.animator.GetFloat("stancePercent") < 0.98f) return false;
        if (HaveClearShot(otherLocation) == false) return false;
        return true;
    }

    bool HaveClearShot(Vector3 hostilePosition) {
        float distanceToOther = Vector3.Distance(hostilePosition, eyes.position);

        Ray ray = new Ray(eyes.position, eyes.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, distanceToOther, visibleToAI, QueryTriggerInteraction.Collide)) {
            Entity other = hit.collider.GetComponent<Entity>();
            if (other != null && other.isPlayer == false) return false; 
        }

        Vector3 directionToOther = (hostilePosition - eyes.position).normalized;
        if (EntityIsBehindObject(directionToOther, distanceToOther)) return false;

        return true;
    }

    bool CoverStillGood() {
        if (cover == null) return false;

        float coverDistance = Vector3.Distance(anomalyLocation, cover.transform.position);
        Vector3 directionToCover = ((cover.transform.position + cover.transform.up) - anomalyLocation).normalized;
        if (!Physics.Raycast(anomalyLocation, directionToCover, coverDistance, gameController.obscuresVision)) return false;

        return true;
    }

    Vector3 CalculateDirectionFromAngle(float angleInDegrees, bool angleIsGlobal) {
        if (!angleIsGlobal) angleInDegrees += transform.eulerAngles.y;
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }

    float CalculatePathLength(Vector3 toPosition) {
        NavMeshPath path = new NavMeshPath();

        pathfinder.CalculatePath(toPosition, path);

        Vector3[] allPoints = new Vector3[path.corners.Length + 2];

        allPoints[0] = transform.position;
        allPoints[allPoints.Length - 1] = toPosition;


        for (int i = 0; i < path.corners.Length; i++) {
            allPoints[i + 1] = path.corners[i];
        }

        float pathLength = 0f;

        for (int i = 0; i < allPoints.Length - 1; i++) {
            pathLength += Vector3.Distance(allPoints[i], allPoints[i + 1]);
        }

        return pathLength;
    }
    // Dynamic Variables // -------------------------

    void Start() {
        entity = GetComponent<Entity>();
        pathfinder = GetComponent<NavMeshAgent>();
        gunController = GetComponent<GunController>();
        unit = GetComponentInParent<Unit_FSM>();

        gameController = unit.gameController;
        eyes = entity.headBone;
        visibleToAI = gameController.visibleToAI;
        anomalyLocation = unit.resetLocation;
        previousAnomalyLocation = unit.resetLocation;
        expressionText.text = "";
        senseFringe = senseRadius - (senseRadius * 0.1f);
        shotTimer = Time.time;

        if (gunController != null) gunController.Initialize();
        AIStateEnded();

        if (scanRefreshRate > 0) {
            // Not in use yet //
        }
    }

    void Update() {
        SensoryScan();

        if (!busy) {
            entity.SetStanceAction(false, 0);

            if (hostileDetected) {
                DisplayEmote("!", Color.red);
            } else if (anomalyDetected) {
                DisplayEmote("!", Color.white);
            } else {
                Patrol();
            }

            hostileDetected = false;
            anomalyDetected = false;
        }

        Animate();
    }

    void SensoryScan() {
        if (currentState == AIStates.Alert) {
            anomalyDetected = true;
            if (previousAnomalyLocation != unit.hostileLocation) anomalyLocation = unit.hostileLocation;
            previousAnomalyLocation = unit.hostileLocation;
        }

        hostile = null;
        float distanceToOther;
        float closestDistance = Mathf.Infinity;
        Entity closestHostile = null;

        Entity otherEntity;
        Collider[] nearbyEntities = Physics.OverlapSphere(eyes.position, senseRadius, visibleToAI);

        for (int i = 0; i < nearbyEntities.Length; i++) {
            otherEntity = nearbyEntities[i].GetComponent<Entity>();

            if (isAlive(otherEntity) && otherEntity.isPlayer) {
                distanceToOther = Vector3.Distance(eyes.position, otherEntity.transform.position);

                if (canSee(otherEntity, distanceToOther) && closestDistance > distanceToOther) {
                    closestDistance = distanceToOther;
                    closestHostile = otherEntity;
                }

                if (closestHostile != null) {
                    if (currentState == AIStates.Intrusion && closestDistance > senseFringe) {
                        AnomalyDetected(closestHostile);
                    } else {
                        HostileDetected(closestHostile);
                    }
                }

                if (currentState == AIStates.Intrusion && canHear(otherEntity)) {
                    AnomalyDetected(otherEntity);
                }

                // Debug text in expressionText.
                if (unit.debugMode) {
                    Vector3 directionToOther = (otherEntity.transform.position - eyes.position).normalized;
                    distanceToOther = Vector3.Distance(eyes.position, otherEntity.transform.position);
                    expressionText.color = Color.white;
                    expressionText.text =
                        "State: " + currentState +
                        " In Range: " + (isAlive(otherEntity) && otherEntity.isPlayer) +
                        " In Field: " + EntityIsInFieldOfView(directionToOther) +
                        " In Cover: " + EntityIsBehindObject(directionToOther, distanceToOther);
                }
            }
        }
    }

    void HostileDetected(Entity other) {
        hostile = other;
        unit.hostile = hostile;

        unit.hostileLocation = hostile.transform.position;
        anomalyLocation = unit.hostileLocation;
        entity.targetCollisionRadius = hostile.collisionRadius;

        hostileDetected = true;

        if (gameController.alertAlreadyActive) {
            gameController.SetAIState((int)AIStates.Alert);
            gameController.ResetAIStatePercent();
        }
    }

    void AnomalyDetected(Entity other) {
        anomalyLocation = other.transform.position;
        entity.targetCollisionRadius = other.collisionRadius;

        anomalyDetected = true;
    }

    void Chase() {
        if (currentState == AIStates.Intrusion && pathfinder.speed != entity.walkSpeed) pathfinder.speed = entity.walkSpeed; 
        if (currentState != AIStates.Intrusion && pathfinder.speed != entity.runSpeed) pathfinder.speed = entity.runSpeed;

        Vector3 directionToAnomaly = (anomalyLocation - transform.position).normalized;
        Vector3 destination = anomalyLocation - directionToAnomaly * (entity.collisionRadius + entity.targetCollisionRadius + entity.meleeDistanceThreshold / 2);
        if (pathfinder.destination != destination) pathfinder.destination = destination;

        if (pathfinder.remainingDistance <= pathfinder.stoppingDistance) {
            timeWaited += Time.deltaTime;

            if (timeWaited >= timeToWait) {
                // Done waiting.
                if (!unit.debugMode && currentState == AIStates.Intrusion) DisplayEmote("?", Color.white);
                unit.hostileLocation = unit.resetLocation;
                anomalyLocation = unit.resetLocation;
                entity.targetCollisionRadius = 0;
                timeWaited = 0;
            }
        } else timeWaited = 0;
    }

    void Patrol() {
        if (pathfinder.speed != entity.walkSpeed) pathfinder.speed = entity.walkSpeed;
        if (waypoints != null && (pathfinder.destination == unit.resetLocation || pathfinder.remainingDistance <= pathfinder.stoppingDistance)) {
            // At destination.
            timeWaited += Time.deltaTime;
            if (timeWaited >= timeToWait) {
                // Done waiting.
                patrolIndex++;
                if (patrolIndex >= waypoints.Length) {
                    patrolIndex = 0;
                    if (!patrolIsCircuit) {
                        Array.Reverse(waypoints);
                        patrolIndex++;
                    }
                }
                timeWaited = 0;
            }
        } else timeWaited = 0;
        if (pathfinder.destination != waypoints[patrolIndex]) pathfinder.destination = waypoints[patrolIndex];
    }

    void Animate() {
        currentSpeed = pathfinder.velocity.magnitude;
        aSpeedPercent = ((pathfinder.speed == entity.runSpeed) ? currentSpeed / entity.runSpeed : currentSpeed / entity.walkSpeed * 0.5f) * 2;
        aVelocity = transform.InverseTransformDirection(pathfinder.velocity.normalized) * aSpeedPercent;
        entity.SetSpeed(aVelocity.z, aVelocity.x);
    }

    void LookAtLocation(Vector3 target, bool aimWhileLooking) {
        Vector3 targetLocation = new Vector3(target.x, transform.position.y, target.z);
        transform.LookAt(targetLocation);
        if (aimWhileLooking) entity.SetStanceAction(true, 1);
    }

    public void Die() {
        StopAllCoroutines();
        expressionText.text = "";
        if (cover != null) {
            cover.SetOccupant(null);
            cover = null;
        }
        pathfinder.destination = transform.position;
        this.enabled = false;
    }

    public void AssignPatrol(Transform newPatrol) {
        patrol = newPatrol;
        waypoints = new Vector3[patrol.childCount];
        for (int i = 0; i < waypoints.Length; i++) {
            waypoints[i] = patrol.GetChild(i).position;
        }

        patrolIndex = 0;
    }

    public void AIStateEnded() {
        currentState = gameController.currentState.stateName;

        if (currentState == AIStates.Caution || currentState == AIStates.Intrusion) {
            busy = false;
            if (cover != null) {
                cover.SetOccupant(null);
                cover = null;
            }
        }
    }

    void DisplayEmote(string expression, Color color) {
        if (expressionText == null || expression == null) return;
        StopCoroutine(Emote());
        expressionText.text = expression;
        expressionText.color = color;
        StartCoroutine(Emote());
    }

    IEnumerator Emote() {
        float displayDuration = 1.5f;
        float displayedTime = 0;

        if (expressionText.color == Color.red) displayDuration = 0.5f;

        busy = true;
        pathfinder.destination = transform.position;

        while (displayedTime < displayDuration) {
            displayedTime += Time.deltaTime;

            if (anomalyLocation != unit.resetLocation) LookAtLocation(anomalyLocation, false);
            yield return null;
        }

        expressionText.text = "";
        if (hostileDetected) StartCoroutine(EngageHostile());
        else if (anomalyDetected) StartCoroutine(InvestigateAnomaly());
        else busy = false;
    }

    IEnumerator InvestigateAnomaly() {
        if (currentState == AIStates.Intrusion) pathfinder.speed = entity.walkSpeed;
        else pathfinder.speed = entity.runSpeed;

        Vector3 directionToAnomaly = (anomalyLocation - transform.position).normalized;
        pathfinder.destination = anomalyLocation - directionToAnomaly * (entity.collisionRadius + entity.targetCollisionRadius + entity.meleeDistanceThreshold / 2);

        while (hostileDetected == false && transform.position != pathfinder.destination) {
            yield return null;
        }

        if (hostileDetected) {
            DisplayEmote("!", Color.red);
        } else {
            anomalyLocation = unit.resetLocation;
            entity.targetCollisionRadius = 0;
            anomalyDetected = false;
            DisplayEmote("?", Color.white);
        }
    }

    IEnumerator HoldPosition() {
        bool holding = true;

        while (holding) {
            LookAtLocation(transform.position + cover.transform.forward, true);
            if (hostile != null) holding = false;
            yield return null;
        }

        StartCoroutine(EngageHostile());
    }

    IEnumerator EngageHostile() {
        bool didShoot = false;
        float fireDuration = 0.5f;
        float fireTimer = 0;

        while (fireTimer < fireDuration) {
            fireTimer += Time.deltaTime;

            LookAtLocation(anomalyLocation, true);
            if (CanShoot(anomalyLocation)) {
                gunController.OnTriggerHold();
                gunController.OnTriggerRelease();
                didShoot = true;
            }
            yield return null;
        }

        if (didShoot) shotTimer = Time.time + entity.rangedCooldown;

        if (CoverStillGood() == false) StartCoroutine(GetToCover());
        else StartCoroutine(HoldPosition());
    }

    IEnumerator GetToCover() {

        Vector3 directionToCover = Vector3.zero;
        float coverDistance = Mathf.Infinity;
        if (cover != null) cover.SetOccupant(null);
        cover = null;

        Collider[] nearbyCover = Physics.OverlapSphere(transform.position, senseRadius, visibleToAI);
        Cover_FSM theCover;
        float closest = Mathf.Infinity;

        for (int i = 0; i < nearbyCover.Length; i++) {
            theCover = nearbyCover[i].GetComponent<Cover_FSM>();

            if (theCover != null && theCover.occupied == false) {
                coverDistance = Vector3.Distance(anomalyLocation, theCover.transform.position);
                directionToCover = ((theCover.transform.position + theCover.transform.up) - anomalyLocation).normalized;
                if (Physics.Raycast(anomalyLocation, directionToCover, coverDistance, gameController.obscuresVision)) {
                    if (coverClosestToEnemy == false) coverDistance = Vector3.Distance(transform.position, theCover.transform.position);
                    if (closest > coverDistance) {
                        closest = coverDistance;
                        cover = theCover;
                    }
                }
            }
        }

        if (cover != null) {
            cover.SetOccupant(entity);
            pathfinder.speed = entity.runSpeed;
            pathfinder.destination = cover.transform.position;
        }

        while (cover != null && transform.position != pathfinder.destination) {
            yield return null;
        }

        if (gameController.alertAlreadyActive) StartCoroutine(EngageHostile());
        else StartCoroutine(RadioHQ("I see an intruder! Requesting backup!", true, AIStates.Alert));
    }

    IEnumerator RadioHQ(string reportToMake, bool isStateRequest, AIStates stateToRequest) {
        bool grabStart = true;
        bool grabUpdate = false;
        float grabDuration = 0.333f;
        float grabTimer = 0;

        bool talkStart = false;
        bool talkUpdate = false;
        float talkDuration = 3;
        float talkTimer = 0;

        bool releaseStart = false;
        bool releaseUpdate = false;
        float releaseDuration = 0.333f;
        float releaseTimer = 0;

        bool usingRadio = true;
        while (usingRadio) {

            LookAtLocation(anomalyLocation, false);

            if (grabStart) {
                busy = true;
                pathfinder.destination = transform.position;
                entity.SetStanceAction(false, -1);
                grabUpdate = true;
                grabStart = false;
            }

            if (grabUpdate) {
                grabTimer += Time.deltaTime;
                if (grabTimer >= grabDuration) talkStart = true;
            }

            if (talkStart) {
                gameController.RadioStart(reportToMake);
                talkUpdate = true;
                talkStart = false;
            }

            if (talkUpdate) {
                talkTimer += Time.deltaTime;
                if (talkTimer >= talkDuration) releaseStart = true;
            }

            if (releaseStart) {
                gameController.RadioEnd(isStateRequest, stateToRequest);
                entity.SetStanceAction(false, 0);
                releaseUpdate = true;
                releaseStart = false;
            }

            if (releaseUpdate) {
                releaseTimer += Time.deltaTime;
                if (releaseTimer >= releaseDuration) usingRadio = false;
            }
            yield return null;
        }

        if (gameController.alertAlreadyActive) StartCoroutine(HoldPosition());
    }
}
