using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Content;
using Unity.Mathematics;
using Unity.MLAgents;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ML_Agents.Examples._3DBall.Scripts.Ball3DAgentDOTS
{

    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateBefore(typeof(BeforePhysicsSystemGroup))]
    public partial class AcademySystem : SystemBase
    {
        private Academy m_Academy;

        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<InitTag>();
            m_Academy = Academy.Instance;
        }

        protected override void OnUpdate()
        {
            var go = GameObject.Find("Agents");
            if (go == null)
            {
                Debug.LogWarning("no agents");
                return;
            }
            var agent = go.GetComponent<Ball3DDOTSAgent>();
            var actionReceived = agent.ActionsFlag;
            var episodeBegin = agent.EpisodeBeginFlag;


            //collect ovservations
            {
                Dependency.Complete();
                ComponentLookup<LocalTransform> ltComponentLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
                ComponentLookup<PhysicsVelocity> pvComponentLookup = SystemAPI.GetComponentLookup<PhysicsVelocity>(true);
                foreach (var (com,e) in SystemAPI.Query<RefRO<Ball3DComponent>>().WithEntityAccess())
                {
                    var ballPos = ltComponentLookup[com.ValueRO.ball];
                    var ballVel = pvComponentLookup[com.ValueRO.ball].Linear;
                    var cubeRo = ltComponentLookup[e].Rotation.value;

                    agent.Observations[0] = new float3(cubeRo.x,cubeRo.y,cubeRo.z);
                    agent.Observations[1] = ltComponentLookup[e].Position;
                    agent.Observations[2] = ballPos.Position;
                    agent.Observations[3] = ballVel;
                }
            }

            if (actionReceived)
            {
                agent.ActionsFlag = false;
                ComponentLookup<LocalTransform> ltComponentLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
                var ecb = new EntityCommandBuffer(Allocator.TempJob);
                foreach (var (LT,Co,e) in SystemAPI.Query<RefRO<LocalTransform>,RefRO<Ball3DComponent>>().WithEntityAccess())
                {
                    var actionZ = Mathf.Clamp(agent.Actions[0], -1f, 1f);
                    var actionX = Mathf.Clamp(agent.Actions[1], -1f, 1f);

                    var temRot = LT.ValueRO.WithRotation(quaternion.identity);
                    temRot = temRot.RotateX(math.radians(actionX * 25f));
                    temRot = temRot.RotateZ(math.radians(actionZ * 25f));
                    temRot = temRot.WithRotation(math.nlerp(LT.ValueRO.Rotation, temRot.Rotation,
                        SystemAPI.Time.DeltaTime));
                    var ballPos = ltComponentLookup[Co.ValueRO.ball];

                    if ((ballPos.Position.y - LT.ValueRO.Position.y) < -2f ||
                        Mathf.Abs(ballPos.Position.x - LT.ValueRO.Position.x) > 3f ||
                        Mathf.Abs(ballPos.Position.z - LT.ValueRO.Position.z) > 3f)
                    {
                        agent.SetReward(-1);
                        agent.EndEpisode();
                    }
                    else
                    {
                        agent.SetReward(1);
                    }
                    ecb.SetComponent(e,temRot);
                }

                ecb.Playback(EntityManager);
                ecb.Dispose();
            }

            if (episodeBegin)
            {
                agent.EpisodeBeginFlag = false;
                ComponentLookup<LocalTransform> ltComponentLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
                var ecb = new EntityCommandBuffer(Allocator.Temp);
                foreach (var (LT,Co,e) in SystemAPI.Query<RefRO<LocalTransform>,RefRO<Ball3DComponent>>().WithEntityAccess())
                {
                    var temRot = LT.ValueRO;
                    temRot.Rotation = quaternion.identity;
                    temRot = temRot.RotateX(Random.Range(5, 10) * Mathf.Deg2Rad);
                    temRot = temRot.RotateZ(Random.Range(5, 10) * Mathf.Deg2Rad);
                    ecb.SetComponent(e,temRot);
                    ecb.DestroyEntity(Co.ValueRO.ball);
                    var ball = ecb.Instantiate(SystemAPI.GetSingleton<ConfigComponent>().ball);
                    ecb.SetComponent(ball, new LocalTransform()
                    {
                        Position = new float3(Random.Range(-1.5f,1.5f),4,Random.Range(-1.5f,1.5f)),
                        Rotation = quaternion.identity,Scale = 1f
                    });

                    ecb.SetComponent(e,new Ball3DComponent(){ball = ball});
                }
                ecb.Playback(EntityManager);
                ecb.Dispose();
            }
        }
    }
}
