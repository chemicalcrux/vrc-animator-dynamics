using UnityEngine;

namespace Pursuit.Code
{
    public class TestDynamics : MonoBehaviour
    {
        [SerializeField] private SecondOrderDynamicsFloat dynamics;
        [SerializeField] private float target;

        void Awake()
        {
            dynamics.Setup();
        }
        
        void Update()
        {
            dynamics.Update(0.2f, target);
        }
    }
}