//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using MLAgents;
//using System;

//namespace Aircraft
//{
//    public class AircraftAgent2 : Agent
//    {
//        [Header("MovementParameters")]
//        public float thrust = 1000000f;
//        public float pitchSpeed = 100f;
//        public float yawSpeed = 100f;
//        public float rollSpeed = 100f;
//        public float boostMultiplier = 100f;

//        [Header("Explosion Stuff")]
//        [Tooltip("The Aircraft mesh that will disapper on explosion")]
//        public GameObject meshObject;

//        public GameObject explsionEffect;

//        [Header("Training Parameters")]
//        public int stepTimeout = 300;

//        public int NextCheckpointIndex { get; set; }


//        private AircraftArea area;
//        new private Rigidbody rigidbody;
//        private TrailRenderer trail;
//        private RayPerception3D rayPerception;


       
//        // When the next step timeout will be during training
//        private float nextStepTimeout;
//        //wheter the aircraft is frozen intentionally
//        private bool frozen = false;

//        //Controls
//        private float pitchChange = 0f;
//        private float smoothpitchChange = 0f;
//        private float maxPitchAngle = 45f;
//        private float yawChange = 0f;
//        private float smoothYawChange = 0f;

//        private float rollChange = 0f;
//        private float smoothRollChange = 0f;
//        private float maxRollAngle = 45f;
//        private bool boost;

//        public override void InitializeAgent()
//        {
//            base.InitializeAgent();
//            area = GetComponentInParent<AircraftArea>();
//            rigidbody = GetComponent<Rigidbody>();
//            trail = GetComponent<TrailRenderer>();
//            rayPerception = GetComponent<RayPerception3D>();

//            //override the max step in inspector; 5000 for trainig, 0 = infinte for player
//            agentParameters.maxStep = area.trainingMode ? 5000 : 0;
//        }

//        public override void AgentAction(float[] vectorAction)
//        {
//            //Read Values for Pitch and Yaw
//            // Pitch - Y axis Up and Down
//            // Yaw - turning, the plane in x axis right left
//            // roll over - rolling over the plane

//            pitchChange = vectorAction[0]; // up or none 0- dont pitch, 1 pitch up, 2 pitch down

//            if(pitchChange == 2)
//            {
//                pitchChange = -1;
//            }

//            yawChange = vectorAction[1]; // turn right or none, 0- dont turn, 1 to turn right, 2 to turn left
//            if(yawChange == 2)
//            {
//                yawChange = -1;
//            }

//            boost = vectorAction[2] == 1;
//            if (boost && !trail.emitting) trail.Clear();
//            trail.emitting = boost;

//            if (frozen) return;

//            ProcessMovement();

//            if(area.trainingMode)
//            {
//                // small negative rewards for every step.
//                AddReward(-1f / agentParameters.maxStep);

//                //Mkae sure we haven't run out of time if trainig.
//                if(GetStepCount() > nextStepTimeout)
//                {
//                    AddReward(-.5f);
//                    Done();
//                    AgentReset();
//                }

//                Vector3 localCheckpointDir = VectorToNextCheckpoint();
//                if (localCheckpointDir.magnitude < area.AircraftAcademy.CheckpointRadius)
//                {
//                    GotCheckpoint();
//                }
//            }

//        }


//        public override void AgentReset()
//        {
//            //Reset Velocity position, orientation

//            rigidbody.velocity = Vector3.zero;
//            rigidbody.angularVelocity = Vector3.zero;
//            trail.emitting = false;
//            area.ResetAgentPosition(agent: this, randomize: area.trainingMode);


//            if (area.trainingMode) nextStepTimeout = GetStepCount() + stepTimeout;
//        }
//        /// <summary>
//        /// Prevent agent from moving and taking actions
//        /// </summary>
//        public void FreezeAgent()
//        {
//            Debug.Assert(area.trainingMode == false, "Freeze/Thaw not supported in training");
//            frozen = true;
//            rigidbody.Sleep();
//            trail.emitting = false;


//        }

//        /// <summary>
//        /// Resume movement
//        /// </summary>
//        public void ThawAgent()
//        {
//            Debug.Assert(area.trainingMode == false, "Freeze/Thaw not supported in training");
//            frozen = false;
//            rigidbody.WakeUp();
            
//        }
//        /// <summary>
//        /// collect observation used by agent to make decisions
//        /// </summary>

//        public override void CollectObservations()
//        {
//            //Observe aircraft velocity
//            // 1 Vector3 = 3 Values
//            AddVectorObs(transform.InverseTransformDirection(rigidbody.velocity));

//            //Where is the next checkpoint
//            // 1 vector3 = 3 values
//            AddVectorObs(VectorToNextCheckpoint());

//            //orientation of the next checkpoint
//            // 1 vector3 = 3 values
//            Vector3 nextCheckpointForward = area.Checkpoints[NextCheckpointIndex].transform.forward;
//            AddVectorObs(transform.InverseTransformDirection(nextCheckpointForward));


//            //observe RayPerceptionResults
//            string[] detectableObjects = { "Untagged", "checkpoint" };

