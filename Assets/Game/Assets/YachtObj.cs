using UnityEngine;

namespace Game.Assets
{
    public class YachtObj
    {
        public double V_current; // prędkość [m/s]
        public double Deg_from_north; // kąt od północy [°]
        
        public YachtPhysics yachtPhysics;
        
        private bool Initialized = false;
        
        void Start()
        {
            V_current = 0.0;
            Deg_from_north = 0.0;
            Initialized = yachtPhysics != null;
        }

        void Update()
        {
            if (!Initialized) return;
            V_current += yachtPhysics.ComputeAcceleration(V_current, Deg_from_north) * Time.deltaTime;
        }

    }
}