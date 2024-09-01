using Unity.Burst;
using Unity.Entities;
using Unity.MLAgents;
using Unity.Physics.Systems;

namespace ML_Agents.Examples.GridWorld.Scripts.GridWorld
{
    [UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
    public partial struct AcademyFixedUpdate : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AreaData>();
        }

        public void OnUpdate(ref SystemState state)
        {
            Academy.Instance.EnvironmentStep();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}
