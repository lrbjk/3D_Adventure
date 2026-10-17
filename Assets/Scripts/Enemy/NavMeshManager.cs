using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Collections.Generic;

namespace Astraparasitus
{
    public class NavMeshManager : MonoBehaviour
    {
        public List<NavMeshSurface> surface = new List<NavMeshSurface>();

        void Awake()
        {
            for(int i = 0; i < transform.childCount; i++)
            {
                surface.Add(transform.GetChild(i).GetComponent<NavMeshSurface>());
                surface[i].BuildNavMesh();
            }
            
            
           
        }
    }
}
