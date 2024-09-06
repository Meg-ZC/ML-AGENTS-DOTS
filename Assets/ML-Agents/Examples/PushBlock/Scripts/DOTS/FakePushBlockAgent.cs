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

        /// <summary>
        /// Use the ground's bounds to pick a random spawn position.
        /// </summary>
        public void GetRandomSpawnPos()
        {
            respawnSignal = true;
        }

        /// <summary>
        /// Called when the agent moves the block into the goal.
        /// </summary>
        public void ScoredAGoal()
        {
            // We use a reward of 5.
            AddReward(5f);

            // By marking an agent as done AgentReset() will be called automatically.
            EndEpisode();
            GetRandomSpawnPos();
        }

        /// <summary>
        /// Moves the agent according to the selected action.
        /// </summary>
        public void MoveAgent(ActionSegment<int> act)
        {
            var action = act[0];
            // Debug.Log($"stepnum: {StepCount}");

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

        /// <summary>
        /// Called every step of the engine. Here the agent takes an action.
        /// </summary>
        public override void OnActionReceived(ActionBuffers actionBuffers)

        {
            // Move the agent using the action.
            MoveAgent(actionBuffers.DiscreteActions);
            // Debug.Log(actionBuffers.DiscreteActions);

            // Penalty given each step to encourage agent to finish task quickly.
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

        /// <summary>
        /// In the editor, if "Reset On Done" is checked then AgentReset() will be
        /// called automatically anytime we mark done = true in an agent script.
        /// </summary>
        public override void OnEpisodeBegin()
        {
            respawnSignal = true;
        }


    }
}
