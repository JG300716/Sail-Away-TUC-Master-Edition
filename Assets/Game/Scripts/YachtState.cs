using UnityEngine;

namespace Game.Assets
{
    public enum YachtSailState : byte
    {
        No_Sail = 0x00,
        Grot_Only = 0x01,
        Fok_Only = 0x02,
        Grot_and_Fok = 0x03
    }
    
    public class YachtState : MonoBehaviour
    {
        [SerializeField] public double Acceleration; // przyspieszenie [m/s²]
        [SerializeField] public double V_current; // prędkość [m/s]
        [SerializeField] public double Deg_from_north; // kąt od północy [°]
        [SerializeField] public YachtSailState SailState = YachtSailState.No_Sail;
    
        public YachtPhysics yachtPhysics;
        
        private bool Initialized = false;
        
        void Start()
        {
            V_current = 0.0;
            Deg_from_north = 0.0;
            Initialized = yachtPhysics != null;
        }

        public void ApplyRotation(float deltaDeg)
        {
            Deg_from_north = (Deg_from_north + deltaDeg) % 360.0;
            if (Deg_from_north < 0) Deg_from_north += 360.0;
        }
        
        void Update()
        {
            if (!Initialized) return;
            Acceleration = yachtPhysics.ComputeAcceleration(V_current, Deg_from_north, SailState);
            V_current += Acceleration * Time.deltaTime;
            transform.Translate(Vector3.forward * (float)(V_current * Time.deltaTime));
        }

    }
}