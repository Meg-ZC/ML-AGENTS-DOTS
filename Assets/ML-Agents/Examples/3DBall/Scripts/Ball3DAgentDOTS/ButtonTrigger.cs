using System;
using UnityEngine;

namespace ML_Agents.Examples._3DBall.Scripts.Ball3DAgentDOTS
{
    public class ButtonTrigger : MonoBehaviour
    {
        public bool EnablePhysics;
        public bool Toggle;

        public void Start()
        {
            EnablePhysics = true;
        }

        void OnGUI()
        {
            if (EnablePhysics)
            {
                if (GUI.Button(new Rect(10, 10, 150, 80), "Disable Physics"))
                {
                    Toggle = true;
                }
            }
            else
            {
                if (GUI.Button(new Rect(10, 10, 150, 80), "Enable Physics"))

                {
                    Toggle = true;
                }
            }
        }
    }
}
