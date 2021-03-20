using System.Collections.Generic;
using UnityEngine;

public class ZombieController : EntityController {

    private const float msBetweenSensoryCycles = 250f / 1000f;
    private float nextSensoryCycle;

    private const float msBetweenAttackCycles = 4000f / 1000f;
    private float nextAttackCycle;

    private AIState aiState;

    private float alertDuration = 10f;
    private float evasionDuration = 60f;
    private float cautionDuration = 300f;
    private float investigationDuration = 10f;
    private float nextAIStateTime;

    private Vector3 enemyLastKnownPosition = Metrics.nullVector3;
    private EntityData enemy = null;
    private List<EntityData> enemies = new List<EntityData>();

    public override void Initialize(int ownerUniqueID) {
        base.Initialize(ownerUniqueID);

        input.reloadRequest = false;
        state.aimTime = 0.25f;
        state.rotationTime = 0.25f;

        attributes.healthMaximum = 100;

        attributes.senseRadius = 20f;
        attributes.fieldOfView = 160f;

        attributes.speedStandingSlow = 2;
        attributes.speedStandingFast = 10;

        attributes.speedAiming = 10;
    }

    public override void FixedInstructionCycle() {
        if (Metrics.testMode == true) CollisionAvoidance();
        base.FixedInstructionCycle();
    }

    public override void InstructionCycle() {
        if (Metrics.testMode == true) {
            SensoryScan();
            AIStateMachine();

            Alert();
            Evasion();
            Caution();
            Investigation();
            Normal();
        }

        base.InstructionCycle();
    }

    public override void LateInstructionCycle() {
        base.LateInstructionCycle();
    }

    private void SensoryScan() {
        if (aiState == AIState.Alert) return;
        if (Metrics.time < nextSensoryCycle) return;
        nextSensoryCycle = Metrics.time + msBetweenSensoryCycles;

        enemy = null;
        enemies.Clear();
        Collider[] possibleEnemies = Physics.OverlapSphere(rigidbody.position, attributes.senseRadius, Metrics.entityMask);
        if (possibleEnemies.Length <= 0) return;

        EntityData possibleEnemy;
        Vector3 direction;
        float distance;

        for (int i = 0; i < possibleEnemies.Length; i++) {

            possibleEnemy = (EntityData)GridData.GetDamageable(possibleEnemies[i].gameObject);

            if (possibleEnemy == null) continue;
            if (possibleEnemy.instructionSetID != PStrings.playerSpawn) continue;

            direction = (possibleEnemy.gameObject.transform.position - rigidbody.position).normalized;

            if (Vector3.Angle(rigidbody.transform.forward, direction) < attributes.fieldOfView * 0.5f) {

                distance = Vector3.Distance(rigidbody.position, possibleEnemy.gameObject.transform.position);

                if (Physics.Raycast(rigidbody.position + (Vector3.up * 1.5f), direction, distance, Statics.occlusionMask) == false) {
                    enemies.Add(possibleEnemy);
                }
            }
        }

        if (enemies.Count > 0) SetTarget(enemies[0]);
    }

    private void AIStateMachine() {
        if (Metrics.time < nextAIStateTime) return;

        if (aiState == AIState.Normal) {



        } else if (aiState == AIState.Alert) {

            enemyLastKnownPosition = enemy.gameObject.transform.position;
            enemy = null;

            aiState = AIState.Evasion;
            nextAIStateTime = Metrics.time + evasionDuration;

        } else if (aiState == AIState.Evasion) {

            enemyLastKnownPosition = Metrics.nullVector3;

            aiState = AIState.Caution;
            nextAIStateTime = Metrics.time + cautionDuration;

        } else if (aiState == AIState.Caution) {

            aiState = AIState.Normal;
            nextAIStateTime = Metrics.time;

        }
    }

