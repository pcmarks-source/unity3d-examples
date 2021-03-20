using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEngine.Networking;

public enum Plan_Weighted { Null, Patrol, Chase, Evade }

public class Brain_Weighted : NetworkBehaviour {

    // Self Assigned //
    GameController gameController;
    public Entity entity { get; protected set; }
    NavMeshAgent pathfinder;
    GunController gunController;

    [Header("Traits")]
    [SerializeField] float desiredCombatDistance = 10;

    [Header("Senses")]
    [Range(0, 50)] [SerializeField] float senseRadius = 20;
    [Range(0, 360)] [SerializeField] float fieldOfView = 90;
    [SerializeField] [Range(0, 1)] float scanRefreshRate = 0.2f;
    [Space(12)] // This can be cleaned up by automating looksFor/Obscures and thinking of a better way to handle factions.
    [SerializeField] LayerMask looksFor;
    [SerializeField] LayerMask obscuresVision;
    [SerializeField] List<EntityType> hostileTo;
    [SerializeField] List<EntityType> friendlyTo;

    List<Vector3> thingsToInvestigate;
    List<Entity> visibleEntities;
    List<Entity> visibleLKPs;

    Dictionary<Entity, EntityMemory_Weighted> threatMemory;
    Dictionary<Entity, EntityMemory_Weighted> friendMemory;
    Dictionary<Entity, LastKnownPosition_Weighted> lastKnownPositions;
    List<Node> coverLocations;

    [Header("Patrol")]
    public bool patrolIsCircuit = true;
    [Space(12)]
    [HideInInspector] public Transform patrol;
    Vector3[] waypoints;
    int patrolIndex;
    [SerializeField] float waitTime = 4;
    float timeWaited;
    [SerializeField] [Range(0, 1)] float pathRefreshRate = 0.2f;

    [Header("Flavor")]
    [SerializeField] Text expressionText;
    float morale;

    // Decision Making //
    Plan_Weighted currentPlan;
    EntityMemory_Weighted target;
    EntityMemory_Weighted friend;
    Vector3 orientation;
    Vector3 destination;
    float speed;
    Dictionary<float, Vector3> orientationPlans;
    List<DestinationPlan_Weighted> destinationPlans;
    Dictionary<float, float> speedPlans;

    // Group Coordination //

    [HideInInspector] public bool partOfUnit = false;
    [HideInInspector] public Unit_Weighted unit = null;
    float selfConfidence = 8;

    // Animation & Speed //
    bool ads = false;
    bool canShoot = false;
    bool canLook = false;
    bool canMove = true;

    // Melee //
    float collisionRadius;
    float targetCollisionRadius;
    float meleeTimer;

    // Dynamic Variables // -------------------------
    bool heardSomethingFrom(Entity originOfSound) {
        if (originOfSound.velocity.magnitude - 0.1f < originOfSound.walkSpeed) return false;
        if (CalculatePathLength(originOfSound.transform.position) > senseRadius) return false;
        return true;
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
        expressionText.text = "";
        if (!isServer) Destroy(this);

        gameController = GameObject.FindWithTag("GameController").GetComponent<GameController>();

        entity = GetComponent<Entity>();
        pathfinder = GetComponent<NavMeshAgent>();
        destination = transform.position;
        gunController = GetComponent<GunController>();

        thingsToInvestigate = new List<Vector3>();
        visibleEntities = new List<Entity>();
        visibleLKPs = new List<Entity>();

        threatMemory = new Dictionary<Entity, EntityMemory_Weighted>();
        friendMemory = new Dictionary<Entity, EntityMemory_Weighted>();
        lastKnownPositions = new Dictionary<Entity, LastKnownPosition_Weighted>();
        orientationPlans = new Dictionary<float, Vector3>();
        destinationPlans = new List<DestinationPlan_Weighted>();
        speedPlans = new Dictionary<float, float>();
        coverLocations = gameController.coverLocations;

        collisionRadius = GetComponent<CapsuleCollider>().radius;
        if (desiredCombatDistance <= 0) desiredCombatDistance = 0.01f; // Sanity feature.

        unit = GetComponentInParent<Unit_Weighted>();
        if (unit != null) {
            unit.JoinUnit(this);
        } else {
            StartCoroutine(ScanTimer());
        }

        if (gunController != null) gunController.Initialize();

        StartCoroutine(UpdatePath());
    }

