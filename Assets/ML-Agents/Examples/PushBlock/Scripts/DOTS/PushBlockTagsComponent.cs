using Unity.Collections;
using Unity.Entities;
using Unity.Physics;

namespace ML_Agents.Examples.PushBlock.Scripts.DOTS
{
    public struct PushBlockAreaTagsComponent : IComponentData
    {
        public int Index;
        public Entity Agent;
        public Entity Block;
    }
    public struct PushBlockAgentTagsComponent : IComponentData
    {
        public Entity Block;
        public int Index;
    }
    public struct PushBlockBlockTagsComponent : IComponentData
    {
        public int Index;
    }
    public struct PushBlockGoalTagsComponent : IComponentData
    {

    }

    public struct BlockGoalCollisionSignal : IComponentData
    {

    }
}
