using Unity.Collections;
using Unity.Entities;
using Unity.Physics;

namespace ML_Agents.Examples.PushBlock.Scripts.DOTS
{
    public struct PushBlockAreaTagsComponent : IComponentData
    {
        public bool ReSpawn;
        public int Index;
        public Entity Agent;
        public Entity Block;
    }
    public struct PushBlockAgentTagsComponent : IComponentData
    {
        public Entity Block;
    }
    public struct PushBlockBlockTagsComponent : IComponentData
    {

    }
    public struct PushBlockGoalTagsComponent : IComponentData
    {

    }

    public struct BlockGoalCollisionSignal : IComponentData
    {

    }
}