    void Update() {
        Animate();

        expressionText.text = "Plan: " + currentPlan.ToString() + " canLook: " + canLook;
    }

    public void AssignPatrol(Transform pathHolder) {
        patrol = pathHolder;
        waypoints = new Vector3[pathHolder.childCount];
        for (int i = 0; i < waypoints.Length; i++) {
            waypoints[i] = pathHolder.GetChild(i).position;
        }

        patrolIndex = 0;
        speed = entity.walkSpeed;
        destination = waypoints[patrolIndex];
    }

    public void AssignInvestigation(Entity other, Entity othersTarget, Vector3 othersPosition, Vector3 othersHeading, float othersSpeed, float othersCollisionRadius) {
        EntityMemory_Weighted newMemory = new EntityMemory_Weighted(other, othersTarget, othersPosition, othersHeading, othersSpeed, othersCollisionRadius);
        RememberEntity(other, newMemory);
    }

    public void SetExpression(string expression, Color color) {
        if (expressionText == null || expression == null) return;
        StopCoroutine(Emote());
        expressionText.text = expression;
        expressionText.color = color;
        StartCoroutine(Emote());
    }

    void Animate() {
        if (canLook) transform.LookAt(orientation);

        if (gunController != null) {
            if (ads) entity.SetStance(true, gunController.equippedGun.weaponID + 1);
            else entity.SetStance(false, gunController.equippedGun.weaponID);
        }

        Vector3 aVelocity = transform.InverseTransformDirection(pathfinder.velocity.normalized) * pathfinder.velocity.magnitude;
        entity.SetSpeed(aVelocity.z, aVelocity.x);
    }

    public void Die() {
        StopAllCoroutines();
        if (partOfUnit) unit.LeaveUnit(this, true);
        expressionText.text = "";
        pathfinder.enabled = false;

        foreach (KeyValuePair<Entity, LastKnownPosition_Weighted> lkp in lastKnownPositions) {
            Destroy(lkp.Value.gameObject);
        }

        this.enabled = false;
    }

    public void SensoryScan() {

        // This uses senseRadius and fieldOfView to determine if the AI can see, hear or smell anything that is included
        // in 'looksFor'. It then sends the things it has sensed to SensoryEvaluation to decide what's a threat, what's a friend
        // and what in general to remember or think about.

        visibleEntities.Clear();
        visibleLKPs.Clear();

        Collider[] nearByThings = Physics.OverlapSphere(transform.position, senseRadius, looksFor);
        Transform thing;
        Entity theThing;
        LastKnownPosition_Weighted theLKP;
        Vector3 dirToThing;
        bool entityIsAlive;
        bool memoryIsMine;


        for (int i = 0; i < nearByThings.Length; i++) {
            thing = nearByThings[i].transform;
            theThing = thing.GetComponent<Entity>();
            theLKP = thing.GetComponent<LastKnownPosition_Weighted>();
            dirToThing = (thing.position - transform.position).normalized;
            entityIsAlive = (theThing != null && !theThing.dead && !visibleEntities.Contains(theThing));
            memoryIsMine = (theLKP != null && !visibleLKPs.Contains(theLKP.isOf) && lastKnownPositions.ContainsKey(theLKP.isOf));

            // Vision.
            if (entityIsAlive || memoryIsMine) {
                if (Vector3.Angle(transform.forward, dirToThing) < fieldOfView / 2) {
                    float distanceToThing = Vector3.Distance(transform.position, thing.position);
                    if (!Physics.Raycast(transform.position + (Vector3.up * 1.65f), dirToThing, distanceToThing, obscuresVision)) {
                        // The AI sees an entity.
                        if (entityIsAlive) {
                            visibleEntities.Add(theThing);
                        }

                        // The AI remembers an entity being here.
                        if (memoryIsMine) {
                            visibleLKPs.Add(theLKP.isOf);
                        }
                    }
                }
            }

            // Hearing.
            if (entityIsAlive && heardSomethingFrom(theThing)) {
                // The AI hears an entity.
                visibleEntities.Add(theThing); // thingsToInvestigate.Add(thing.transform.position); // Might be useful but it's cluttery. 
            }
        }

        if (!partOfUnit) {
            FormEntityMemories(visibleEntities);
            JudgeEntities(visibleLKPs);
        } else {
            visibleEntities.Add(entity);
            unit.ReportEntities(visibleEntities);
            unit.ReportLPKs(visibleLKPs);
        }
    }

