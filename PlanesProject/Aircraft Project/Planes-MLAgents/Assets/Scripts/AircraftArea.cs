using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;



namespace Aircraft
{
    public class AircraftArea : MonoBehaviour
    {
        public CinemachineSmoothPath racePath;
        
        public GameObject checkpointPrefab;
        public GameObject finishCheckpointPrefab;

        public bool trainingMode;


        public List<AircraftAgent> AircraftAgents { get; private set; }

        public List<GameObject> Checkpoints { get; private set; }

        public AircraftAcademy AircraftAcademy { get; private set; }


        private void Awake()
        {
            //actions to perform when the script wakes up
            //find all aircraft agents in the area.

            AircraftAgents = transform.GetComponentsInChildren<AircraftAgent>().ToList();
            
            Debug.Assert(AircraftAgents.Count > 0, "No Aircrafts found");

            AircraftAcademy = FindObjectOfType<AircraftAcademy>();
        }

        private void Start()
        {
            Debug.Assert(racePath != null, "Race Path not set");

            Checkpoints = new List<GameObject>();

            int numCheckpoints = (int)racePath.MaxUnit(CinemachinePathBase.PositionUnits.PathUnits);
            for(int i = 0; i < numCheckpoints; i++)
            {
                //instantiate a checkpoint of finish game
                GameObject checkpoint;
                if (i == (numCheckpoints - 1)) checkpoint = Instantiate<GameObject>(finishCheckpointPrefab);
                else checkpoint = Instantiate<GameObject>(checkpointPrefab);

                //set paret position and rotation
                checkpoint.transform.SetParent(racePath.transform);
                checkpoint.transform.localPosition = racePath.m_Waypoints[i].position;
                checkpoint.transform.rotation = racePath.EvaluateOrientationAtUnit(i, CinemachinePathBase.PositionUnits.PathUnits);

                //Add the checkpoint to the List
                Checkpoints.Add(checkpoint);

             
            }
        }

        //reset the position of agent using its current next checkpoint index unless randomize is true


        public void ResetAgentPosition(AircraftAgent agent, bool randomize = false)
        {
            if(randomize)
            {
                //Pick a new next checkpoint at random
                agent.NextCheckpointIndex = Random.Range(0, Checkpoints.Count);
            }

            //Set Start Position to a previous Checkpoint Index
            int previousCheckpointIndex = agent.NextCheckpointIndex - 1;
            if (previousCheckpointIndex == -1) previousCheckpointIndex = Checkpoints.Count - 1;

            float startPosition = racePath.FromPathNativeUnits(previousCheckpointIndex, CinemachinePathBase.PositionUnits.PathUnits);

            // Convert the position on the race path to a position in 3d space
            Vector3 basePosition = racePath.EvaluatePosition(startPosition);

            //orientation at that position
            Quaternion orientation = racePath.EvaluateOrientation(startPosition);


            Vector3 positionOffset = Vector3.right * (AircraftAgents.IndexOf(agent) - AircraftAgents.Count / 2f) * 10f;

            agent.transform.position = basePosition + orientation * positionOffset;
            agent.transform.rotation = orientation;
        }
    }
}


