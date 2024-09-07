using Unity.Mathematics;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ML_Agents.Examples.PushBlock.Scripts.DOTS
{
    public class FakePushBlockAgent : Agent
    {
        public static int AgentNumber
        {
            get
            {
                var g = GameObject.FindGameObjectsWithTag("agent");
                return g.Length;
            }
        }
        private EnvironmentParameters m_ResetParams;

        public bool respawnSignal;
        public float3 action2DOTS;

        protected override void Awake()
        {
            base.Awake();
            respawnSignal = false;
        }

        public override void Initialize()
        {
            m_ResetParams = Academy.Instance.EnvironmentParameters;

        }

        public void GetRandomSpawnPos()
        {
            respawnSignal = true;
        }

        public void ScoredAGoal()
        {
            AddReward(5f);

            EndEpisode();
            GetRandomSpawnPos();
        }

        public void MoveAgent(ActionSegment<int> act)
        {
            var action = act[0];

            switch (action)
            {
                case 1:
                    action2DOTS = new float3(0, 0, 1);
                    break;
                case 2:
                    action2DOTS = new float3(0, 0, -1);
                    break;
                case 3:
                    action2DOTS = new float3(0, 1, 0);
                    break;
                case 4:
                    action2DOTS = new float3(0, -1, 0);
                    break;
                case 5:
                    action2DOTS = new float3(-0.75f, 0, 0);
                    break;
                case 6:
                    action2DOTS = new float3(0.75f, 0, 0);
                    break;
                default:
                    action2DOTS = float3.zero;
                    break;
            }
        }

        public override void OnActionReceived(ActionBuffers actionBuffers)

        {
            MoveAgent(actionBuffers.DiscreteActions);

            AddReward(-1f / MaxStep);
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var discreteActionsOut = actionsOut.DiscreteActions;
            if (Input.GetKey(KeyCode.D))
            {
                discreteActionsOut[0] = 3;
            }
            else if (Input.GetKey(KeyCode.W))
            {
                discreteActionsOut[0] = 1;
            }
            else if (Input.GetKey(KeyCode.A))
            {
                discreteActionsOut[0] = 4;
            }
            else if (Input.GetKey(KeyCode.S))
            {
                discreteActionsOut[0] = 2;
            }
        }

        public override void OnEpisodeBegin()
        {
            respawnSignal = true;
        }
    }
}