    void LKPCleanup(List<Entity> lkpIntel) {
        Entity isOf;
        for (int i = 0; i < lkpIntel.Count; i++) {
            isOf = lkpIntel[i];
            if (lastKnownPositions.ContainsKey(isOf)) {
                if (isOf.dead || isOf.transform.position != lastKnownPositions[isOf].transform.position) {
                    if (threatMemory.ContainsKey(isOf)) threatMemory.Remove(isOf);
                    if (friendMemory.ContainsKey(isOf)) friendMemory.Remove(isOf);
                    Destroy(lastKnownPositions[isOf].gameObject);
                    lastKnownPositions.Remove(isOf);
                }
            }
        }
    }

    public void FormEntityMemories(List<Entity> entityIntel) {
        Entity other;
        Entity othersTarget;
        Vector3 othersPosition;
        Vector3 othersHeading;
        float othersSpeed;
        float othersCollisionRadius;

        for (int i = 0; i < entityIntel.Count; i++) {
            other = entityIntel[i];
            if (other != entity) {
                othersTarget = other.target;
                othersPosition = other.transform.position;
                othersHeading = other.velocity;
                othersSpeed = othersHeading.magnitude;
                othersCollisionRadius = other.GetComponent<CapsuleCollider>().radius;

                // Remembering the thing, or not.
                EntityMemory_Weighted newMemory = new EntityMemory_Weighted(other, othersTarget, othersPosition, othersHeading, othersSpeed, othersCollisionRadius);
                RememberEntity(other, newMemory);
            }
        }
    }

    void RememberEntity(Entity thing, EntityMemory_Weighted memory) {

        // This simply determines who the memory is about and if it's worth keeping.
        // It then tells the brain to think about it's important memories and decide what to focus on.

        if (thing == null || memory == null || friendMemory == null || threatMemory == null) return;

        if (friendlyTo.Contains(thing.entityType)) {
            if (friendMemory.ContainsKey(thing)) friendMemory.Remove(thing);
            friendMemory.Add(thing, memory);
        }

        if (hostileTo.Contains(thing.entityType)) {
            if (threatMemory.ContainsKey(thing)) threatMemory.Remove(thing);
            threatMemory.Add(thing, memory);
        }
    }

    float EvaluateMemory(EntityMemory_Weighted memory) {
        // Turning the entity's type into a base threat. More dangerous thing should have higher threat.
        float baseThreat = 10;
        if (memory.subject.entityType == EntityType.Plant || memory.subject.entityType == EntityType.Prey || memory.subject.entityType == EntityType.Civilian) baseThreat = 20;
        if (memory.subject.entityType == EntityType.Predator || memory.subject.entityType == EntityType.Shambler || memory.subject.entityType == EntityType.Soldier) baseThreat = 30;

        // Turning the entity's target into a threat rating to myself and my friends.
        float threatToMe = 10;
        float threatToFriend = 10;
        if (memory.target != null) {
            if (memory.target.entityType == entity.entityType) { threatToMe = 20; threatToFriend = 30; }
            if (memory.target == entity) { threatToMe = 30; threatToFriend = 20; }
        }

        // Taking the calculated threat ratings and multiplying them against distance and speed to get final threat weight.
        float distancePlusSpeed = Vector3.Distance(transform.position, memory.position) / memory.speed + 1;
        float memoryWeight = (baseThreat + threatToMe + threatToFriend * 10) / (distancePlusSpeed * distancePlusSpeed);

        // Updating the last known position of the memory.
        if (lastKnownPositions.ContainsKey(memory.subject)) {
            lastKnownPositions[memory.subject].transform.position = memory.position;
        } else {
            GameObject lkpObject = Instantiate(entity.lastKnownPosition, memory.position, Quaternion.Euler(memory.heading)) as GameObject;
            lkpObject.transform.SetParent(unit.gameObject.transform);
            LastKnownPosition_Weighted lkp = lkpObject.GetComponent<LastKnownPosition_Weighted>();
            lkp.SetSubject(memory.subject);
            lastKnownPositions.Add(memory.subject, lkp);
        }

        return memoryWeight;
    }

