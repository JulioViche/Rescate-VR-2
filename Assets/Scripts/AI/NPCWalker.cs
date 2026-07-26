using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCWalker : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float wanderRadius = 15f;
    [SerializeField] private float minIdleTime = 1.5f;
    [SerializeField] private float maxIdleTime = 4f;
    [SerializeField] private float arrivalThreshold = 0.3f;

    private NavMeshAgent agent;
    private float idleTimer;
    private bool waiting;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        if (!agent.isOnNavMesh)
        {
            Debug.LogError("NPCWalker: El NPC no está sobre el NavMesh.");
            return;
        }

        PickNewDestination();
    }

    private void Update()
    {
        // Comprobar que el NPC siga estando sobre el NavMesh
        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning("NPCWalker: El NPC ya no está sobre el NavMesh.");
            return;
        }

        // Si está esperando, contar el tiempo
        if (waiting)
        {
            idleTimer -= Time.deltaTime;

            if (idleTimer <= 0f)
            {
                waiting = false;
                PickNewDestination();
            }

            return;
        }

        // Esperar a que el agente termine de calcular la ruta
        if (agent.pathPending)
            return;

        // Comprobar si llegó al destino
        if (agent.remainingDistance <= arrivalThreshold)
        {
            waiting = true;
            idleTimer = Random.Range(minIdleTime, maxIdleTime);
        }
    }

    private void PickNewDestination()
    {
        for (int i = 0; i < 10; i++)
        {
            // Crear una posición aleatoria alrededor del NPC
            Vector3 randomPosition =
                transform.position +
                Random.insideUnitSphere * wanderRadius;

            // Buscar el punto más cercano que esté sobre el NavMesh
            if (NavMesh.SamplePosition(
                randomPosition,
                out NavMeshHit hit,
                wanderRadius,
                NavMesh.AllAreas))
            {
                // Asignar el nuevo destino al NavMeshAgent
                agent.SetDestination(hit.position);

                return;
            }
        }

        Debug.LogWarning(
            "NPCWalker: No se encontró un destino válido en el NavMesh."
        );
    }
}