//            //Look ahead and upward
//            // (2 tags + 1 hit/not + 1 distance to obj) * 3 rayangles = 12 values
//            AddVectorObs(rayPerception.Perceive(rayDistance: 250f, rayAngles: new float[] { 60f, 90f, 120f },
//                detectableObjects: detectableObjects,
//                startOffset: 0,
//                endOffset: 75f));

//            // Look Center and AT HORIZON
//            // (2 tags + 1 hit/not + 1 distance to obj) * 7 rayangles = 28 values
//            AddVectorObs(rayPerception.Perceive(rayDistance: 250f, rayAngles: new float[] { 60f, 70f, 80f, 90f,100f,110f, 120f },
//                detectableObjects: detectableObjects,
//                startOffset: 0,
//                endOffset: 0f));

//            // Look ahead and downward
//            // (2 tags + 1 hit/not + 1 distance to obj) * 3 rayangles = 12 values
//            AddVectorObs(rayPerception.Perceive(rayDistance: 250f, rayAngles: new float[] { 60f, 90f, 120f },
//                detectableObjects: detectableObjects,
//                startOffset: 0,
//                endOffset: -75f));


//            //total observations = 3 + 3+ 3 + 12 + 28 + 12 = 61




//        }





//        /// <summary>
//        /// Called when the agent flies through the correct checkpoint
//        /// </summary>
//        private void GotCheckpoint()
//        {
//            NextCheckpointIndex = (NextCheckpointIndex + 1) % area.Checkpoints.Count;
//            if(area.trainingMode)
//            {
//                AddReward(.5f);
//                nextStepTimeout = GetStepCount() + stepTimeout;
//            }
//        }

//        /// <summary>
//        /// // Gets a vector to next checkpoint the agent needs to fly through
//        /// </summary>
//        /// <returns>local space vector</returns>

//        private Vector3 VectorToNextCheckpoint()
//        {
//            Vector3 nextCheckPointDir = area.Checkpoints[NextCheckpointIndex].transform.position - transform.position;
//            Vector3 LocalCheckPointDir = transform.InverseTransformDirection(nextCheckPointDir);

//            return LocalCheckPointDir;
//        }

//        //calculate and apply movement
//        private void ProcessMovement()
//        {
//            float boostModifier = boost ? boostMultiplier : 1f;

//            //Apply forward thrust
//            rigidbody.AddForce(transform.forward * thrust * boostModifier, ForceMode.Force);


//            //current rotation
//            Vector3 curRot = transform.rotation.eulerAngles;

//            // Claculate Roll Angle( 180 to -180)
//            float rollAngle = curRot.z > 180f ? curRot.z - 360f : curRot.z;
//            if(yawChange == 0)
//            {
//                //Not turning, AI to smoothly roll back to center and level
//                rollChange = -rollAngle / maxRollAngle;

//            }
//            else
//            {
//                rollChange = -yawChange;
//            }

//            //calculate smooth deltas
//            smoothpitchChange = Mathf.MoveTowards(smoothpitchChange, pitchChange, 2f * Time.fixedDeltaTime);
//            smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);
//            smoothRollChange = Mathf.MoveTowards(smoothRollChange, rollChange, 2f * Time.fixedDeltaTime);

//            //calculate new pitch yaw and roll. Clamp pitch and roll.
//            float pitch = ClampAngles(curRot.x + smoothpitchChange * Time.fixedDeltaTime * pitchSpeed, -maxPitchAngle, maxPitchAngle);
//            float yaw = curRot.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed;
//            float roll = ClampAngles(curRot.z + smoothRollChange * Time.fixedDeltaTime * rollSpeed, -maxRollAngle, maxRollAngle);


//            //Set New Rotation

//            transform.rotation = Quaternion.Euler(pitch, yaw, roll);
//        }

//        private static float ClampAngles(float angle, float from, float to)
//        {
//            if (angle < 0f) angle = 360f + angle;
//            if (angle > 180f) return Mathf.Max(angle, 360f + from);
//            return Mathf.Min(angle, to);
//        }


//        private void OnTriggerEnter(Collider other)
//        {
//            if(other.transform.CompareTag("checkpoint") && other.gameObject == area.Checkpoints[NextCheckpointIndex])
//            {
//                GotCheckpoint();
//            }
//        }

//        private void OnCollisionEnter(Collision collision)
//        {
//            if(!collision.transform.CompareTag("agent"))
//            {
//                //We hit something other than agent
//                AddReward(-1f);
//                Done();
//                return;
//            }
//            else
//            {
//                StartCoroutine(ExplosionReset());
//            }
//        }
//        /// <summary>
//        /// Reset aircraft to most recent completed Checkpoint
//        /// </summary>
//        /// <returns></returns>
//        private IEnumerator ExplosionReset()
//        {
//            FreezeAgent();

//            // disable aircraft Mesh Object and enable explosion
//            meshObject.SetActive(false);
//            explsionEffect.SetActive(true);
//            yield return new WaitForSeconds(2f);

//            //Disable exploison, re-enable aircraft mesh
//            meshObject.SetActive(true);
//            explsionEffect.SetActive(false);
//            area.ResetAgentPosition(agent: this);
//            yield return new WaitForSeconds(1f);


//            ThawAgent();
//        }
//    }
//}
