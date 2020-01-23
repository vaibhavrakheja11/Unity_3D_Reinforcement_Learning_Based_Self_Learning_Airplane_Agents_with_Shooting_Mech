using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Aircraft
{
    public class AircraftPlayer : AircraftAgent
    {
        // Start is called before the first frame update
        [Header("Input Binding")]
        public InputAction pitchInput;
        public InputAction yawInput;
        public InputAction boostInput;
        public InputAction pauseInput;



        public override void InitializeAgent()
        {
            base.InitializeAgent();
            yawInput.Enable();
            pitchInput.Enable();
            boostInput.Enable();
            pauseInput.Enable();
        }

        //reads player input and converts into a vector action array;
        public override float[] Heuristic()
        {
            float pitchValue = Mathf.Round(pitchInput.ReadValue<float>());
            float yawValue = Mathf.Round(yawInput.ReadValue<float>());
            float boostValue = Mathf.Round(boostInput.ReadValue<float>());


            if (pitchValue == -1f) pitchValue = 2f;

            if (yawValue == -1f) yawValue = 2f;

            return new float[] { pitchValue, yawValue, boostValue };

        }

        private void OnDestroy()
        {
            yawInput.Disable();
            pitchInput.Disable();
            boostInput.Disable();
            pauseInput.Disable();
        }
    }
}
