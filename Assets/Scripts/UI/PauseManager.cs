using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("Asigna aquí el GameObject del 'PauseMenu' (el panel del menú) o el 'Canvas'")]
    public GameObject pauseCanvas;

    [Tooltip("Asigna aquí el objeto 'Filter' (overlay de fondo oscuro, opcional)")]
    public GameObject filterOverlay;

    [Header("Settings")]
    [Tooltip("Tecla para pausar/reanudar (Old Input System). Default: P")]
    public KeyCode pauseKey = KeyCode.P;

    private bool isPaused = false;

    void Awake()
    {
        FindPauseCanvasIfNeeded();

        // CRÍTICO: Si PauseManagerGO está dentro de pauseCanvas (PauseMenu), se desvincularía al ocultar el menú.
        // Nos movemos al padre o a la raíz para permanecer activos durante el juego.
        if (pauseCanvas != null && transform.IsChildOf(pauseCanvas.transform))
        {
            transform.SetParent(pauseCanvas.transform.parent, true);
        }
    }

    void Start()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pauseCanvas == null || filterOverlay == null)
        {
            FindPauseCanvasIfNeeded();
        }

        // Ocultamos la UI y el filtro al iniciar el juego
        SetMenuVisible(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Detección de tecla P o Escape (Input Viejo)
        bool pOldInput = Input.GetKeyDown(pauseKey) || Input.GetKeyDown(KeyCode.Escape);

        // Detección de tecla P o Escape (Input System Nuevo)
        bool pNewInput = false;
        if (Keyboard.current != null)
        {
            pNewInput = Keyboard.current[Key.P].wasPressedThisFrame || Keyboard.current[Key.Escape].wasPressedThisFrame;
        }

        if (pOldInput || pNewInput)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (pauseCanvas == null || filterOverlay == null)
        {
            FindPauseCanvasIfNeeded();
        }

        SetMenuVisible(isPaused);

        Time.timeScale = isPaused ? 0f : 1f;

        if (isPaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void SetMenuVisible(bool visible)
    {
        if (pauseCanvas != null)
        {
            pauseCanvas.SetActive(visible);

            // Si pauseCanvas es el Canvas raíz y contiene un hijo "PauseMenu", también lo aseguramos
            Transform pmChild = pauseCanvas.transform.Find("PauseMenu");
            if (pmChild != null)
            {
                pmChild.gameObject.SetActive(visible);
            }

            // Si el menú usa CanvasGroup, ajustamos el alpha e interactividad
            CanvasGroup cg = pauseCanvas.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = visible ? 1f : 0f;
                cg.blocksRaycasts = visible;
                cg.interactable = visible;
            }
        }
        else
        {
            Debug.LogError("[PM] Error: pauseCanvas no asignado o no encontrado en la escena.");
        }

        // Activar / Desactivar el filtro de fondo (Filter)
        if (filterOverlay != null)
        {
            filterOverlay.SetActive(visible);

            CanvasGroup filterCg = filterOverlay.GetComponent<CanvasGroup>();
            if (filterCg != null)
            {
                filterCg.alpha = visible ? 1f : 0f;
                filterCg.blocksRaycasts = visible;
                filterCg.interactable = visible;
            }
        }
    }

    private void FindPauseCanvasIfNeeded()
    {
        // 1. Buscar "PauseMenu" y "Filter" incluso si están inactivos
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject go in allObjects)
        {
            if (go.hideFlags == HideFlags.None && go.scene.isLoaded)
            {
                if (pauseCanvas == null && go.name == "PauseMenu")
                {
                    pauseCanvas = go;
                }
                if (filterOverlay == null && go.name == "Filter")
                {
                    filterOverlay = go;
                }
            }
        }

        // 2. Si no se encontró "PauseMenu", buscar cualquier Canvas en la escena
        if (pauseCanvas == null)
        {
            Canvas mainCanvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (mainCanvas != null)
            {
                Transform pmChild = mainCanvas.transform.Find("PauseMenu");
                pauseCanvas = pmChild != null ? pmChild.gameObject : mainCanvas.gameObject;

                if (filterOverlay == null)
                {
                    Transform filterChild = mainCanvas.transform.Find("Filter");
                    if (filterChild != null)
                    {
                        filterOverlay = filterChild.gameObject;
                    }
                }
            }
        }
    }

    public void OnResume() => TogglePause();

    public void OnRestart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnOptions()
    {
        Debug.Log("Opciones - próximamente");
    }

    public void OnMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