    public void JudgeEntities(List<Entity> lkpIntel) {

        // This sifts through memories of entities the AI has seen and decides what entities to focus on.
        // This is the end of the line for sensory input. All other actions are based on what is done in this chain.

        // This also calculates threat and morale, and posts it to the expression text.
        // Morale goes up when there are more and better friends around.
        // Morale goes down when there are more and better threats around.
        // The importance of a threat goes up the closer it is especially if it's focused on this AI.

        morale = selfConfidence;

        EntityMemory_Weighted heaviestTarget = null;
        EntityMemory_Weighted heaviestFriend = null;
        float heaviestWeight = 0;
        float memoryWeight = 0;

        entity.SetTarget(null);
            targetCollisionRadius = 0;

        if (threatMemory != null) {
            heaviestTarget = null;
            heaviestWeight = 0;
            foreach (KeyValuePair<Entity, EntityMemory_Weighted> memory in threatMemory) {
                memoryWeight = EvaluateMemory(memory.Value);
                if (memoryWeight > heaviestWeight) {
                    heaviestWeight = memoryWeight;
                    heaviestTarget = memory.Value;
                }
                morale -= memoryWeight;
            }
        }

        if (friendMemory != null) {
            heaviestFriend = null;
            heaviestWeight = 0;
            foreach (KeyValuePair<Entity, EntityMemory_Weighted> memory in friendMemory) {
                memoryWeight = EvaluateMemory(memory.Value);
                if (memoryWeight > heaviestWeight) {
                    heaviestWeight = memoryWeight;
                    heaviestFriend = memory.Value;
                }
                morale += memoryWeight;
            }
        }

        LKPCleanup(visibleLKPs);

        target = heaviestTarget;
        friend = heaviestFriend;

        if (target != null) {
            entity.SetTarget(target.subject);
            targetCollisionRadius = target.collisionRadius;
        } else {
            entity.SetTarget(null);
            targetCollisionRadius = 0;
        }

        Locomotion();
    }

    void Locomotion() {
        // Initialize.
        orientationPlans.Clear();
        destinationPlans.Clear();
        speedPlans.Clear();
        float heaviestWeight = 0;
        float planWeight = 0;

        FormPlans();

        // What should I be looking at?
        if (orientationPlans != null) {
            heaviestWeight = 0;
            foreach (KeyValuePair<float, Vector3> orientationPlan in orientationPlans) {
                planWeight = orientationPlan.Key;
                if (planWeight > heaviestWeight) {
                    heaviestWeight = planWeight;
                    orientation = orientationPlan.Value;
                }
            }
        }

        // Where should I be moving to?
        if (destinationPlans != null) {
            heaviestWeight = 0;
            foreach (DestinationPlan_Weighted destinationPlan in destinationPlans) {
                planWeight = destinationPlan.weight;
                if (planWeight > heaviestWeight) {
                    heaviestWeight = planWeight;
                    destination = destinationPlan.destination;
                    currentPlan = destinationPlan.plan;
                }
            }
        }

        // How fast should I be moving?
        if (speedPlans != null) {
            heaviestWeight = 0;
            foreach (KeyValuePair<float, float> speedPlan in speedPlans) {
                planWeight = speedPlan.Key;
                if (planWeight > heaviestWeight) {
                    heaviestWeight = planWeight;
                    speed = speedPlan.Value;
                    if (ads) speed = Mathf.Clamp(speed, 0, entity.walkSpeed);
                }
            }
        }
        // Finalize.
        if (currentPlan == Plan_Weighted.Null || currentPlan == Plan_Weighted.Patrol) pathfinder.autoBraking = true;
        else pathfinder.autoBraking = false;

        if (orientationPlans.Count > 0) canLook = true;
        else canLook = false;

        entity.SetVelocity(pathfinder.velocity);
        Combat();
    }

