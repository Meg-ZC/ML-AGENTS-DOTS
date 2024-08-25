using Unity.Collections;
using Unity.Mathematics;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ML_Agents.Examples._3DBall.Scripts.Ball3DAgentDOTS
{
    public class Ball3DDOTSAgent : Agent
    {
        [Tooltip("Whether to use vector observation. This option should be checked " +
            "in 3DBall scene, and unchecked in Visual3DBall scene. ")]
        public bool useVecObs;
        // Rigidbody m_BallRb;
        EnvironmentParameters m_ResetParams;
        public NativeArray<float3> Observations;//0 GO.transform.rotation 1 GO.transform.position
                                            //2.ball.transform.position 3.ball.velocity
        public NativeArray<float> Actions;//0 GO.transform.rotation.z 1 GO.transform.rotation.x
        public bool ActionsFlag = false;
        public bool EpisodeBeginFlag = false;
        public override void Initialize()
        {
            Observations = new NativeArray<float3>(4, Allocator.Persistent);
            Actions = new NativeArray<float>(2, Allocator.Persistent);
            m_ResetParams = Academy.Instance.EnvironmentParameters;
        }

        protected override void Awake()
        {
            base.Awake();

            Physics.simulationMode = SimulationMode.Script;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Observations.Dispose();
            Actions.Dispose();
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            if (useVecObs)
            {
                sensor.AddObservation(Observations[0].z);
                sensor.AddObservation(Observations[0].x);
                sensor.AddObservation(Observations[2] - Observations[1]);
                sensor.AddObservation(Observations[3]);
            }
        }

        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            Actions[0] = actionBuffers.ContinuousActions[0];
            Actions[1] = actionBuffers.ContinuousActions[1];
            ActionsFlag = true;

            var continuousActionsOut = actionBuffers.ContinuousActions;
            // Debug.Log(new float2(continuousActionsOut[0], continuousActionsOut[1]));
        }

        public override void OnEpisodeBegin()//reset cube's rotation, ball's position and velocity
        {
            EpisodeBeginFlag = true;
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var continuousActionsOut = actionsOut.ContinuousActions;
            continuousActionsOut[0] = -Input.GetAxis("Horizontal");
            continuousActionsOut[1] = Input.GetAxis("Vertical");
            ActionsFlag = true;

        }

    }
}
