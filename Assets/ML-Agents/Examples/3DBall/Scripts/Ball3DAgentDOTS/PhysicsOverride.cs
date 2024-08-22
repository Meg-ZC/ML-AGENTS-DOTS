using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;

namespace ML_Agents.Examples._3DBall.Scripts.Ball3DAgentDOTS
{
    public partial struct PhysicsOverride : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsStep>();
        }

        // [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var stepComponent = SystemAPI.GetSingleton<PhysicsStep>();
            var com = GameObject.Find("Button").GetComponent<ButtonTrigger>();

            if (com.EnablePhysics && com.Toggle)
            {
                stepComponent.SimulationType = SimulationType.NoPhysics;
                com.EnablePhysics = false;
                com.Toggle = false;
                SystemAPI.SetSingleton(stepComponent);
            }
            else if (com.EnablePhysics == false && com.Toggle)
            {
                stepComponent.SimulationType = SimulationType.HavokPhysics;
                com.EnablePhysics = true;
                com.Toggle = false;
                SystemAPI.SetSingleton(stepComponent);
            }

            // Debug.Log($"{stepComponent.SimulationType.ToString()}");
        }
    }
}
