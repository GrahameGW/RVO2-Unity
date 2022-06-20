using System.Collections.Generic;
using UnityEngine;
using Vector2 = TiercelFoundry.RVO2.Vector2;


namespace TiercelFoundry.RVO2
{
    public class RVOManager : MonoBehaviour
    {
        private static RVOManager instance;

        public Simulator Simulator { get; private set; }
        //private readonly Dictionary<int, RVOAgent> agents = new();
        private readonly List<RVOAgent> agents = new();
        private readonly List<RVOAgent> inactive = new();
        private readonly List<int> obstacles = new();
        private readonly Queue<RVOAgent> agentsToAdd = new();

        [HideInInspector]
        public CoordFrame referenceFrame;
        public delegate Vector2 ConvertVector3(Vector3 vector3);
        public ConvertVector3 Convert;
        
        [Header("Agent Defaults")]
        public float neighborDistance = 15f;
        public int maxNeighborCount = 10;
        public float agentTimeHorizon = 5.0f;
        public float obstacleTimeHorizon = 5.0f;
        public float radius = 4.0f;
        public float maxSpeed = 1.0f;



        private void Awake()
        {
            Convert = ChooseConvertFunction(referenceFrame);
            Simulator = Simulator.Instance;
            Simulator.SetTimeStep(0.25f);
            Simulator.SetAgentDefaults(15.0f, 10, 5.0f, 5.0f, 2.0f, 2.0f, new Vector2(0.0f, 0.0f));
            if (instance != null)
            {
                Debug.LogError("An instance of RVO Manager already exists. Please ensure you only have one RVOManager component in the scene.", this);
                enabled = false;
            }
            else
            {
                instance = this;
            }
        }
        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }


        public void Start()
        {
            // add obstacles in awake
            Simulator.ProcessObstacles();
            while (agentsToAdd.Count != 0)
            {
                AddAgent(agentsToAdd.Dequeue());
            }
        }

        public void Update()
        {
            Simulator.DoStep();
        }

        public void AddAgent(RVOAgent agent)
        {
            if (Convert == null || Simulator == null)
            {
                agentsToAdd.Enqueue(agent);
                return;
            }
            
            var position = Convert(agent.transform.position);
            int sid = agent.overrideAgentDefaults ?
                Simulator.AddAgent(
                    position, agent.neighborDistance, agent.maxNeighborCount,
                    agent.agentTimeHorizon, agent.obstacleTimeHorizon, agent.radius,
                    agent.maxSpeed, new Vector2(0f, 0f)
                ) :
                Simulator.AddAgent(
                    position, neighborDistance, maxNeighborCount,
                    agentTimeHorizon, obstacleTimeHorizon, radius,
                    maxSpeed, new Vector2(0f, 0f)
                );
            //agents.Add(sid, agent);
            //processQueue.Add(agent);
            agent.sid = sid;
            agents.Add(agent);
        }

        public void RemoveAgent(RVOAgent agent)
        {
            /*
            var position = Convert(agent.transform.position);
            int agentNo = simulator.QueryNearAgent(position, 0.25f);
            if (agentNo == -1 || !agents.ContainsKey(agentNo))
                return;
            agents.Remove(agentNo);
            processQueue.Remove(agent);
            simulator.DelAgent(agentNo);
            */
            //agents.Remove(agent.sid);
            //processQueue.Remove(agent);
            if (agent.sid == -1 || !agents.Contains(agent))
                return;
            agents.Remove(agent);
            Simulator.DelAgent(agent.sid);
        }

        public int AddObstacle(List<Vector2> vertices)
        {
            int id = Simulator.AddObstacle(vertices);
            obstacles.Add(id);
            return id;
        }

        public void RemoveObstacle(int id)
        {
            if (obstacles.Contains(id))
            {
                obstacles.Remove(id);
                Simulator.RemoveObstacle(id);
            }
        }
        public void DisableRVO()
        {
            foreach (var agent in agents)
            {
                agent.enabled = false;
            }
        }
        public void EnableRVO()
        {
            foreach (var agent in agents)
            {
                agent.enabled = true;
            }
        }
        public void OnReferenceFrameChanged()
        {
            Convert = ChooseConvertFunction(referenceFrame);
        }

        private ConvertVector3 ChooseConvertFunction(CoordFrame frame)
        {
            return frame switch
            {
                CoordFrame.XY => VectorConversionFunc.ToXY,
                CoordFrame.XZ => VectorConversionFunc.ToXZ,
                CoordFrame.YZ => VectorConversionFunc.ToYX,
                CoordFrame.YX => VectorConversionFunc.ToYZ,
                CoordFrame.ZY => VectorConversionFunc.ToZX,
                CoordFrame.ZX => VectorConversionFunc.ToZY,
                _ => throw new System.ArgumentException("Invalid value for RVOManager.navFrame"),
            };
        }
    }
}

