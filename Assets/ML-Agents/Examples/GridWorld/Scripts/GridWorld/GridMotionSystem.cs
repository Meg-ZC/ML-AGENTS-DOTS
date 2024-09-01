using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.MLAgents;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;


namespace ML_Agents.Examples.GridWorld.Scripts.GridWorld
{

    [UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
    public partial struct GridMotionSystem : ISystem
    {
        Random m_Random;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AreaData>();
            m_Random = new Random(1);
        }

        public void OnUpdate(ref SystemState state)
        {
            var agent = GameObject.Find("FakeAgent").GetComponent<FakeAgent>();
            var areaData = SystemAPI.GetSingleton<AreaData>();

            bool OnEpisodeBegin = agent.OnEpisodeBeginCalled;
            bool onActionReceived = agent.OnActionReceivedCalled;

            if (OnEpisodeBegin)
            {
                agent.OnEpisodeBeginCalled = ResetEnv(areaData,ref state);
                return;
            }
            else if (onActionReceived)
            {
                agent.OnActionReceivedCalled = false;
                var (reward,pos) = ActionReceived(agent.ActionReceivedValue,agent.CurrentGoal,ref state);
                if (Mathf.Abs(reward) > 0.5f)
                {
                    agent.EndEpisode();
                    Academy.Instance.EnvironmentStep();
                }
                else
                {
                    agent.SetReward(reward);
                    agent.currentPos = pos;
                }
            }
            agent.RequestDecision();

        }

        bool ResetEnv(AreaData areaData,ref SystemState state)
        {
            var haveAgent = SystemAPI.TryGetSingletonEntity<AgentData>(out var agent);
            var haveGoalEx = SystemAPI.TryGetSingletonEntity<GoalEXData>(out var goalEx);
            var haveGoalPlus = SystemAPI.TryGetSingletonEntity<GoalPlusData>(out var goalPlus);
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            #region create entities
            if (!haveAgent)
            {
                agent = ecb.Instantiate(areaData.Agent);
                ecb.AddComponent<AgentData>(agent);
            }
            if (!haveGoalEx)
            {
                agent = ecb.Instantiate(areaData.GoalEx);
                ecb.AddComponent<GoalEXData>(agent);
            }
            if (!haveGoalPlus)
            {
                agent = ecb.Instantiate(areaData.GoalPlus);
                ecb.AddComponent<GoalPlusData>(agent);
            }

            ecb.Playback(state.EntityManager);
            if(!(haveAgent && haveGoalEx && haveGoalPlus))
                return true;
            #endregion


            ecb.Dispose();
            ecb = new EntityCommandBuffer(Allocator.Temp);

            #region set positions

            NativeHashSet<int2> positions = new NativeHashSet<int2>(3, Allocator.Temp);

            while (positions.Count < 3)
            {
                int2 pos = m_Random.NextInt2(-2, 3);
                positions.Add(pos);
            }

            var posArray = positions.ToNativeArray(Allocator.Temp);
            // Debug.Log($"posArray[0] = {posArray[0]}");
            // Debug.Log($"posArray[1] = {posArray[1]}");
            // Debug.Log($"posArray[2] = {posArray[2]}");
            ecb.SetComponent(agent,new LocalTransform(){Position = new float3(posArray[0].x,0,posArray[0].y),Rotation = quaternion.identity,Scale = 1f});
            ecb.SetComponent(goalPlus,new LocalTransform(){Position = new float3(posArray[1].x,0,posArray[1].y),Rotation = quaternion.identity,Scale = 1f});
            ecb.SetComponent(goalEx,new LocalTransform(){Position = new float3(posArray[2].x,0,posArray[2].y),Rotation = quaternion.identity,Scale = 1f});

            ecb.Playback(state.EntityManager);

            #endregion

            return false;
        }

        (float,float3) ActionReceived(int action,FakeAgent.GridGoal goal,ref SystemState state)
        {
            var agent = SystemAPI.GetSingletonEntity<AgentData>();
            var localTransform = SystemAPI.GetComponent<LocalToWorld>(agent).Position;
            switch (action)
            {
                case FakeAgent.k_NoAction:
                    // do nothing
                    break;
                case FakeAgent.k_Right:
                    localTransform += new float3(1f, 0, 0f);
                    break;
                case FakeAgent.k_Left:
                    localTransform += new float3(-1f, 0, 0f);
                    break;
                case FakeAgent.k_Up:
                    localTransform += new float3(0f, 0, 1f);
                    break;
                case FakeAgent.k_Down:
                    localTransform += new float3(0f, 0, -1f);
                    break;
                default:
                    Debug.Log("wrong action");
                    break;
            }
            localTransform.x = Mathf.Clamp(localTransform.x, -2f, 2f);
            localTransform.z = Mathf.Clamp(localTransform.z, -2f, 2f);
            SystemAPI.SetComponent(agent,new LocalTransform(){Position = localTransform,Scale = 1,Rotation = quaternion.identity});

            var goalEx = SystemAPI.GetSingletonEntity<GoalEXData>();
            var goalPlus = SystemAPI.GetSingletonEntity<GoalPlusData>();
            var goalExPos = SystemAPI.GetComponent<LocalToWorld>(goalEx).Position;
            var goalPlusPos = SystemAPI.GetComponent<LocalToWorld>(goalPlus).Position;

            if (math.distance(goalExPos,localTransform) < 0.1f)
            {
                return (goal == FakeAgent.GridGoal.RedEx ? 1f : -1f,localTransform);
            }
            else if (math.distance(goalPlusPos,localTransform) < 0.1f)
            {
                return (goal == FakeAgent.GridGoal.GreenPlus ? 1f : -1f,localTransform);
            }

            return (-0.01f, localTransform);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}
