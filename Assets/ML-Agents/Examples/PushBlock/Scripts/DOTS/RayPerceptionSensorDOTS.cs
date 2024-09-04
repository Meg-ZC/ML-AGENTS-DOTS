using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.MLAgents.Sensors;
using Unity.Physics;
using Unity.Physics.Authoring;
using UnityEngine;
using RaycastHit = Unity.Physics.RaycastHit;

namespace ML_Agents.Examples.PushBlock.Scripts.DOTS
{

    public struct RayPerceptionInput
    {
        public float RayLength;
        public CustomPhysicsMaterialTags DetectableTags;
        public IReadOnlyList<float> Angles;
        public float StartOffset;
        public float EndOffset;
        public float CastRadius;
        public PhysicsCategoryNames RayLayerMask;


        public int CountTags()
        {
            int i = 0;
            var tags = DetectableTags.Value;
            for (int t = 0; t < 8; t++)
            {
                i += (tags & (1 << t)) > 0 ? 1 : 0;
            }
            return i;
        }
        public int OutputSize()
        {
            return (CountTags() + 2) * (Angles?.Count ?? 0);
        }
    }
    public class RayPerceptionOutput
    {
        public NativeArray<RaycastHit> RayOutputs;
        public void ToFloatArray(int numDetectableTags, int rayCount, float[] buffer,byte inputTags)
        {
            for(int i = 0;i < rayCount;i++)
            {
                var highOffset = i * (numDetectableTags + 2);
                if (RayOutputs[i].Entity != Entity.Null)
                {
                    var flag = (inputTags & RayOutputs[i].Material.CustomTags);
                    var pos = (int)Mathf.Log(flag,2);
                    buffer[highOffset + pos - 1] = 1f;
                    Debug.Log($"TagCount:{numDetectableTags} Tag:{RayOutputs[i].Material.CustomTags} Pos:{pos}");
                }
                buffer[highOffset + numDetectableTags] = RayOutputs[i].Entity != Entity.Null ? 0f : 1f;
                buffer[highOffset + numDetectableTags + 1] = RayOutputs[i].Fraction;
            }
        }
    }
    public class RayPerceptionSensorDOTS:ISensor
    {
        float[] m_Observations;
        ObservationSpec m_ObservationSpec;
        string m_Name;

        RayPerceptionInput m_RayPerceptionInput;
        RayPerceptionOutput m_RayPerceptionOutput;

        public RayPerceptionInput RayPerceptionInput => m_RayPerceptionInput;
        public RayPerceptionOutput RayPerceptionOutput => m_RayPerceptionOutput;

        public void Dispose()
        {
            if (m_RayPerceptionOutput.RayOutputs.IsCreated)
                m_RayPerceptionOutput.RayOutputs.Dispose();
        }

        public RayPerceptionSensorDOTS(string name, RayPerceptionInput rayPerceptionInput)
        {
            m_Name = name;
            m_RayPerceptionInput = rayPerceptionInput;
            m_RayPerceptionOutput = new RayPerceptionOutput();
            m_RayPerceptionOutput.RayOutputs = new NativeArray<RaycastHit>(m_RayPerceptionInput.Angles.Count, Allocator.Persistent);
            SetNumObservations(rayPerceptionInput.OutputSize());
        }

        void SetNumObservations(int numObservations)
        {
            m_ObservationSpec = ObservationSpec.Vector(numObservations);
            m_Observations = new float[numObservations];
        }

        public void SetRayPerceptionInput(RayPerceptionInput rayPerceptionInput)
        {
            if (m_RayPerceptionInput.OutputSize() != rayPerceptionInput.OutputSize())
            {
                Debug.Log(
                    "Changing the number of tags or rays at runtime is not " +
                    "supported and may cause errors in training or inference."
                );
                // Changing the shape will probably break things downstream, but we can at least
                // keep this consistent.
                SetNumObservations(rayPerceptionInput.OutputSize());
                if (m_RayPerceptionOutput.RayOutputs.IsCreated)
                {
                    m_RayPerceptionOutput.RayOutputs.Dispose();
                    m_RayPerceptionOutput.RayOutputs = new NativeArray<RaycastHit>(rayPerceptionInput.Angles.Count, Allocator.Persistent);
                }
            }
            m_RayPerceptionInput = rayPerceptionInput;
        }
        public ObservationSpec GetObservationSpec()
        {
            return m_ObservationSpec;
        }

        public int Write(ObservationWriter writer)
        {
            Array.Clear(m_Observations, 0, m_Observations.Length);
            var numRays = m_RayPerceptionInput.Angles.Count;
            var numDetectableTags = m_RayPerceptionInput.CountTags();

            // For each ray, write the information to the observation buffer
            if (m_RayPerceptionOutput!= null)
            {
                if(m_RayPerceptionOutput.RayOutputs.IsCreated)
                    m_RayPerceptionOutput.ToFloatArray(numDetectableTags,numRays,m_Observations,m_RayPerceptionInput.DetectableTags.Value);
            }
            Debug.Log($"RayPerceptionOutput:{m_RayPerceptionOutput.RayOutputs.Length}");

            // Finally, add the observations to the ObservationWriter
            writer.AddList(m_Observations);
            return m_Observations.Length;
        }

        public byte[] GetCompressedObservation()
        {
            return null;
        }

        public void Update()
        {
            var numRays = m_RayPerceptionInput.Angles.Count;
            if (!m_RayPerceptionOutput.RayOutputs.IsCreated || m_RayPerceptionOutput.RayOutputs.Length != numRays)
            {
                if (m_RayPerceptionOutput.RayOutputs.IsCreated)
                    m_RayPerceptionOutput.RayOutputs.Dispose();
                m_RayPerceptionOutput.RayOutputs = new NativeArray<RaycastHit>(numRays, Allocator.Persistent);
            }
        }

        public void Reset()
        {
        }

        public CompressionSpec GetCompressionSpec()
        {
            return CompressionSpec.Default();
        }

        public string GetName()
        {
            return m_Name;
        }
    }
}