    void FormPlans() {
        // Initialize.
        Vector3 plannedOrientation = Vector3.zero;
        Vector3 plannedDestination = Vector3.zero;
        float weight = 0;
        ads = false;
        canShoot = false;

        if (target != null) {
            Vector3 targetPosition = target.position;
            float targetDistance = Vector3.Distance(targetPosition, transform.position);
            Vector3 directionToTarget = (targetPosition - transform.position).normalized;

            // Plan to not move at all.
            Plan_DontMove(plannedDestination, weight, targetDistance);

            // Plan to chase down a target.
            Plan_Chase(plannedDestination, weight, targetPosition, directionToTarget, targetDistance);

            // Plan to run away from a target.
            Plan_Evade(plannedDestination, weight, targetPosition, directionToTarget, targetDistance);

            // AI should look at its target.
            Plan_LookAtTarget(plannedOrientation, weight, targetPosition, targetDistance);

            // AI should aim its weapon at the target.
            Plan_AimDownSight();

            // AI should fire it's weapon.
            Plan_FireWeapon(gameController.collisionMask);

            // AI should melee its target.
            Plan_MeleeAttack(targetDistance);
        }

        if (target == null) {
            // Plan to casually patrol.
            Plan_Patrol();
        }
    }

    void Plan_Patrol() {
        if (waypoints != null && pathfinder.remainingDistance <= pathfinder.stoppingDistance) {
            timeWaited += Time.deltaTime;
            if (timeWaited >= waitTime && pathfinder.remainingDistance <= pathfinder.stoppingDistance) {
                patrolIndex++;
                if (patrolIndex >= waypoints.Length) {
                    patrolIndex = 0;
                    if (!patrolIsCircuit) {
                        patrolIndex++;
                        Array.Reverse(waypoints);
                    }
                }
                DestinationPlan_Weighted destinationPlan = new DestinationPlan_Weighted(Plan_Weighted.Patrol, waypoints[patrolIndex], 1);
                destinationPlans.Add(destinationPlan);
                speedPlans.Add(1, entity.walkSpeed);
                timeWaited = 0;
            }
        } else {
            DestinationPlan_Weighted destinationPlan = new DestinationPlan_Weighted(Plan_Weighted.Patrol, waypoints[patrolIndex], 1);
            destinationPlans.Add(destinationPlan);
            speedPlans.Add(1, entity.walkSpeed);
            timeWaited = 0;
        }
    }

    void Plan_DontMove(Vector3 plannedDestination, float weight, float targetDistance) {
        plannedDestination = transform.position;
        weight = -1;
        if (visibleLKPs.Contains(target.subject) && targetDistance >= desiredCombatDistance / 2) weight = desiredCombatDistance + (desiredCombatDistance / 2);

        DestinationPlan_Weighted destinationPlan = new DestinationPlan_Weighted(Plan_Weighted.Null, plannedDestination, weight);
        destinationPlans.Add(destinationPlan);
        speedPlans.Add(weight, entity.walkSpeed);
    }

    void Plan_Chase(Vector3 plannedDestination, float weight, Vector3 targetPosition, Vector3 directionToTarget, float targetDistance) {
        plannedDestination = targetPosition - directionToTarget * (collisionRadius + targetCollisionRadius + entity.meleeDistanceThreshold / 2);

        weight = targetDistance;

        DestinationPlan_Weighted destinationPlan = new DestinationPlan_Weighted(Plan_Weighted.Chase, plannedDestination, weight);
        destinationPlans.Add(destinationPlan);
        speedPlans.Add(weight, entity.runSpeed);
    }

