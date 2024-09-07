using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Serialization;

namespace ML_Agents.Examples.PushBlock.Scripts.DOTS
{
    public class RayPerceptionSensorComponentDOTS : RayPerceptionSensorDOTSComponentBaseDOTS
    {
        [SerializeField, FormerlySerializedAs("startVerticalOffset")]
        [Range(-10f, 10f)]
        [Tooltip("Ray start is offset up or down by this amount.")]
        float m_StartVerticalOffset;

        /// <summary>
        /// Ray start is offset up or down by this amount.
        /// </summary>
        public float StartVerticalOffset
        {
            get => m_StartVerticalOffset;
            set { m_StartVerticalOffset = value; UpdateSensor(); }
        }

        [SerializeField, FormerlySerializedAs("endVerticalOffset")]
        [Range(-10f, 10f)]
        [Tooltip("Ray end is offset up or down by this amount.")]
        float m_EndVerticalOffset;

        /// <summary>
        /// Ray end is offset up or down by this amount.
        /// </summary>
        public float EndVerticalOffset
        {
            get => m_EndVerticalOffset;
            set { m_EndVerticalOffset = value; UpdateSensor(); }
        }

        public override float GetStartVerticalOffset()
        {
            return StartVerticalOffset;
        }

        public override float GetEndVerticalOffset()
        {
            return EndVerticalOffset;
        }
    }
}
