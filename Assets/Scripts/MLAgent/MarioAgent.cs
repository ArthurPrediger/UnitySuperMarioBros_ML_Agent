using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Linq;

public class MarioAgent : Agent
{
    private Camera mainCamera;
    private Rigidbody2D rb;
    private Collider2D capsuleCollider;

    private Vector2 velocity;
    private float inputAxis;
    private float[] axis = { 0.0f, -1.0f, 1.0f };

    public float moveSpeed = 8f;
    public float maxJumpHeight = 5f;
    public float maxJumpTime = 1f;
    public float jumpForce => (2f * maxJumpHeight) / (maxJumpTime / 2f);
    public float gravity => (-2f * maxJumpHeight) / Mathf.Pow(maxJumpTime / 2f, 2f);

    float multiplier = 1f;

    public bool grounded { get; private set; }
    public bool jumping { get; private set; }
    public bool running => Mathf.Abs(velocity.x) > 0.25f || Mathf.Abs(inputAxis) > 0.25f;
    public bool sliding => (inputAxis > 0f && velocity.x < 0f) || (inputAxis < 0f && velocity.x > 0f);
    public bool falling => velocity.y < 0f && !grounded;

    public bool tryingToEnterPipe { get; private set; }

    private bool jumpPressed = false;
    private bool jumpJustPressed = false;
    private float distanceToTarget = 0f;

    private List<Transform> agentTargets = new();
    private List<Transform> agentInitTrans = new();
    private List<float> curDistProgression = new();
    private readonly float distProgressionReward = 0.01f;
    private float maxTargetDist;

    private void Awake()
    {
        mainCamera = Camera.main;
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<Collider2D>();
    }
    private void Start()
    {
        rb.isKinematic = false;
        capsuleCollider.enabled = true;
        velocity = Vector2.zero;
        inputAxis = 0f;
        jumping = false;
        tryingToEnterPipe = false;

        GameObject flagPole = GameObject.FindGameObjectWithTag("FinalGoal");

        agentTargets.Add(flagPole.transform);
        agentInitTrans.Add(rb.transform);
        Vector2 curTarget = agentTargets.Last().position;
        distanceToTarget = Vector3.Distance(agentInitTrans.Last().position, curTarget);
        maxTargetDist = distanceToTarget;
        curDistProgression.Add(distProgressionReward);
    }

    public override void OnEpisodeBegin()
    {
    }

    public void AddTarget(Transform targetTrans, Transform initTrans)
    {
        agentTargets.Add(targetTrans);
        agentInitTrans.Add(initTrans);
        curDistProgression.Add(distProgressionReward);
        maxTargetDist = Vector3.Distance(agentInitTrans.Last().position, agentTargets.Last().position);
    }

