using System;
using Unity.MLAgents;
using UnityEngine;

namespace ML_Agents.Examples._3DBall.Scripts
{
    public class TimeScaler : MonoBehaviour
    {
        public float TimeScale = 1f;

        private void Start()
        {
            Time.timeScale = TimeScale;
        }
    }
}
