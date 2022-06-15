using UnityEngine;
using Random = System.Random;


namespace TiercelFoundry.RVO2
{
    public class RVOAgent : MonoBehaviour
    {
        [HideInInspector] public int sid = -1;
        [HideInInspector] public Vector3 goalPosition;
        public Vector3 ScaledVelocity { get; private set; }


        public bool overrideAgentDefaults;
        public float neighborDistance = 15f;
        public int maxNeighborCount = 10;
        public float agentTimeHorizon = 5.0f;
        public float obstacleTimeHorizon = 5.0f;
        public float radius = 2.0f;
        public float maxSpeed = 2.0f;
        [SerializeField] float maxDistFromGoal = 0.05f;
        
        [SerializeField] RVOManager manager;
        [SerializeField] bool steerWithRVOAgent;

        /** Random number generator. */
        private readonly Random random = new Random();


        private void OnEnable()
        {
            if (manager == null)
            {
                manager = FindObjectOfType<RVOManager>();
                if (manager == null)
                {
                    enabled = false;
                    Debug.LogWarning($"No RVO Manager found for {name}. Disabling RVOAgent component. Have you added an RVO manager to the scene?");
                    return;
                }
            }
            manager.AddAgent(this);
        }
        private void OnDisable()
        {
            if (manager != null)
            {
                manager.RemoveAgent(this);
            }
        }

        public void Update()
        {
            if (sid < 0) 
                return;

            if (Vector3.Distance(transform.position, goalPosition) <= maxDistFromGoal)
            {
                manager.Simulator.SetAgentPrefVelocity(sid, new Vector2(0, 0));
                return;
            }

            Vector2 pos = manager.Simulator.GetAgentPosition(sid);
            Vector2 vel = manager.Simulator.GetAgentPrefVelocity(sid);

            var normalVelocity = new Vector3(vel.X(), 0, vel.Y()).normalized;
            var sqrMag = normalVelocity.sqrMagnitude;
            sqrMag /= (maxSpeed * maxSpeed);
            ScaledVelocity = normalVelocity * sqrMag;

            if (steerWithRVOAgent)
            {
                if (Mathf.Abs(vel.X()) > 0.01f && Mathf.Abs(vel.Y()) > 0.01f)
                {
                    transform.forward = normalVelocity;
                }
                transform.position = new Vector3(pos.X(), transform.position.y, pos.Y()); 
                // todo: reverse conversion functions to allow for more frames of reference
            }

            Vector2 goalVector = manager.Convert(goalPosition) - manager.Simulator.GetAgentPosition(sid);
            if (RVOMath.AbsSq(goalVector) > 1.0f)
            {
                goalVector = RVOMath.Normalize(goalVector);
            }

            manager.Simulator.SetAgentPrefVelocity(sid, goalVector);

            /* Perturb a little to avoid deadlocks due to perfect symmetry. */
            float angle = (float)random.NextDouble() * 2.0f * Mathf.PI;
            float dist = (float)random.NextDouble() * 0.0001f;

            manager.Simulator.SetAgentPrefVelocity(sid, manager.Simulator.GetAgentPrefVelocity(sid) +
                                                         dist *
                                                         new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
        }
    }
}

