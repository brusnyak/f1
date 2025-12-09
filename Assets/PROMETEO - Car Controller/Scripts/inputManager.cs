using UnityEngine;
using System.Collections.Generic;

public class inputManager : MonoBehaviour
{
    public enum driver
    {
        AI, keyboard
    }
    [SerializeField] public driver driveController;
    public TrackWaypoints waypoints;
    public PrometeoCarController carController;

    [HideInInspector] public float vertical;
    [HideInInspector] public float horizontal;
    [HideInInspector] public bool handbrake;
    [HideInInspector] public bool boosting;

    [Header("AI Settings")]
    public float aiTargetSpeed = 150f;
    public float aiSteerSensitivity = 0.03f;
    public float aiBrakeSensitivity = 0.8f;
    public Transform currentWaypoint;
    public List<Transform> nodes = new List<Transform>();
    [Range(0, 10)] public int aiWaypointLookAheadIndex = 4;
    [Range(0, 5)] public float steerForce = 2.5f;

    [Header("AI Braking Settings")]
    public float aiBrakingLookAheadDistance = 20f;
    public float aiSharpTurnAngleThreshold = 70f;
    public float aiAggressiveBrakeAngleThreshold = 100f;
    public float aiBrakeStrength = 0.7f;
    public float aiHandbrakeStrength = 1f;

    [Header("AI Collision Recovery")]
    public float stuckTimeThreshold = 2.0f; // Increased from 1.2f to prevent false triggers
    public float recoveryReverseTime = 2.0f;
    public float minSpeedThreshold = 5f; // Lowered to detect stuck better
    public float wrongDirectionAngleThreshold = 120f;
    public float clearanceCheckDistance = 8f; // Check if path behind is clear

    [Header("AI Enhanced Obstacle Avoidance")]
    public float avoidanceDistance = 20f; // Increased look-ahead
    public float avoidanceStrength = 1.2f; // Stronger avoidance
    public float sideRayOffset = 2.0f; // How far to sides
    public float sideRayAngle = 25f; // Angle of side rays
    public int numberOfRays = 5; // More rays for better detection
    public float wallAvoidanceSpeedReduction = 0.6f; // Slow down near walls

    private float stuckTimer;
    private bool isRecovering;
    private float recoveryTimer;
    private Vector3 lastPosition;
    private bool raceStarted = false;
    private float lastCollisionTime = -999f;
    private float lastForwardProgress = 0f;
    private int stuckFrameCounter = 0; // Count frames being stuck

    // Recovery state machine
    private enum RecoveryPhase { Reversing, Reorienting, Complete }
    private RecoveryPhase currentRecoveryPhase;
    private Vector3 recoveryTargetDirection;

