using System;
using Unity.Collections;
using Unity.MLAgents.Sensors;
using Unity.Physics;
using Unity.Physics.Authoring;
using UnityEngine;
using UnityEngine.Serialization;

namespace ML_Agents.Examples.PushBlock.Scripts.DOTS
{
    public abstract class RayPerceptionSensorDOTSComponentBaseDOTS : SensorComponent
    {
        [SerializeField, FormerlySerializedAs("sensorName")]
        string m_SensorName = "RayPerceptionSensorDOTSDOTS";

        private bool m_Init = false;

        public bool IsInit => m_Init;

        public string SensorName
        {
            get { return m_SensorName; }
            set { m_SensorName = value; }
        }

        [SerializeField, FormerlySerializedAs("detectableTags")]
        [Tooltip("List of tags in the scene to compare against.")]
        private CustomPhysicsMaterialTags m_DetectableTags;

        public CustomPhysicsMaterialTags DetectableTags
        {
            get => m_DetectableTags;
            set => m_DetectableTags = value;
        }
        [SerializeField, FormerlySerializedAs("raysPerDirection")]
        [Range(0, 50)]
        [Tooltip("Number of rays to the left and right of center.")]
        int m_RaysPerDirection = 3;

        public int RaysPerDirection
        {
            get { return m_RaysPerDirection; }
            // Note: can't change at runtime
            set { m_RaysPerDirection = value; }
        }

        [SerializeField, FormerlySerializedAs("maxRayDegrees")]
        [Range(0, 180)]
        [Tooltip("Cone size for rays. Using 90 degrees will cast rays to the left and right. " +
                 "Greater than 90 degrees will go backwards.")]
        float m_MaxRayDegrees = 70;

        public float MaxRayDegrees
        {
            get => m_MaxRayDegrees;
            set { m_MaxRayDegrees = value; UpdateSensor(); }
        }

        [SerializeField, FormerlySerializedAs("rayLength")]
        [Range(1, 1000)]
        [Tooltip("Length of the rays to cast.")]
        float m_RayLength = 20f;

        public float RayLength
        {
            get => m_RayLength;
            set { m_RayLength = value; UpdateSensor(); }
        }

        [SerializeField, FormerlySerializedAs("rayLayerMask")]
        [Tooltip("Controls which layers the rays can hit.")]
        private PhysicsCategoryNames m_RayLayerMask;

        /// <summary>
        /// Controls which layers the rays can hit.
        /// </summary>
        public PhysicsCategoryNames RayLayerMask
        {
            get => m_RayLayerMask;
            set { m_RayLayerMask = value; UpdateSensor(); }
        }

        [SerializeField, FormerlySerializedAs("observationStacks")]
        [Range(1, 50)]
        [Tooltip("Number of raycast results that will be stacked before being fed to the neural network.")]
        int m_ObservationStacks = 1;

        public int ObservationStacks
        {
            get { return m_ObservationStacks; }
            set { m_ObservationStacks = value; }
        }


        [SerializeField]
        [Tooltip("Disable to provide the rays in left to right order.  Warning: Alternating order will be deprecated, disable it to ensure compatibility with future versions of ML-Agents.")]
        public bool m_AlternatingRayOrder = true;

        public bool AlternatingRayOrder
        {
            get { return m_AlternatingRayOrder; }
            set { m_AlternatingRayOrder = value; }
        }

        [NonSerialized]
        RayPerceptionSensorDOTS m_RaySensor;
        public RayPerceptionSensorDOTS RaySensor => m_RaySensor;

        public virtual float GetEndVerticalOffset()
        {
            return 0f;
        }
        public virtual float GetStartVerticalOffset()
        {
            return 0f;
        }

        public override ISensor[] CreateSensors()
        {
            var rayPerceptionInput = GetRayPerceptionInput();
            m_Init = true;

            m_RaySensor = new RayPerceptionSensorDOTS(m_SensorName, rayPerceptionInput);

            if (ObservationStacks != 1)
            {
                var stackingSensor = new StackingSensor(m_RaySensor, ObservationStacks);
                return new ISensor[] { stackingSensor };
            }

            return new ISensor[] { m_RaySensor };
        }
        internal static float[] GetRayAnglesAlternating(int raysPerDirection, float maxRayDegrees)
        {
            // Example:
            // { 90, 90 - delta, 90 + delta, 90 - 2*delta, 90 + 2*delta }
            var anglesOut = new float[2 * raysPerDirection + 1];
            var delta = maxRayDegrees / raysPerDirection;
            anglesOut[0] = 90f;
            for (var i = 0; i < raysPerDirection; i++)
            {
                anglesOut[2 * i + 1] = 90 - (i + 1) * delta;
                anglesOut[2 * i + 2] = 90 + (i + 1) * delta;
            }
            return anglesOut;
        }
        internal static float[] GetRayAngles(int raysPerDirection, float maxRayDegrees)
        {
            // Example:
            // { 90 - 3*delta, 90 - 2*delta, ..., 90, 90 + delta, ..., 90 + 3*delta }
            var anglesOut = new float[2 * raysPerDirection + 1];
            var delta = maxRayDegrees / raysPerDirection;

            for (var i = 0; i < 2 * raysPerDirection + 1; i++)
            {
                anglesOut[i] = 90 + (i - raysPerDirection) * delta;
            }

            return anglesOut;
        }
        public RayPerceptionInput GetRayPerceptionInput()
        {
            var rayAngles = m_AlternatingRayOrder ?
                GetRayAnglesAlternating(RaysPerDirection, MaxRayDegrees) :
                GetRayAngles(RaysPerDirection, MaxRayDegrees);

            var rayPerceptionInput = new RayPerceptionInput();
            rayPerceptionInput.RayLength = RayLength;
            rayPerceptionInput.DetectableTags = DetectableTags;
            rayPerceptionInput.Angles = new NativeArray<float>(rayAngles, Allocator.Persistent);
            rayPerceptionInput.StartOffset = GetStartVerticalOffset();
            rayPerceptionInput.EndOffset = GetEndVerticalOffset();

            return rayPerceptionInput;
        }

        internal void UpdateSensor()
        {
            if (m_RaySensor != null)
            {
                var rayInput = GetRayPerceptionInput();
                m_RaySensor.SetRayPerceptionInput(rayInput);
            }
        }

        private void OnDestroy()
        {
            m_RaySensor.Dispose();
        }
    }
}
