using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Random = System.Random;
using RaycastHit = Unity.Physics.RaycastHit;

namespace ML_Agents.Examples.PushBlock.Scripts.DOTS
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSimulationGroup))]
    public partial class PushBlockMultiSystem : SystemBase
    {
        private Random m_Random;
        protected override void OnCreate()
        {
            base.OnCreate();
            m_Random = new Random(1);
            RequireForUpdate<PushBlockAgentTagsComponent>();
        }

        protected override void OnUpdate()
        {
            var agentConfig = SystemAPI.GetSingleton<PushBlockConfigComponent>();
            var go = GameObject.FindGameObjectsWithTag("agent");
            var goCount = go.Length;
            var raySample = go[0].GetComponents<RayPerceptionSensorComponentDOTS>();
            var rayPerGo = raySample.Length;
            var components = go.Select(g => g.GetComponents<RayPerceptionSensorComponentDOTS>()).ToArray();
            var componentsPerAgent = components[0].Length;

            var angle = raySample[0].GetRayPerceptionInput().Angles;
            var startOffset = raySample[0].GetRayPerceptionInput().StartOffset;
            var endOffset = raySample[0].GetRayPerceptionInput().EndOffset;
            var rayLength = raySample[0].GetRayPerceptionInput().RayLength;
            var detectableTags = raySample[0].GetRayPerceptionInput().DetectableTags;

            NativeArray<RaycastHit> raycastHits =
                new NativeArray<RaycastHit>(goCount * componentsPerAgent * angle.Length, Allocator.Persistent);
            var offsetArray = new float2x2(new float2(components[0][0].StartVerticalOffset, components[0][0].EndVerticalOffset),
                new float2(components[0][1].StartVerticalOffset, components[0][1].EndVerticalOffset));

            var job = new RayJob()
            {
                LT = GetComponentLookup<LocalTransform>(),
                PhysicsWorld = SystemAPI.GetSingletonRW<PhysicsWorldSingleton>().ValueRW.PhysicsWorld,
                Angles = angle,
                RayLength = rayLength,
                DetectableTags = detectableTags,
                RayOutputs = raycastHits,
                ComponentPerAgent = componentsPerAgent,
                Offset = offsetArray
            };
            Dependency = job.Schedule(Dependency);

            raycastHits.Dispose(Dependency);

        }

    }

    public partial struct RayJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> LT;
        [ReadOnly] public PhysicsWorld PhysicsWorld;
        public NativeArray<float> Angles;
        public float RayLength;
        public CustomPhysicsMaterialTags DetectableTags;

        public NativeArray<RaycastHit> RayOutputs;
        public int ComponentPerAgent;
        public float2x2 Offset;
        private void Execute(RefRO<PushBlockAreaTagsComponent> area)
        {
            var agentLT = LT[area.ValueRO.Agent];
            for (int j = 0; j < ComponentPerAgent; j++)
            {
                for (int i = 0; i < Angles.Length; i++)
                {
                    var temLT = agentLT;
                    temLT = temLT.RotateY(Mathf.Deg2Rad * (Angles[i] - 90));

                    RaycastInput input = new RaycastInput()
                    {
                        Start = temLT.Position + new float3(0,Offset[j][0],0),
                        End = temLT.Position + new float3(0,Offset[j][1],0) + temLT.Forward() * RayLength,
                        Filter = new CollisionFilter()
                        {
                            BelongsTo = (uint)~DetectableTags.Value,
                            CollidesWith = DetectableTags.Value,
                            GroupIndex = 0
                        }
                    };

                    Debug.Log($"offset = {Offset[j]}");
                    var isHit = PhysicsWorld.CastRay(input, out RaycastHit hit);
                    hit.Fraction = isHit ? hit.Fraction : 1f;
                    RayOutputs[area.ValueRO.Index * Angles.Length * 2 + j * Angles.Length + i] = hit;

                    var tag = hit.Material.CustomTags;

                    Color color = Color.black;
                    if(tag == 4) color = Color.green;
                    if(tag == 8) color = Color.red;

                    Debug.DrawRay(input.Start, isHit?(hit.Position - input.Start):(input.End - input.Start),color);

                }
            }

        }
    }


}
