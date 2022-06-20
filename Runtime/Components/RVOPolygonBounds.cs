using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace TiercelFoundry.RVO2
{
    public class RVOPolygonBounds : MonoBehaviour
    {
        [SerializeField] RVOManager manager;
        [SerializeField] List<Vector3> vertices;

        private int obstacleId;

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
            obstacleId = manager.AddObstacle(SortConcave(vertices));
        }
        private void OnDisable()
        {
            if (manager != null)
            {
                manager.RemoveObstacle(obstacleId);
            }
        }

        private List<Vector2> SortConcave(List<Vector3> vertices)
        {
            var x = vertices.Sum(v => manager.Convert(v).X()) / vertices.Count;
            var y = vertices.Sum(v => manager.Convert(v).Y()) / vertices.Count;

            var center = new Vector2(x, y);
            var p0 = Vector2.Up - center;

            return vertices.Select(v => manager.Convert(v))
                .OrderBy(s => Vector2.Angle(p0, s - center) + s.X() > center.X() ? System.MathF.PI : 0f) // ccw
                .ToList();
        }

        private void OnGizmosSelected() 
        {
            Gizmos.Color = Color.Cyan;
            for (int i = 1; i < vertices.Count; i++) 
            {
                Gizmos.DrawLine(vertices[i - 1], vertices[i]);
            }
            Gizmos.DrawLine(vertices[vertices.Count - 1], vertices[0]);
        }
    }
}

