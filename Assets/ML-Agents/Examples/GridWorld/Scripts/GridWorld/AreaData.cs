using Unity.Collections;
using Unity.Entities;

namespace ML_Agents.Examples.GridWorld.Scripts.GridWorld
{
    public struct AreaData : IComponentData
    {
        public Entity GoalEx;
        public Entity GoalPlus;
        public Entity Agent;
        public int Size;
    }

    public struct GoalEXData : IComponentData{}
    public struct GoalPlusData : IComponentData{}
    public struct AgentData : IComponentData{}

}