    private void Alert() {
        if (aiState != AIState.Alert) return;

        pathfindManager.SetDestination(Coordinates.FromWorldSpace(enemy.gameObject.transform.position));

        input.aimRequest = true;
        input.attackStartRequest = CanAttack();
        input.attackStopRequest = CanAttack();

        state.pace = EntityPace.Jogging;

        input.lookPosition = (pathfindManager.atDestination) ? enemy.gameObject.transform.position : Coordinates.ToWorldSpaceFlat(pathfindManager.intermediary);
        input.aimPosition = input.lookPosition;

        input.moveDirection = (pathfindManager.atDestination == true) ? Vector3.zero : pathfindManager.directionToIntermediary;
        input.moveMagnitude = input.moveDirection.magnitude;

        input.lookDirection = (input.aimRequest) ? input.lookPosition - rigidbody.position : (input.moveDirection != Vector3.zero) ? input.moveDirection : input.lookDirection;
    }

    private void Evasion() {
        if (aiState != AIState.Evasion) return;

        pathfindManager.SetDestination(enemyLastKnownPosition);

        input.aimRequest = false;
        input.attackStartRequest = false;
        input.attackStopRequest = false;

        state.pace = EntityPace.Walking;

        input.lookPosition = (pathfindManager.atDestination) ? enemyLastKnownPosition : Coordinates.ToWorldSpaceFlat(pathfindManager.intermediary);
        input.aimPosition = enemyLastKnownPosition;

        input.moveDirection = (pathfindManager.atDestination == true) ? Vector3.zero : pathfindManager.directionToIntermediary;
        input.moveMagnitude = input.moveDirection.magnitude;

        input.lookDirection = (input.aimRequest) ? input.lookPosition - rigidbody.position : (input.moveDirection != Vector3.zero) ? input.moveDirection : input.lookDirection;
    }

    private void Caution() {
        if (aiState != AIState.Caution) return;

        pathfindManager.SetDestination(waypointManager.currentWaypoint.cellData.coordinates);

        input.aimRequest = false;
        input.attackStartRequest = false;
        input.attackStopRequest = false;

        state.pace = EntityPace.Walking;

        input.lookPosition = Coordinates.ToWorldSpaceFlat(pathfindManager.intermediary);
        input.aimPosition = input.lookPosition;

        input.moveDirection = (pathfindManager.atDestination == true) ? Vector3.zero : pathfindManager.directionToIntermediary;
        input.moveMagnitude = input.moveDirection.magnitude;

        input.lookDirection = (input.aimRequest) ? input.lookPosition - rigidbody.position : (input.moveDirection != Vector3.zero) ? input.moveDirection : input.lookDirection;
    }

    private void Investigation() {
        if (aiState != AIState.Investigation) return;
        Debug.Log("There is no code and this state should never be reached.");
    }

    private void Normal() {
        if (aiState != AIState.Normal) return;

        pathfindManager.SetDestination(waypointManager.currentWaypoint.cellData.coordinates);

        input.aimRequest = false;
        input.attackStartRequest = false;
        input.attackStopRequest = false;

        state.pace = EntityPace.Walking;

        input.lookPosition = Coordinates.ToWorldSpaceFlat(pathfindManager.intermediary);
        input.aimPosition = input.lookPosition;

        input.moveDirection = (pathfindManager.atDestination == true) ? Vector3.zero : pathfindManager.directionToIntermediary;
        input.moveMagnitude = input.moveDirection.magnitude;

        input.lookDirection = (input.aimRequest) ? input.lookPosition - rigidbody.position : (input.moveDirection != Vector3.zero) ? input.moveDirection : input.lookDirection;
    }

    private void CollisionAvoidance() {
        Ray ray = new Ray(rigidbody.position + Vector3.up, input.moveDirection);
        float distance = 1f;

        if (Physics.Raycast(ray, distance, Statics.occlusionMask) == true) {
            state.velocityCurrent = Vector3.zero;
            state.velocityTarget = Vector3.zero;
        }
    }

    private bool CanAttack() {
        if (Metrics.time < nextAttackCycle) return false;
        nextAttackCycle = Metrics.time + msBetweenAttackCycles;
        return true;
    }

    public void SetTarget(EntityData target) {
        if (target != null && target.instructionSetID != entityData.instructionSetID) {
            enemy = target;

            aiState = AIState.Alert;
            nextAIStateTime = Metrics.time + alertDuration;
        } else {
            enemyLastKnownPosition = enemy.gameObject.transform.position;
            enemy = null;

            aiState = AIState.Evasion;
            nextAIStateTime = Metrics.time + evasionDuration;
        }
    }
}
