using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
namespace Aircraft
{
    public class AircraftAcademy : Academy
    {
        public float CheckpointRadius { get; private set; }

        public override void InitializeAcademy()
        {
            FloatProperties.RegisterCallback("checkpoint_radius", f =>
            {
                CheckpointRadius = f;
            });
        }
    }
}
