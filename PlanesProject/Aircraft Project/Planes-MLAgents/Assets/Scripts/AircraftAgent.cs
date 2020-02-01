﻿using MLAgents;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Aircraft
{
    public class AircraftAgent : Agent
    {
        [Header("Movement Parameters")]
        public float thrust = 100000f;
        public float pitchSpeed = 100f;
        public float yawSpeed = 100f;
        public float rollSpeed = 100f;
        public float boostMultiplier = 2f;

        [Header("Explosion Stuff")]
        [Tooltip("The aircraft mesh that will disappear on explosion")]
        public GameObject meshObject;

        [Tooltip("The game object of the explosion particle effect")]
        public GameObject explosionEffect;

        [Header("Training")]
        [Tooltip("Number of steps to time out after in training")]
        public int stepTimeout = 300;

        public int NextCheckpointIndex { get; set; }

        // Components to keep track of
        private AircraftArea area;
        new private Rigidbody rigidbody;
        private TrailRenderer trail;
        private RayPerception3D rayPerception;

        // When the next step timeout will be during training
        private float nextStepTimeout;

        // Whether the aircraft is frozen (intentionally not flying)
        private bool frozen = false;

        [Header("Input Binding")]
        public InputAction pitchInput;
        public InputAction yawInput;
        public InputAction boostInput;
        public InputAction pauseInput;
        public InputAction fireInput;

        public GameObject fpsCam;
        public float fireRange = 100f;
        public float damage = 10f;



        // Controls
        private float pitchChange = 0f;
        private float smoothPitchChange = 0f;
        private float maxPitchAngle = 45f;
        private float yawChange = 0f;
        private float smoothYawChange = 0f;
        private float rollChange = 0f;
        private float smoothRollChange = 0f;
        private float maxRollAngle = 45f;
        private bool boost;
        private bool fire1;

        public override void InitializeAgent()
        {
            base.InitializeAgent();
            area = GetComponentInParent<AircraftArea>();
            rigidbody = GetComponent<Rigidbody>();
            trail = GetComponent<TrailRenderer>();
            rayPerception = GetComponent<RayPerception3D>();

            yawInput.Enable();
            pitchInput.Enable();
            boostInput.Enable();
            pauseInput.Enable();
            fireInput.Enable();


            // Override the max step set in the inspector
            // Max 5000 steps if training, infinite steps if racing
            agentParameters.maxStep = area.trainingMode ? 5000 : 0;
        }

        

        

        /// <summary>
        /// Read action inputs from vectorAction
        /// </summary>
        /// <param name="vectorAction">The chosen actions</param>
        /// <param name="textAction">The chosen text action (unused)</param>
        public override void AgentAction(float[] vectorAction)
        {
            // Read values for pitch and yaw
            pitchChange = vectorAction[0]; // up or none
            if (pitchChange == 2) pitchChange = -1f; // down
            yawChange = vectorAction[1]; // turn right or none
            if (yawChange == 2) yawChange = -1f; // turn left

            // Read value for boost and enable/disable trail renderer
            boost = vectorAction[2] == 1;
            if (boost && !trail.emitting) trail.Clear();
            trail.emitting = boost;

            fire1 = vectorAction[3] == 1;
            if (fire1) ShootAirplane();


            if (frozen) return;

            ProcessMovement();

            if (area.trainingMode)
            {
                // Small negative reward every step
                AddReward(-1f / agentParameters.maxStep);

                // Make sure we haven't run out of time if training
                if (GetStepCount() > nextStepTimeout)
                {
                    AddReward(-.5f);
                    Done();
                }

                Vector3 localCheckpointDir = VectorToNextCheckpoint();
                //if (localCheckpointDir.magnitude < area.AircraftAcademy.resetParameters["checkpoint_radius"])
                if (localCheckpointDir.magnitude < area.AircraftAcademy.CheckpointRadius)
                {
                    GotCheckpoint();
                }
                
            }
        }

        /// <summary>
        /// Collects observations used by agent to make decisions
        /// </summary>
        public override void CollectObservations()
        {
            // Observe aircraft velocity (1 vector3 = 3 values)
            AddVectorObs(transform.InverseTransformDirection(rigidbody.velocity));

            // Where is the next checkpoint? (1 vector3 = 3 values)
            AddVectorObs(VectorToNextCheckpoint());

            // Orientation of the next checkpoint (1 vector3 = 3 values)
            Vector3 nextCheckpointForward = area.Checkpoints[NextCheckpointIndex].transform.forward;
            AddVectorObs(transform.InverseTransformDirection(nextCheckpointForward));

            // Observe ray perception results
            string[] detectableObjects = { "Untagged", "Checkpoint", "agent" };

            // Look ahead and upward
            // (2 tags + 1 hit/not + 1 distance to obj) * 3 ray angles = 12 values
            AddVectorObs(rayPerception.Perceive(
                rayDistance: 250f,
                rayAngles: new float[] { 60f, 90f, 120f },
                detectableObjects: detectableObjects,
                startOffset: 0f,
                endOffset: 75f
            ));
            // Look center and at several angles along the horizon
            // (2 tags + 1 hit/not + 1 distance to obj) * 7 ray angles = 28 values
            AddVectorObs(rayPerception.Perceive(
                rayDistance: 250f,
                rayAngles: new float[] { 60f, 70f, 80f, 90f, 100f, 110f, 120f },
                detectableObjects: detectableObjects,
                startOffset: 0f,
                endOffset: 0f
            ));
            // Look ahead and downward
            // (2 tags + 1 hit/not + 1 distance to obj) * 3 ray angles = 12 values
            AddVectorObs(rayPerception.Perceive(
                rayDistance: 250f,
                rayAngles: new float[] { 60f, 90f, 120f },
                detectableObjects: detectableObjects,
                startOffset: 0f,
                endOffset: -75f
            ));

            // Total Observations = 3 + 3 + 3 + 12 + 28 + 12 = 61
        }

        public override void AgentReset()
        {
            // Reset the velocity, position, and orientation
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            trail.emitting = false;
            area.ResetAgentPosition(agent : this, randomize: area.trainingMode);

            // Update the step timeout if training
            if (area.trainingMode) nextStepTimeout = GetStepCount() + stepTimeout;
        }


        public void ShootAirplane()
        {
            ShootImprovedTracer shootTracerObj = GetComponentInChildren<ShootImprovedTracer>();
            RaycastHit hit;
            if(Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out hit, fireRange))
            {
                Debug.Log(hit.transform.name);
                
                shootTracerObj.ShootEnemy(hit);
                if (area.trainingMode)
                {
                    if (hit.transform.CompareTag("agent"))
                    {
                        AddReward(1f);
                        Debug.Log("Reward + 1f");
                            }
                    else
                    { AddReward(-.1f);
                        Debug.Log("Reward - 0.1f");
                    }
                    nextStepTimeout = GetStepCount() + stepTimeout;
                }

            }
        }

        /// <summary>
        /// Prevent the agent from moving and taking actions
        /// </summary>
        public void FreezeAgent()
        {
            Debug.Assert(area.trainingMode == false, "Freeze/Thaw not supported in training");
            frozen = true;
            rigidbody.Sleep();
            trail.emitting = false;
        }

        /// <summary>
        /// Resume agent movement and actions
        /// </summary>
        public void ThawAgent()
        {
            Debug.Assert(area.trainingMode == false, "Freeze/Thaw not supported in training");
            frozen = false;
            rigidbody.WakeUp();
        }

        /// <summary>
        /// Called when the agent flies through the correct checkpoint
        /// </summary>
        private void GotCheckpoint()
        {
            // Next checkpoint reached, update
            NextCheckpointIndex = (NextCheckpointIndex + 1) % area.Checkpoints.Count;

            if (area.trainingMode)
            {
                AddReward(.5f);
                nextStepTimeout = GetStepCount() + stepTimeout;
            }
        }

        /// <summary>
        /// Gets a vector to the next checkpoint the agent needs to fly through
        /// </summary>
        /// <returns>A local-space vector</returns>
        private Vector3 VectorToNextCheckpoint()
        {
            Vector3 nextCheckpointDir = area.Checkpoints[NextCheckpointIndex].transform.position - transform.position;
            Vector3 localCheckpointDir = transform.InverseTransformDirection(nextCheckpointDir);
            return localCheckpointDir;
        }

        // Calculate and apply movement
        private void ProcessMovement()
        {
            // Calculate boost
            float boostModifier = boost ? boostMultiplier : 1f;

            // Apply forward thrust
            rigidbody.AddForce(transform.forward * thrust * boostModifier, ForceMode.Force);

            // Get the current rotation
            Vector3 curRot = transform.rotation.eulerAngles;

            // Calculate the roll angle (between -180 and 180)
            float rollAngle = curRot.z > 180f ? curRot.z - 360f : curRot.z;
            if (yawChange == 0f)
            {
                // Not turning; smoothly roll toward center
                rollChange = -rollAngle / maxRollAngle;
            }
            else
            {
                // Turning; roll in opposite direction of turn
                rollChange = -yawChange;
            }

            // Calculate smooth deltas
            smoothPitchChange = Mathf.MoveTowards(smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
            smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);
            smoothRollChange = Mathf.MoveTowards(smoothRollChange, rollChange, 2f * Time.fixedDeltaTime);

            // Calculate new pitch, yaw, and roll. Clamp pitch and roll.
            float pitch = ClampAngle(curRot.x + smoothPitchChange * Time.fixedDeltaTime * pitchSpeed,
                                        -maxPitchAngle,
                                        maxPitchAngle);
            float yaw = curRot.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed;
            float roll = ClampAngle(curRot.z + smoothRollChange * Time.fixedDeltaTime * rollSpeed,
                                    -maxRollAngle,
                                    maxRollAngle);

            // Set the new rotation
            transform.rotation = Quaternion.Euler(pitch, yaw, roll);
        }

        /// <summary>
        /// Clamps an angle between two values
        /// </summary>
        /// <param name="angle">The input angle</param>
        /// <param name="from">The lower limit</param>
        /// <param name="to">The upper limit</param>
        /// <returns></returns>
        private static float ClampAngle(float angle, float from, float to)
        {
            if (angle < 0f) angle = 360f + angle;
            if (angle > 180f) return Mathf.Max(angle, 360f + from);
            return Mathf.Min(angle, to);
        }

        /// <summary>
        /// React to entering a trigger
        /// </summary>
        /// <param name="other">The collider entered</param>
        private void OnTriggerEnter(Collider other)
        {
            if (other.transform.CompareTag("Checkpoint") &&
                other.gameObject == area.Checkpoints[NextCheckpointIndex])
            {
                GotCheckpoint();
            }
        }

        //public override float[] Heuristic()
        //{
        //    float pitchValue = Mathf.Round(pitchInput.ReadValue<float>());
        //    float yawValue = Mathf.Round(yawInput.ReadValue<float>());
        //    float boostValue = Mathf.Round(boostInput.ReadValue<float>());


        //    if (pitchValue == -1f) pitchValue = 2f;

        //    if (yawValue == -1f) yawValue = 2f;

        //    return new float[] { pitchValue, yawValue, boostValue };

        //}

        /// <summary>
        /// React to collisions
        /// </summary>
        /// <param name="collision">Collision info</param>
        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.transform.CompareTag("agent"))
            {
                // We hit something that wasn't another agent
                if (area.trainingMode)
                {
                    AddReward(-1f);
                    Done();
                    return;
                }
                else
                {
                    StartCoroutine(ExplosionReset());
                }
            }

            if (collision.transform.CompareTag("Missile"))
            {
                // We hit something that wasn't another agent
                Debug.Log(transform.name + ": may day");
                if (area.trainingMode)
                {
                    //AddReward(-1f);
                    Done();
                    return;
                }
                else
                {
                    StartCoroutine(ExplosionReset());
                }
            }


        }
        /// <summary>
        /// Resets the aircraft to the most recent complete checkpoint
        /// </summary>
        /// <returns>yield return</returns>
        private IEnumerator ExplosionReset()
        {
            FreezeAgent();

            // Disable aircraft mesh object, enable explosion
            meshObject.SetActive(false);
            explosionEffect.SetActive(true);
            yield return new WaitForSeconds(2f);

            // Disable explosion, re-enable aircraft mesh
            meshObject.SetActive(true);
            explosionEffect.SetActive(false);
            area.ResetAgentPosition(agent: this);
            yield return new WaitForSeconds(1f);

            ThawAgent();
        }

        private void OnDestroy()
        {
            yawInput.Disable();
            pitchInput.Disable();
            boostInput.Disable();
            pauseInput.Disable();
            fireInput.Disable();
        }
    }
}