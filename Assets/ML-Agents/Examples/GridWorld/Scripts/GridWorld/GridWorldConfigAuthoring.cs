using Unity.Entities;
using UnityEngine;

namespace ML_Agents.Examples.GridWorld.Scripts.GridWorld
{


    public class GridWorldConfigAuthoring : MonoBehaviour
    {
        public int gridSize = 5;
        public GameObject goalEx;
        public GameObject goalPlus;
        public GameObject agent;
        private class GridWorldConfigAuthoringBaker : Baker<GridWorldConfigAuthoring>
        {
            public override void Bake(GridWorldConfigAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.None);
                AddComponent(e,new AreaData()
                {
                    Size = authoring.gridSize,
                    GoalEx = GetEntity(authoring.goalEx,TransformUsageFlags.Dynamic),
                    GoalPlus = GetEntity(authoring.goalPlus,TransformUsageFlags.Dynamic),
                    Agent = GetEntity(authoring.agent,TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}

