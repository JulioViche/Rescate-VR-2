using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(BoxCollider))]
public class NPCSpawner : MonoBehaviour
{
    [Header("Prefab (auto-load)")]
    [Tooltip("Si esta vacio, el spawner carga el prefab desde Resources/npc placeholder.prefab " +
             "con Resources.Load. Si lo asignas a mano, usa esa referencia en su lugar.")]
    [SerializeField] private GameObject npcPrefab;

    [Header("Spawn")]
    [SerializeField] private int spawnCount = 30;
    [SerializeField] private float minDistance = 2f;

    [Tooltip("Radio de la esfera alrededor del spawner donde se buscan candidatos " +
             "antes de hacer snap a NavMesh. Grande = mas chances de encontrar NavMesh valido.")]
    [SerializeField] private float spawnSearchRadius = 50f;

    [SerializeField] private int maxAttemptsPerNpc = 40;

    [Header("Contención")]
    [Tooltip("Nombre del GameObject padre donde se organizan los NPCs. " +
             "Si no existe en la escena, se crea automaticamente en la raiz.")]
    [SerializeField] private string npcParentName = "NPCs";

    [Header("Cuándo")]
    [SerializeField] private bool spawnOnStart = true;

    private BoxCollider box;
    private Transform npcParent;
    private readonly List<GameObject> spawned = new List<GameObject>();

    private void Awake()
    {
        box = GetComponent<BoxCollider>();
    }

    private void Start()
    {
        npcParent = ResolveOrCreateParent();
        if (spawnOnStart) SpawnAll();
    }

    public void SpawnAll()
    {
        GameObject prefab = ResolvePrefab();
        if (prefab == null)
        {
            Debug.LogError("NPCSpawner: no se encontro el prefab. Asignalo a mano o pon uno en Assets/Resources/ llamado 'npc placeholder'.", this);
            return;
        }

        if (npcParent == null)
        {
            Debug.LogError("NPCSpawner: no se pudo crear ni encontrar el GameObject padre de NPCs.", this);
            return;
        }

        var usedPositions = new List<Vector3>(spawnCount);

        for (int i = 0; i < spawnCount; i++)
        {
            if (!TryFindValidPoint(usedPositions, out Vector3 point))
            {
                Debug.LogWarning($"NPCSpawner: no se encontro punto valido dentro de la caja para NPC {i}.", this);
                continue;
            }

            Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            GameObject npc = Instantiate(prefab, point, rot, npcParent);
            npc.name = $"{npcParentName}_{i:D2}";

            NPCWalker walker = npc.GetComponent<NPCWalker>();
            if (walker != null) walker.SetWanderBounds(box);

            usedPositions.Add(point);
            spawned.Add(npc);
        }

        Debug.Log($"NPCSpawner: spawneados {spawned.Count} de {spawnCount} intentos dentro de '{box.name}'.", this);
    }

    private Transform ResolveOrCreateParent()
    {
        // Buscar uno existente en la escena por nombre
        GameObject existing = GameObject.Find(npcParentName);
        if (existing != null) return existing.transform;

        // Si no existe, crearlo en la raiz
        GameObject go = new GameObject(npcParentName);
        Debug.Log($"NPCSpawner: creado GameObject padre '{npcParentName}' en la raiz.", this);
        return go.transform;
    }

    private GameObject ResolvePrefab()
    {
        if (npcPrefab != null) return npcPrefab;
        return Resources.Load<GameObject>("npc placeholder");
    }

    private bool TryFindValidPoint(List<Vector3> alreadySpawned, out Vector3 result)
    {
        Bounds b = box.bounds;
        for (int attempt = 0; attempt < maxAttemptsPerNpc; attempt++)
        {
            // 1) Random en una esfera alrededor del spawner (no de la caja)
            //    Esto garantiza que estamos cerca del NavMesh
            Vector3 randomAroundSpawner = transform.position + Random.insideUnitSphere * spawnSearchRadius;

            // 2) Snap al NavMesh (casi siempre funciona, está cerca del suelo)
            if (!NavMesh.SamplePosition(randomAroundSpawner, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                continue;

            // 3) Validar que el punto del NavMesh este dentro de la caja de contencion
            if (!b.Contains(hit.position))
                continue;

            // 4) Verificar separacion minima con NPCs ya spawneados
            bool tooClose = false;
            for (int i = 0; i < alreadySpawned.Count; i++)
            {
                if (Vector3.Distance(hit.position, alreadySpawned[i]) < minDistance)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            result = hit.position;
            return true;
        }

        result = Vector3.zero;
        return false;
    }
}
