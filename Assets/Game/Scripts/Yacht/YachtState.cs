using UnityEngine;

namespace Game.Scripts
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
        [Header("State (Read Only)")]
        [SerializeField] public double Acceleration;   // przyśpieszenie wzdłuż kursu [m/s²]
        [SerializeField] public double V_current;      // prędkość postępowa [m/s]
        [SerializeField] public double Deg_from_north; // kurs [°]
        [SerializeField] public bool isDriving;     

        [Header("Sails")]
        [SerializeField] public YachtSailState SailState = YachtSailState.No_Sail;

        [Header("References")]
        [SerializeField] public UnifiedYachtPhysics yachtPhysics;

        private bool Initialized = true;

        void Start()
        {
            V_current = 0.0;
            Deg_from_north = transform.eulerAngles.y;
            //Initialized = yachtPhysics != null;

            if (!Initialized)
            {
                Debug.LogError("[YachtState] UnifiedYachtPhysics reference missing!");
            }
        }

        // =========================================================
        // ===================== ROTATION ==========================
        // =========================================================
        public void ApplyRotation(float deltaDeg)
        {
            Deg_from_north = (Deg_from_north + deltaDeg) % 360.0;
            if (Deg_from_north < 0.0)
                Deg_from_north += 360.0;

            transform.rotation = Quaternion.Euler(0f, (float)Deg_from_north, 0f);
        }

        // =========================================================
        // ===================== UPDATE ============================
        // =========================================================
        // void Update()
        // {
        //     if (!Initialized)
        //         return;
        //
        //     // 1. Pobierz aktualne przyśpieszenie z fizyki (świat XZ)
        //     Vector2 accelWorld = yachtPhysics.GetCurrentAcceleration2D();
        //
        //     // 2. Oblicz kierunek jachtu z kursu
        //     Vector2 forwardDir = new Vector2(
        //         Mathf.Sin((float)Deg_from_north * Mathf.Deg2Rad),
        //         Mathf.Cos((float)Deg_from_north * Mathf.Deg2Rad)
        //     ).normalized;
        //
        //     // 3. Rzut przyśpieszenia na oś jachtu (1D model)
        //     double accelForward = Vector2.Dot(accelWorld, forwardDir);
        //
        //     // 4. Zapisz przyśpieszenie
        //     Acceleration = accelForward;
        //
        //     // 5. Integracja prędkości
        //     V_current += Acceleration * Time.deltaTime;
        //
        //     // 6. Ograniczenie cofania
        //     if (V_current < -2.0)
        //         V_current = -2.0;
        //
        //     // (opcjonalnie) ruch wizualny jeśli NIE używasz Rigidbody
        //     //transform.Translate(Vector3.forward * (float)(V_current * Time.deltaTime));
        // }
    }
}