    public void RemoveLastTarget()
    {
        if(agentTargets.Count > 1)
        {
            agentTargets.RemoveAt(agentTargets.Count - 1);
            agentInitTrans.RemoveAt(agentInitTrans.Count - 1);
            curDistProgression.RemoveAt(curDistProgression.Count - 1);
            maxTargetDist = Vector3.Distance(agentInitTrans.Last().position, agentTargets.Last().position);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 18 observations
        sensor.AddObservation(rb.position);
        sensor.AddObservation(velocity);
        sensor.AddObservation(grounded);
        sensor.AddObservation(jumping);
        sensor.AddObservation(running);
        sensor.AddObservation(sliding);
        sensor.AddObservation(falling);
        sensor.AddObservation(jumpJustPressed);
        sensor.AddObservation(jumpPressed);
        sensor.AddObservation(multiplier);
        sensor.AddObservation(agentTargets.Last().position);
        sensor.AddObservation(agentInitTrans.Last().position);
        sensor.AddObservation(agentTargets.Last().position - agentInitTrans.Last().position);
        sensor.AddObservation(curDistProgression.Last());

        // 6 raycasts (front, back, down, up, 4 diagonals)
        float rayDistance = maxJumpHeight + 0.001f;
        sensor.AddObservation(Physics2D.Raycast(rb.position, Vector2.right, rayDistance, LayerMask.GetMask("Default")).distance);
        sensor.AddObservation(Physics2D.Raycast(rb.position, Vector2.left, rayDistance, LayerMask.GetMask("Default")).distance);
        sensor.AddObservation(Physics2D.Raycast(rb.position, Vector2.down, rayDistance, LayerMask.GetMask("Default")).distance);
        sensor.AddObservation(Physics2D.Raycast(rb.position, Vector2.up, rayDistance, LayerMask.GetMask("Default")).distance);
        sensor.AddObservation(Physics2D.Raycast(rb.position, new Vector2(1, 1), rayDistance, LayerMask.GetMask("Default")).distance);
        sensor.AddObservation(Physics2D.Raycast(rb.position, new Vector2(-1, 1), rayDistance, LayerMask.GetMask("Default")).distance);
        sensor.AddObservation(Physics2D.Raycast(rb.position, new Vector2(-1, -1), rayDistance, LayerMask.GetMask("Default")).distance);
        sensor.AddObservation(Physics2D.Raycast(rb.position, new Vector2(1, -1), rayDistance, LayerMask.GetMask("Default")).distance);

        sensor.AddObservation(Physics2D.Raycast(rb.position, Vector2.right, rayDistance, LayerMask.GetMask("Enemy")).distance);
        sensor.AddObservation(Physics2D.Raycast(rb.position, Vector2.left, rayDistance, LayerMask.GetMask("Enemy")).distance);
        sensor.AddObservation(Physics2D.Raycast(rb.position, Vector2.down, rayDistance, LayerMask.GetMask("Enemy")).distance);
        sensor.AddObservation(Physics2D.Raycast(rb.position, Vector2.up, rayDistance, LayerMask.GetMask("Enemy")).distance);
        sensor.AddObservation(Physics2D.Raycast(rb.position, new Vector2(1, 1), rayDistance, LayerMask.GetMask("Enemy")).distance);
        sensor.AddObservation(Physics2D.Raycast(rb.position, new Vector2(-1, 1), rayDistance, LayerMask.GetMask("Enemy")).distance);
        sensor.AddObservation(Physics2D.Raycast(rb.position, new Vector2(-1, -1), rayDistance, LayerMask.GetMask("Enemy")).distance);
        sensor.AddObservation(Physics2D.Raycast(rb.position, new Vector2(1, -1), rayDistance, LayerMask.GetMask("Enemy")).distance);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int move = actions.DiscreteActions[0]; // 0,1,2
        int jump = actions.DiscreteActions[1]; // 0,1
        int enterPipe = actions.DiscreteActions[2]; // 0,1

        inputAxis = axis[move];

        tryingToEnterPipe = enterPipe == 1;

        if(jump == 1 && !jumpJustPressed && !jumpPressed)
        {
            jumpJustPressed = true;
            jumpPressed = true;
        }
        else if(jump == 1 && jumpPressed)
        {
            jumpJustPressed = false;
        }
        else if (jump == 0)
        {
            jumpJustPressed = false;
            jumpPressed = false;
        }

        // Small living penalty
        AddReward(-0.001f);

        // Reward for forward movement
        Vector2 curTarget = agentTargets.Last().position;
        distanceToTarget = Vector2.Distance(rb.position, curTarget);
        //Debug.Log(distanceToTarget);
        float normalizedDistProgression = (maxTargetDist - distanceToTarget) / maxTargetDist;

        while (normalizedDistProgression > curDistProgression.Last())
        {
            if(agentTargets.Count == 1)
                AddReward(2f);
            else
                AddReward(0.1f);
            Debug.Log("Reward for " + (curDistProgression.Last() * 100f).ToString() + 
                "% of the progression distance for the current target.");
            curDistProgression[^1] += distProgressionReward;
        }
    }


    private void Update()
    {
        HorizontalMovement();

        grounded = rb.Raycast(Vector2.down);

        if (grounded)
        {
            GroundedMovement();
        }

        ApplyGravity();
    }

    private void FixedUpdate()
    {
        // Move mario based on his velocity
        Vector2 position = rb.position;
        position += velocity * Time.fixedDeltaTime;

        // Clamp within the screen bounds
        Vector2 leftEdge = mainCamera.ScreenToWorldPoint(Vector2.zero);
        Vector2 rightEdge = mainCamera.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));
        position.x = Mathf.Clamp(position.x, leftEdge.x + 0.5f, rightEdge.x - 0.5f);

        rb.MovePosition(position);
    }

    private void HorizontalMovement()
    {
        // Accelerate / decelerate
        velocity.x = Mathf.MoveTowards(velocity.x, inputAxis * moveSpeed, moveSpeed * Time.deltaTime);

        // Check if running into a wall
        if (rb.Raycast(Vector2.right * velocity.x))
        {
            velocity.x = 0f;
        }

        // Flip sprite to face direction
        if (velocity.x > 0f)
        {
            transform.eulerAngles = Vector3.zero;
        }
        else if (velocity.x < 0f)
        {
            transform.eulerAngles = new Vector3(0f, 180f, 0f);
        }
    }

    private void GroundedMovement()
    {
        // Prevent gravity from infinitly building up
        velocity.y = Mathf.Max(velocity.y, 0f);
        jumping = velocity.y > 0f;

        // Perform jump
        if (jumpJustPressed)
        {
            velocity.y = jumpForce;
            jumping = true;
        }
    }

    private void ApplyGravity()
    {
        // Check if falling
        bool falling = velocity.y < 0f || !jumpPressed;
        multiplier = falling ? 2f : 1f;
        //multiplier = falling ? 1f : 1f;

        // Apply gravity and terminal velocity
        velocity.y += gravity * multiplier * Time.deltaTime;
        velocity.y = Mathf.Max(velocity.y, gravity / 2f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = 0;  // No move
        if (Input.GetKey(KeyCode.A)) discreteActions[0] = 1;
        if (Input.GetKey(KeyCode.D)) discreteActions[0] = 2;
        discreteActions[1] = Input.GetKey(KeyCode.W) ? 1 : 0;
        discreteActions[2] = Input.GetKey(KeyCode.S) ? 1 : 0;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            // Bounce off enemy head
            if (transform.DotTest(collision.transform, Vector2.down))
            {
                velocity.y = jumpForce / 2f;
                jumping = true;
            }
        }
        else if (collision.gameObject.layer != LayerMask.NameToLayer("PowerUp"))
        {
            // Stop vertical movement if mario bonks his head
            if (transform.DotTest(collision.transform, Vector2.up))
            {
                velocity.y = 0f;
            }
        }
    }
}
