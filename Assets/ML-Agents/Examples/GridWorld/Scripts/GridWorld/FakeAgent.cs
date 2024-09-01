using System;
using System.Linq;
using Unity.Mathematics;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace ML_Agents.Examples.GridWorld.Scripts.GridWorld
{
    public class FakeAgent : Agent
{
    [FormerlySerializedAs("m_Area")]
    [Header("Specific to GridWorld")]
    public float timeBetweenDecisionsAtInference;
    float m_TimeSinceDecision;

    public bool OnEpisodeBeginCalled = true;
    public bool OnActionReceivedCalled = false;
    public bool OnObservationReceivedCalled = true;

    public int ActionReceivedValue;
    public float3 currentPos;

    VectorSensorComponent m_GoalSensor;

    public enum GridGoal
    {
        GreenPlus,
        RedEx,
    }

    // Visual representations of the agent. Both are blue on top, but different colors on the bottom - this
    // allows the user to see which corresponds to the current goal, but it's not visible to the camera.
    // Only one is active at a time.

    GridGoal m_CurrentGoal;

    public GridGoal CurrentGoal
    {
        get { return m_CurrentGoal; }
        set
        {
            switch (value)
            {
                case GridGoal.GreenPlus:
                    break;
                case GridGoal.RedEx:
                    break;
            }
            m_CurrentGoal = value;
        }
    }

    [Tooltip("Selecting will turn on action masking. Note that a model trained with action " +
        "masking turned on may not behave optimally when action masking is turned off.")]
    public bool maskActions = true;

    public const int k_NoAction = 0;  // do nothing!
    public const int k_Up = 1;
    public const int k_Down = 2;
    public const int k_Left = 3;
    public const int k_Right = 4;

    EnvironmentParameters m_ResetParams;

    public override void Initialize()
    {
        m_GoalSensor = this.GetComponent<VectorSensorComponent>();
        m_ResetParams = Academy.Instance.EnvironmentParameters;
        ActionReceivedValue = k_NoAction;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Array values = Enum.GetValues(typeof(GridGoal));

        if (m_GoalSensor is object)
        {
            int goalNum = (int)CurrentGoal;
            m_GoalSensor.GetSensor().AddOneHotObservation(goalNum, values.Length);
        }
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        // Mask the necessary actions if selected by the user.
        if (maskActions)
        {
            // Prevents the agent from picking an action that would make it collide with a wall
            var positionX = (int)currentPos.x;
            var positionZ = (int)currentPos.z;

            if (positionX == -2)
            {
                actionMask.SetActionEnabled(0, k_Left, false);
            }

            if (positionX == 2)
            {
                actionMask.SetActionEnabled(0, k_Right, false);
            }

            if (positionZ == -2)
            {
                actionMask.SetActionEnabled(0, k_Down, false);
            }

            if (positionZ == 2)
            {
                actionMask.SetActionEnabled(0, k_Up, false);
            }
        }
    }

    // to be implemented by the developer
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        OnActionReceivedCalled = true;
        ActionReceivedValue = actionBuffers.DiscreteActions[0];
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        OnActionReceivedCalled = true;
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = k_NoAction;
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[0] = k_Right;
        }
        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = k_Up;
        }
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[0] = k_Left;
        }
        if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = k_Down;
        }

        ActionReceivedValue = discreteActionsOut[0];
    }

    // to be implemented by the developer
    public override void OnEpisodeBegin()
    {
        OnEpisodeBeginCalled = true;
        Array values = Enum.GetValues(typeof(GridGoal));
        if (m_GoalSensor is object)
        {
            CurrentGoal = (GridGoal)values.GetValue(UnityEngine.Random.Range(0, values.Length));
        }
        else
        {
            CurrentGoal = GridGoal.GreenPlus;
        }
    }

    // public void FixedUpdate()
    // {
    //     WaitTimeInference();
    // }

    void WaitTimeInference()
    {
        if (Academy.Instance.IsCommunicatorOn)
        {
            RequestDecision();
        }
        else
        {
            if (m_TimeSinceDecision >= timeBetweenDecisionsAtInference)
            {
                m_TimeSinceDecision = 0f;
                RequestDecision();
            }
            else
            {
                m_TimeSinceDecision += Time.fixedDeltaTime;
            }
        }
    }
}
}