    void Plan_Evade(Vector3 plannedDestination, float weight, Vector3 targetPosition, Vector3 directionToTarget, float targetDistance) {
        Vector3 directionToSafety = directionToTarget * -1;
        plannedDestination = targetPosition + (directionToSafety * UnityEngine.Random.Range(5,15));

        weight = desiredCombatDistance / (targetDistance / desiredCombatDistance);

        DestinationPlan_Weighted destinationPlan = new DestinationPlan_Weighted(Plan_Weighted.Evade, plannedDestination, weight);
        destinationPlans.Add(destinationPlan);
        speedPlans.Add(weight, entity.runSpeed);
    }

    void Plan_LookAtTarget(Vector3 plannedOrientation, float weight, Vector3 targetPosition, float targetDistance) {
        if (desiredCombatDistance <= 0.1f) return;

        plannedOrientation = new Vector3(targetPosition.x, 0, targetPosition.z);
        weight = -1;
        if (targetDistance >= desiredCombatDistance / 2) weight = desiredCombatDistance + (desiredCombatDistance / 2);

        orientationPlans.Add(weight, plannedOrientation);
    }

    void Plan_AimDownSight() {
        if (visibleLKPs.Contains(target.subject)) ads = true;
    }

    void Plan_FireWeapon(LayerMask collisionMask) {
        if (gunController == null || ads == false) return;
        Ray ray = new Ray(gunController.equippedGun.projectileSpawn[0].position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, senseRadius - (senseRadius / 10), collisionMask, QueryTriggerInteraction.Collide)) {
            Entity hitThing = hit.collider.GetComponent<Entity>();
            if (hitThing != null) {
                foreach (EntityType t in hostileTo) {
                    if (hitThing.entityType == t) {
                        canShoot = true;
                        break;
                    }
                }
            }
        }
    }

    void Plan_MeleeAttack(float targetDistance) {
        if (target != null && Time.time > meleeTimer) {
            if (targetDistance < entity.meleeDistanceThreshold + collisionRadius + targetCollisionRadius) {
                meleeTimer = Time.time + entity.meleeCooldown;
                weight = weightMultiplier / targetDistance * targetDistance;

                combatPlans.Add(weight, combatPlan);
                StartCoroutine(Melee(target.subject));
            }
        }
    }

    void Combat() {
        if (canShoot && gunController != null) {
            gunController.OnTriggerRelease();
            gunController.OnTriggerHold();
        }
    }

    public IEnumerator ScanTimer() {
        while (true) {
            yield return new WaitForSeconds(scanRefreshRate);
            SensoryScan();
        }
    }

    IEnumerator UpdatePath() {
        while (true) {
            yield return new WaitForSeconds(pathRefreshRate);
            if (canMove) {
                if (pathfinder.speed != speed) pathfinder.speed = speed;
                if (pathfinder.destination != destination) pathfinder.SetDestination(destination);
            }
        }
    }

    IEnumerator Emote() {
        float speed = 3;
        float percent = 0;

        while (percent <= 1) {
            percent += Time.deltaTime * speed;
            yield return null;
        }
        expressionText.text = "";
    }

    IEnumerator Melee(Entity victim) {
        canMove = false;
        destination = transform.position;
        pathfinder.SetDestination(transform.position);

        float attackSpeed = 3;
        float percent = 0;

        bool hasAppliedDamage = false;

        if (entity.entityType == EntityType.Shambler) entity.TriggerLunge();

        while (percent <= 1) {

            if (percent >= 0.5f && !hasAppliedDamage) {
                hasAppliedDamage = true;
                victim.TakeDamage(entity.meleeDamage, victim.transform.position + Vector3.up * 1.65f, Vector3.forward);
            }

            percent += Time.deltaTime * attackSpeed;

            yield return null;
        }

        canMove = true;
    }

}