    private void Awake()
    {
        carController = GetComponent<PrometeoCarController>();
        lastPosition = transform.position;

        GameObject waypointsObject = GameObject.FindGameObjectWithTag("trackLayer");
        if (waypointsObject != null)
        {
            waypoints = waypointsObject.GetComponent<TrackWaypoints>();
            if (waypoints != null)
            {
                nodes = waypoints.nodes;
            }
            else
            {
                Debug.LogError("TrackWaypoints component not found on GameObject with tag 'trackLayer'.");
            }
        }
        else
        {
            Debug.LogError("GameObject with tag 'trackLayer' not found.");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (driveController == driver.AI && raceStarted)
        {
            int hitLayer = collision.gameObject.layer;
            bool hitObstacle = hitLayer == LayerMask.NameToLayer("obstacleLayer");
            bool hitCar = hitLayer == LayerMask.NameToLayer("Cars");
            bool hitTrackBoundary = collision.gameObject.CompareTag("trackLayer");

            if (hitObstacle || hitTrackBoundary) // Only count walls, not other cars
            {
                lastCollisionTime = Time.time;

                // Only add to stuck timer if moving slow
                if (carController.carSpeed < 20f)
                {
                    stuckTimer += 0.3f;
                }

                Debug.Log($"{gameObject.name} hit {collision.gameObject.name}, speed: {carController.carSpeed:F1}");
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (driveController == driver.AI && raceStarted && !isRecovering)
        {
            int hitLayer = collision.gameObject.layer;
            bool hitObstacle = hitLayer == LayerMask.NameToLayer("obstacleLayer");
            bool hitTrackBoundary = collision.gameObject.CompareTag("trackLayer");

            // Stuck against wall
            if ((hitObstacle || hitTrackBoundary) && carController.carSpeed < 10f)
            {
                stuckTimer += Time.deltaTime * 2f;
            }
        }
    }

    private void FixedUpdate()
    {
        if (nodes.Count == 0) return;

        calculateDistanceOfWaypoints();

        if (driveController == driver.AI)
        {
            if (raceStarted)
            {
                HandleAICarState();
            }
        }

        switch (driveController)
        {
            case driver.AI:
                AIDrive();
                break;
            case driver.keyboard:
                KeyboardDrive();
                break;
        }
    }

    public void SetRaceStarted(bool started)
    {
        raceStarted = started;
        if (started)
        {
            lastPosition = transform.position;
            stuckTimer = 0f;
            isRecovering = false;
            lastForwardProgress = 0f;
            stuckFrameCounter = 0;
        }
    }

    private void HandleAICarState()
    {
        if (isRecovering)
        {
            RecoverCar();
        }
        else
        {
            CheckIfStuck();
        }
    }

    private void CheckIfStuck()
    {
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);
        bool isMovingSlow = carController.carSpeed < minSpeedThreshold;
        bool barelyMoved = distanceMoved < 0.5f;

        // Check forward progress toward waypoint
        bool notMakingProgress = false;
        if (currentWaypoint != null)
        {
            float currentProgress = Vector3.Dot(-transform.forward, (currentWaypoint.position - transform.position).normalized);
            notMakingProgress = currentProgress <= lastForwardProgress + 0.05f && isMovingSlow;
            lastForwardProgress = currentProgress;
        }

        // Check if facing wrong direction (more lenient threshold)
        bool facingWrongWay = false;
        if (currentWaypoint != null)
        {
            Vector3 toWaypoint = (currentWaypoint.position - transform.position).normalized;
            float angleToWaypoint = Vector3.Angle(-transform.forward, toWaypoint);
            facingWrongWay = angleToWaypoint > wrongDirectionAngleThreshold && carController.carSpeed < 15f;
        }

        // Count consecutive frames of being stuck
        if ((isMovingSlow && barelyMoved) || notMakingProgress)
        {
            stuckFrameCounter++;
        }
        else
        {
            stuckFrameCounter = 0;
        }

        // Stuck detection logic
        bool recentCollision = Time.time - lastCollisionTime < 3f;

        if (recentCollision && isMovingSlow && barelyMoved)
        {
            // Stuck after collision
            stuckTimer += Time.deltaTime * 1.5f;
        }
        else if (facingWrongWay)
        {
            // Facing wrong way and not moving fast
            stuckTimer += Time.deltaTime * 2f;
        }
        else if (stuckFrameCounter > 60) // ~1 second at 60fps
        {
            // Stuck for extended time
            stuckTimer += Time.deltaTime;
        }
        else if (carController.carSpeed > 30f)
        {
            // Moving well, reset timer
            stuckTimer = Mathf.Max(0f, stuckTimer - Time.deltaTime);
        }

        if (stuckTimer >= stuckTimeThreshold)
        {
            StartRecovery();
        }

        lastPosition = transform.position;
    }

    private void StartRecovery()
    {
        // Check if we can actually reverse (is the path behind clear?)
        if (!IsPathBehindClear())
        {
            // If path behind is blocked, just try to wiggle forward
            stuckTimer = stuckTimeThreshold * 0.5f; // Reduce timer, try again later
            return;
        }

        isRecovering = true;
        recoveryTimer = recoveryReverseTime;
        currentRecoveryPhase = RecoveryPhase.Reversing;

        // Store target direction (toward waypoint)
        if (currentWaypoint != null)
        {
            recoveryTargetDirection = (currentWaypoint.position - transform.position).normalized;
        }

        Debug.Log($"{gameObject.name} STARTING RECOVERY - Speed: {carController.carSpeed:F1}, StuckTimer: {stuckTimer:F2}");
    }

    private bool IsPathBehindClear()
    {
        Vector3 rayOrigin = transform.position + transform.up * 0.5f;
        Vector3 reverseDirection = transform.forward; // Behind the car (remember, -forward is actual forward)

        int obstacleLayerMask = (1 << LayerMask.NameToLayer("obstacleLayer")) |
                                (1 << LayerMask.NameToLayer("TrackLayer"));

        RaycastHit hit;
        bool pathClear = !Physics.Raycast(rayOrigin, reverseDirection, out hit, clearanceCheckDistance, obstacleLayerMask);

        Debug.DrawRay(rayOrigin, reverseDirection * clearanceCheckDistance, pathClear ? Color.green : Color.red, 0.5f);

        return pathClear;
    }

    private void RecoverCar()
    {
        recoveryTimer -= Time.deltaTime;

        switch (currentRecoveryPhase)
        {
            case RecoveryPhase.Reversing:
                // REVERSE: Use -1f (will become +1f after inversion = actual reverse)
                vertical = -1f;

                // Steer to align with waypoint while reversing
                if (currentWaypoint != null)
                {
                    Vector3 relativeWaypointPos = transform.InverseTransformPoint(currentWaypoint.position);
                    // When reversing, steer opposite to align front toward waypoint
                    horizontal = Mathf.Sign(relativeWaypointPos.x) * 0.7f;
                }
                else
                {
                    // Oscillating steering if no waypoint
                    horizontal = Mathf.Sin(Time.time * 3f) * 0.8f;
                }

                handbrake = false;

                // Check if we've reversed enough or cleared the obstacle
                if (recoveryTimer <= recoveryReverseTime * 0.3f || carController.carSpeed > 15f)
                {
                    // Move to reorienting phase
                    currentRecoveryPhase = RecoveryPhase.Reorienting;
                    recoveryTimer = 1.0f; // Give 1 second to reorient
                }
                break;

            case RecoveryPhase.Reorienting:
                // Stop reversing, try to face waypoint
                vertical = 0f;

                if (currentWaypoint != null)
                {
                    Vector3 toWaypoint = (currentWaypoint.position - transform.position).normalized;
                    float angleToWaypoint = Vector3.Angle(-transform.forward, toWaypoint);

                    if (angleToWaypoint < 45f)
                    {
                        // Good enough alignment, finish recovery
                        currentRecoveryPhase = RecoveryPhase.Complete;
                    }
                    else
                    {
                        // Keep steering toward waypoint
                        Vector3 relativeWaypointPos = transform.InverseTransformPoint(currentWaypoint.position);
                        horizontal = -Mathf.Sign(relativeWaypointPos.x) * 1f;

                        // Apply light throttle to help turn
                        vertical = -0.3f;
                    }
                }

                if (recoveryTimer <= 0)
                {
                    currentRecoveryPhase = RecoveryPhase.Complete;
                }
                break;

            case RecoveryPhase.Complete:
                // Recovery complete
                isRecovering = false;
                stuckTimer = 0f;
                vertical = 0f;
                horizontal = 0f;
                lastForwardProgress = 0f;
                stuckFrameCounter = 0;
                Debug.Log($"{gameObject.name} RECOVERY COMPLETE");
                break;
        }
    }

    private void AIDrive()
    {
        if (currentWaypoint == null || isRecovering) return;

        if (carController != null)
        {
            float currentSpeed = carController.carSpeed;
            float targetSpeed = aiTargetSpeed;

            bool isSharpTurnAhead = false;
            bool isAggressiveBrakeTurnAhead = false;
            float closestTurnAngle = 0f;

            for (int i = 0; i < aiWaypointLookAheadIndex + 2; i++)
            {
                int lookAheadIndex = (nodes.IndexOf(currentWaypoint) + i) % nodes.Count;
                if (lookAheadIndex < 0) lookAheadIndex += nodes.Count;
                Transform lookAheadWaypoint = nodes[lookAheadIndex];

                Vector3 relativeLookAheadPos = transform.InverseTransformPoint(lookAheadWaypoint.position);
                float lookAheadAngle = Mathf.Atan2(relativeLookAheadPos.x, relativeLookAheadPos.z) * Mathf.Rad2Deg;

                if (Mathf.Abs(lookAheadAngle) > closestTurnAngle)
                {
                    closestTurnAngle = Mathf.Abs(lookAheadAngle);
                }

                if (Mathf.Abs(lookAheadAngle) > aiSharpTurnAngleThreshold)
                {
                    isSharpTurnAhead = true;
                }
                if (Mathf.Abs(lookAheadAngle) > aiAggressiveBrakeAngleThreshold)
                {
                    isAggressiveBrakeTurnAhead = true;
                    break;
                }
            }

            if (isAggressiveBrakeTurnAhead && currentSpeed > (aiTargetSpeed * 0.7f))
            {
                vertical = -aiBrakeStrength;
                handbrake = true;
                targetSpeed *= 0.5f;
            }
            else if (isSharpTurnAhead && currentSpeed > (aiTargetSpeed * 0.85f))
            {
                vertical = -aiBrakeStrength * 0.5f;
                handbrake = false;
                targetSpeed *= 0.7f;
            }
            else
            {
                handbrake = false;
                vertical = 0f;
            }

            if (vertical >= 0)
            {
                if (currentSpeed < targetSpeed - 5f)
                {
                    vertical = 1f;
                }
                else if (currentSpeed > targetSpeed + 5f)
                {
                    vertical = 0f;
                }
                else
                {
                    vertical = 0.5f;
                }
            }
        }
        else
        {
            vertical = 0.3f;
        }

        AISteer();
        AIEnhancedObstacleAvoidance();
    }

    private void KeyboardDrive()
    {
        vertical = Input.GetAxis("Vertical");
        horizontal = Input.GetAxis("Horizontal");
        handbrake = Input.GetKey(KeyCode.Space);
    }

    private void calculateDistanceOfWaypoints()
    {
        Vector3 position = gameObject.transform.position;
        float distance = Mathf.Infinity;
        int closestWaypointIndex = 0;

        for (int i = 0; i < nodes.Count; i++)
        {
            Vector3 difference = nodes[i].transform.position - position;
            float currentDistance = difference.magnitude;
            if (currentDistance < distance)
            {
                distance = currentDistance;
                closestWaypointIndex = i;
            }
        }

        int targetWaypointIndex = (closestWaypointIndex + aiWaypointLookAheadIndex);
        targetWaypointIndex %= nodes.Count;
        if (targetWaypointIndex < 0) targetWaypointIndex += nodes.Count;

        currentWaypoint = nodes[targetWaypointIndex];
    }

    private void AISteer()
    {
        if (isRecovering) return;
        if (currentWaypoint == null) return;

        Vector3 relativeVector = transform.InverseTransformPoint(currentWaypoint.transform.position);

        if (relativeVector.magnitude > 0.001f)
        {
            relativeVector.Normalize();
        }

        float targetSteer = (-relativeVector.x) * steerForce * aiSteerSensitivity;
        horizontal = Mathf.Lerp(horizontal, targetSteer, Time.deltaTime * 5f);

        Debug.DrawLine(transform.position, currentWaypoint.position, Color.blue);
        Debug.DrawRay(transform.position, -transform.forward * 5f, Color.red);
    }

    private void AIEnhancedObstacleAvoidance()
    {
        if (isRecovering) return;

        Vector3 rayOrigin = transform.position + transform.up * 0.5f;
        Vector3 actualForward = -transform.forward;

        int obstacleLayerMask = (1 << LayerMask.NameToLayer("obstacleLayer")) |
                                (1 << LayerMask.NameToLayer("Cars")) |
                                (1 << LayerMask.NameToLayer("TrackLayer"));

        float closestObstacle = float.MaxValue;
        float avoidanceSteer = 0f;
        bool obstacleDetected = false;

        // Multi-ray detection system
        for (int i = 0; i < numberOfRays; i++)
        {
            float t = (float)i / (numberOfRays - 1); // 0 to 1
            float angle = Mathf.Lerp(-sideRayAngle, sideRayAngle, t);

            Vector3 rayDirection = Quaternion.AngleAxis(angle, transform.up) * actualForward;
            RaycastHit hit;

            if (Physics.Raycast(rayOrigin, rayDirection, out hit, avoidanceDistance, obstacleLayerMask))
            {
                obstacleDetected = true;
                float distanceToObstacle = hit.distance;

                // Debug visualization
                Debug.DrawRay(rayOrigin, rayDirection * distanceToObstacle, Color.red);

                if (distanceToObstacle < closestObstacle)
                {
                    closestObstacle = distanceToObstacle;

                    // Calculate avoidance direction
                    Vector3 localHitPoint = transform.InverseTransformPoint(hit.point);
                    float obstacleDirection = Mathf.Sign(localHitPoint.x);

                    // Steer away from obstacle, stronger when closer
                    float urgency = 1f - (distanceToObstacle / avoidanceDistance);
                    avoidanceSteer = -obstacleDirection * avoidanceStrength * urgency;
                }
            }
            else
            {
                // Debug visualization for clear rays
                Debug.DrawRay(rayOrigin, rayDirection * avoidanceDistance, Color.green);
            }
        }

        // Apply avoidance steering and speed reduction
        if (obstacleDetected)
        {
            horizontal = Mathf.Lerp(horizontal, avoidanceSteer, Time.deltaTime * 8f);

            // Slow down if very close to obstacle
            if (closestObstacle < avoidanceDistance * 0.4f)
            {
                vertical = Mathf.Lerp(vertical, vertical * wallAvoidanceSpeedReduction, Time.deltaTime * 10f);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (currentWaypoint != null && driveController == driver.AI)
        {
            Gizmos.color = isRecovering ? Color.red : Color.magenta;
            Gizmos.DrawWireSphere(currentWaypoint.position, 5);
            Gizmos.DrawLine(transform.position, currentWaypoint.position);

            // Show actual forward direction
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, -transform.forward * 10f);

            // Show recovery phase
            if (isRecovering)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, 8f);
            }
        }
    }
}