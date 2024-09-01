using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace ML_Agents.Examples._3DBall.Scripts.Ball3DAgentDOTS
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct InitSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Ball3D>();
            state.RequireForUpdate<ConfigComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;
            var config = SystemAPI.GetSingleton<ConfigComponent>();
            var cube = state.EntityManager.Instantiate(config.cube);
            var ball = state.EntityManager.Instantiate(config.ball);
            // state.EntityManager.AddComponentData(ball,new Parent(){Value = cube});
            // SystemAPI.SetComponent(ball,new LocalTransform());
            state.EntityManager.AddComponentData(cube,new Ball3DComponent(){ball = ball});

            state.EntityManager.CreateSingleton<InitTag>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}
