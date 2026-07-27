using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCWalker : MonoBehaviour
{
    public enum NPCState
    {
        Idle,     // Quieto, mirando un punto
        Walking,  // Caminando hacia un destino, evitando obstaculos
        Panic     // Corriendo, cambiando de direccion con frecuencia
    }

    [Header("Estado actual (solo lectura)")]
    [SerializeField] private NPCState currentState = NPCState.Idle;

    [Header("Velocidades")]
    [SerializeField] private float walkSpeed = 1.4f;
    [SerializeField] private float panicSpeed = 4.5f;
    [SerializeField] private float walkAngularSpeed = 200f;
    [SerializeField] private float panicAngularSpeed = 600f;
    [SerializeField] private float walkAcceleration = 4f;
    [SerializeField] private float panicAcceleration = 12f;

    [Header("Caminata (Walking)")]
    [SerializeField] private float wanderRadius = 15f;
    [SerializeField] private float arrivalThreshold = 0.3f;

    [Header("Quieto (Idle)")]
    [SerializeField] private float minIdleTime = 1.5f;
    [SerializeField] private float maxIdleTime = 4f;
    [SerializeField] private float lookAroundRadius = 6f;   // distancia a la que "mira" algo
    [SerializeField] private float idleTurnSpeed = 4f;       // que tan rapido gira al mirar
    [SerializeField, Range(0f, 1f)] private float chanceToGlanceAgain = 0.35f; // vuelve a mirar otro punto sin caminar

    [Header("Panico (Panic)")]
    [SerializeField] private float panicRedirectMin = 0.6f;  // cada cuanto cambia de direccion en panico (minimo)
    [SerializeField] private float panicRedirectMax = 1.4f;  // (maximo)
    [SerializeField] private float panicWanderRadius = 10f;  // radio de busqueda de nuevo punto en panico
    [SerializeField] private float panicDuration = 6f;       // cuanto dura el panico si no se corta antes

    [Header("Referencias opcionales")]
    [SerializeField] private Animator animator;              // opcional, puede quedar vacio
    [SerializeField] private string animatorSpeedParam = "Speed";

    private NavMeshAgent agent;
    private float stateTimer;
    private float panicTimeLeft;
    private Quaternion idleTargetRotation;
    private bool hasIdleRotationTarget;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (!agent.isOnNavMesh)
        {
            Debug.LogError("NPCWalker: El NPC no esta sobre el NavMesh.", this);
            enabled = false;
            return;
        }

        // Evita que todos los peatones tengan la misma prioridad de avoidance
        // (reduce que se queden "trabados" mirandose entre ellos frente a frente)
        agent.avoidancePriority = Random.Range(10, 90);

        EnterIdle();
    }

    private void Update()
    {
        if (!agent.isOnNavMesh) return;

        switch (currentState)
        {
            case NPCState.Idle:
                TickIdle();
                break;
            case NPCState.Walking:
                TickWalking();
                break;
            case NPCState.Panic:
                TickPanic();
                break;
        }

        UpdateAnimator();
    }

    // ---------------------------------------------------------
    // IDLE: el NPC se detiene y mira hacia un punto cercano,
    // en vez de quedarse mirando a la nada como un maniqui.
    // ---------------------------------------------------------
    private void EnterIdle()
    {
        currentState = NPCState.Idle;
        agent.isStopped = true;
        agent.updateRotation = false; // rotacion manual mientras esta quieto

        stateTimer = Random.Range(minIdleTime, maxIdleTime);
        PickIdleLookTarget();
    }

    private void TickIdle()
    {
        // Girar suavemente hacia el punto que esta "mirando"
        if (hasIdleRotationTarget)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation, idleTargetRotation, Time.deltaTime * idleTurnSpeed);
        }

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            // A veces mira otro punto distinto antes de retomar la caminata,
            // para que no se sienta como un timer fijo y mecanico.
            if (Random.value < chanceToGlanceAgain)
            {
                stateTimer = Random.Range(minIdleTime, maxIdleTime) * 0.6f;
                PickIdleLookTarget();
            }
            else
            {
                agent.updateRotation = true;
                EnterWalking();
            }
        }
    }

    private void PickIdleLookTarget()
    {
        Vector3 randomPoint = transform.position +
            (Quaternion.Euler(0f, Random.Range(-160f, 160f), 0f) * transform.forward) * lookAroundRadius;

        Vector3 direction = randomPoint - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            idleTargetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            hasIdleRotationTarget = true;
        }
        else
        {
            hasIdleRotationTarget = false;
        }
    }

    // ---------------------------------------------------------
    // WALKING: camina hacia un punto aleatorio del NavMesh.
    // El NavMeshAgent se encarga de esquivar obstaculos.
    // ---------------------------------------------------------
    private void EnterWalking()
    {
        currentState = NPCState.Walking;
        agent.isStopped = false;
        agent.speed = walkSpeed;
        agent.angularSpeed = walkAngularSpeed;
        agent.acceleration = walkAcceleration;
        agent.stoppingDistance = 0f;

        PickNewDestination(wanderRadius);
    }

    private void TickWalking()
    {
        if (agent.pathPending) return;

        if (!agent.pathPending && agent.remainingDistance <= arrivalThreshold &&
            (!agent.hasPath || agent.velocity.sqrMagnitude < 0.01f))
        {
            EnterIdle();
        }
    }

    // ---------------------------------------------------------
    // PANIC: corre y cambia de direccion mas seguido, para dar
    // sensacion de descontrol en vez de una ruta calculada y prolija.
    // ---------------------------------------------------------
    public void TriggerPanic(float duration = -1f)
    {
        panicDuration = duration > 0f ? duration : panicDuration;
        EnterPanic();
    }

    private void EnterPanic()
    {
        currentState = NPCState.Panic;
        agent.isStopped = false;
        agent.updateRotation = true;
        agent.speed = panicSpeed;
        agent.angularSpeed = panicAngularSpeed;
        agent.acceleration = panicAcceleration;
        agent.stoppingDistance = 0f;

        panicTimeLeft = panicDuration;
        stateTimer = Random.Range(panicRedirectMin, panicRedirectMax);
        PickNewDestination(panicWanderRadius);
    }

    private void TickPanic()
    {
        panicTimeLeft -= Time.deltaTime;
        stateTimer -= Time.deltaTime;

        bool reachedPoint = !agent.pathPending && agent.remainingDistance <= arrivalThreshold;

        // Redirige por tiempo (erratico) O al llegar a destino, lo que pase primero
        if (stateTimer <= 0f || reachedPoint)
        {
            stateTimer = Random.Range(panicRedirectMin, panicRedirectMax);
            PickNewDestination(panicWanderRadius);
        }

        if (panicTimeLeft <= 0f)
        {
            EnterWalking();
        }
    }

    // ---------------------------------------------------------
    // Utilidad compartida para buscar un punto valido en el NavMesh
    // ---------------------------------------------------------
    private void PickNewDestination(float radius)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPosition = transform.position + Random.insideUnitSphere * radius;

            if (NavMesh.SamplePosition(randomPosition, out NavMeshHit hit, radius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }

        Debug.LogWarning("NPCWalker: No se encontro un destino valido en el NavMesh.", this);
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetFloat(animatorSpeedParam, agent.velocity.magnitude);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, panicWanderRadius);
    }
}