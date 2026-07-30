using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RescateVR.Gameplay
{
    /// <summary>
    /// Gestiona el Menú Radial UI del Botiquín para seleccionar herramientas direccionalmente.
    /// Tecla de acceso: Q o TAB (Soporta Input System Nuevo y Viejo).
    /// </summary>
    public class RadialMenuUI : MonoBehaviour
    {
        [Header("Referencias Principales")]
        public PlayerMedicalKit medicalKit;
        public GameObject radialPanel;

        [Header("Tecla de Acceso (Input Viejo)")]
        public KeyCode toggleKey = KeyCode.Q;

        [Header("Textos de las 4 Secciones (Norte, Este, Sur, Oeste)")]
        public TextMeshProUGUI topText;     // Norte: Guantes
        public TextMeshProUGUI rightText;   // Este: Gasa
        public TextMeshProUGUI bottomText;  // Sur: Estetoscopio
        public TextMeshProUGUI leftText;    // Oeste: Guardar Herramienta

        [Header("Iconos/Imágenes SVG de las 4 Secciones (RawImage)")]
        public RawImage topRawImage;       // Norte: guantes.svg
        public RawImage rightRawImage;     // Este: gasa.svg
        public RawImage bottomRawImage;    // Sur: estetoscopio.svg
        public RawImage leftRawImage;      // Oeste: botiquin.svg

        [Header("Iconos/Imágenes de las 4 Secciones (Image Opcional)")]
        public Image topIconImage;     // Norte: Guantes
        public Image rightIconImage;   // Este: Gasa
        public Image bottomIconImage;  // Sur: Estetoscopio
        public Image leftIconImage;    // Oeste: Guardar

        [Header("Audio")]
        [Tooltip("AudioSource para los efectos del Menú Radial (asegúrate de que exista uno en este objeto)")]
        public AudioSource radialAudioSource;
        [Tooltip("Sonido al abrir el menú radial")]
        public AudioClip openMenuClip;

        private bool isOpen = false;
        private MedicalToolType currentHoverTool = MedicalToolType.None;
        private CanvasGroup canvasGroup;

        void Awake()
        {
            if (radialPanel == null)
            {
                radialPanel = this.gameObject;
            }

            // Asegurar componente CanvasGroup para controlar visibilidad sin apagar el script
            canvasGroup = radialPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = radialPanel.AddComponent<CanvasGroup>();
            }
        }

        void Start()
        {
            if (medicalKit == null)
            {
                medicalKit = Object.FindObjectOfType<PlayerMedicalKit>();
            }

            LoadResourceIconsIfAvailable();

            // Ocultar la UI al iniciar el juego
            SetPanelVisibility(false);
        }

        private void LoadResourceIconsIfAvailable()
        {
            // Cargar Texturas SVG para RawImage (Norte: Estetoscopio, Este: Guantes, Oeste: Gasa, Sur: Botiquín)
            if (topRawImage != null && topRawImage.texture == null)
                topRawImage.texture = Resources.Load<Texture>("tools/estetoscopio");

            if (rightRawImage != null && rightRawImage.texture == null)
                rightRawImage.texture = Resources.Load<Texture>("tools/guantes");

            if (leftRawImage != null && leftRawImage.texture == null)
                leftRawImage.texture = Resources.Load<Texture>("tools/gasa");

            if (bottomRawImage != null && bottomRawImage.texture == null)
                bottomRawImage.texture = Resources.Load<Texture>("tools/botiquin");

            // Cargar Sprites para Image (si aplica)
            if (topIconImage != null && topIconImage.sprite == null)
                topIconImage.sprite = Resources.Load<Sprite>("tools/estetoscopio");

            if (rightIconImage != null && rightIconImage.sprite == null)
                rightIconImage.sprite = Resources.Load<Sprite>("tools/guantes");

            if (leftIconImage != null && leftIconImage.sprite == null)
                leftIconImage.sprite = Resources.Load<Sprite>("tools/gasa");

            if (bottomIconImage != null && bottomIconImage.sprite == null)
                bottomIconImage.sprite = Resources.Load<Sprite>("tools/botiquin");
        }

        void Update()
        {
            // No procesar ni abrir Menú Radial si el Menú de Pausa u Opciones están activos en pantalla
            if (IsPauseOrOptionsMenuActive())
            {
                if (isOpen)
                {
                    CloseMenu();
                }
                return;
            }

            // 1. Detección de Apertura (Presionar Q o Tab)
            if (IsToggleKeyPressedThisFrame() && !isOpen)
            {
                OpenMenu();
            }

            // 2. Mientras el menú esté abierto, calcular dirección y equipar herramienta al SOLTAR la tecla o hacer clic
            if (isOpen)
            {
                CalculateDirection();

                // Al SOLTAR Q / Tab O hacer clic izquierdo: Equipar la herramienta resaltada y cerrar
                if (IsToggleKeyReleasedThisFrame() || Input.GetMouseButtonDown(0))
                {
                    SelectCurrentTool();
                    CloseMenu();
                }
            }
        }

        public void OpenMenu()
        {
            isOpen = true;
            Time.timeScale = 0f; // Pausar tiempo del juego al abrir el Menú Radial
            SetPanelVisibility(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Reproducir sonido de abrir (ignora la pausa del tiempo)
            if (radialAudioSource != null && openMenuClip != null)
            {
                radialAudioSource.ignoreListenerPause = true;
                radialAudioSource.PlayOneShot(openMenuClip);
            }
        }

        public void CloseMenu()
        {
            isOpen = false;
            SetPanelVisibility(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Restaurar tiempo de juego al cerrar (salvo si el Menú de Pausa u Opciones están activos)
            if (!IsPauseOrOptionsMenuActive())
            {
                Time.timeScale = 1f;
            }
        }

        private bool IsPauseOrOptionsMenuActive()
        {
            PauseManager pm = Object.FindFirstObjectByType<PauseManager>(FindObjectsInactive.Include);
            if (pm != null)
            {
                if ((pm.pauseCanvas != null && pm.pauseCanvas.activeInHierarchy) ||
                    (pm.optionsMenuPanel != null && pm.optionsMenuPanel.activeInHierarchy))
                {
                    return true;
                }
            }
            return false;
        }

        private void SetPanelVisibility(bool visible)
        {
            if (canvasGroup == null && radialPanel != null)
            {
                canvasGroup = radialPanel.GetComponent<CanvasGroup>();
                if (canvasGroup == null) canvasGroup = radialPanel.AddComponent<CanvasGroup>();
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.blocksRaycasts = visible;
                canvasGroup.interactable = visible;
            }
        }

        private bool IsToggleKeyPressedThisFrame()
        {
            // Detección en Input Viejo
            bool oldInput = Input.GetKeyDown(toggleKey) || Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Tab);
            if (oldInput) return true;

            // Detección en Input System Nuevo
            if (Keyboard.current != null)
            {
                if (Keyboard.current[Key.Q].wasPressedThisFrame || Keyboard.current[Key.Tab].wasPressedThisFrame)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsToggleKeyReleasedThisFrame()
        {
            // Detección en Input Viejo
            bool oldInput = Input.GetKeyUp(toggleKey) || Input.GetKeyUp(KeyCode.Q) || Input.GetKeyUp(KeyCode.Tab);
            if (oldInput) return true;

            // Detección en Input System Nuevo
            if (Keyboard.current != null)
            {
                if (Keyboard.current[Key.Q].wasReleasedThisFrame || Keyboard.current[Key.Tab].wasReleasedThisFrame)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsToggleKeyHeld()
        {
            // Detección en Input Viejo
            bool oldInput = Input.GetKey(toggleKey) || Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.Tab);
            if (oldInput) return true;

            // Detección en Input System Nuevo
            if (Keyboard.current != null)
            {
                if (Keyboard.current[Key.Q].isPressed || Keyboard.current[Key.Tab].isPressed)
                {
                    return true;
                }
            }

            return false;
        }

        private void CalculateDirection()
        {
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Vector2 mousePos = Input.mousePosition;
            Vector2 dir = mousePos - screenCenter;

            // Zona muerta central (30 píxeles de radio)
            if (dir.magnitude < 30f)
            {
                currentHoverTool = MedicalToolType.None;
                HighlightSection(currentHoverTool);
                return;
            }

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            // Selección direccional por cuadrantes (Norte: Estetoscopio, Este: Guantes, Oeste: Gasa, Sur: Botiquín)
            if (angle >= 45f && angle < 135f)
            {
                currentHoverTool = MedicalToolType.Stethoscope; // Norte: Estetoscopio
            }
            else if (angle >= 315f || angle < 45f)
            {
                currentHoverTool = MedicalToolType.Gloves;      // Este: Guantes
            }
            else if (angle >= 225f && angle < 315f)
            {
                currentHoverTool = MedicalToolType.None;        // Sur: Botiquín / Guardar
            }
            else
            {
                currentHoverTool = MedicalToolType.Gauze;       // Oeste: Gasa
            }

            HighlightSection(currentHoverTool);
        }

        private void HighlightSection(MedicalToolType tool)
        {
            SetTextHighlight(topText, tool == MedicalToolType.Stethoscope);
            SetTextHighlight(rightText, tool == MedicalToolType.Gloves);
            SetTextHighlight(leftText, tool == MedicalToolType.Gauze);
            SetTextHighlight(bottomText, tool == MedicalToolType.None);

            SetRawIconHighlight(topRawImage, tool == MedicalToolType.Stethoscope);
            SetRawIconHighlight(rightRawImage, tool == MedicalToolType.Gloves);
            SetRawIconHighlight(leftRawImage, tool == MedicalToolType.Gauze);
            SetRawIconHighlight(bottomRawImage, tool == MedicalToolType.None);

            SetIconHighlight(topIconImage, tool == MedicalToolType.Stethoscope);
            SetIconHighlight(rightIconImage, tool == MedicalToolType.Gloves);
            SetIconHighlight(leftIconImage, tool == MedicalToolType.Gauze);
            SetIconHighlight(bottomIconImage, tool == MedicalToolType.None);
        }

        private void SetRawIconHighlight(RawImage rawImg, bool isHighlighted)
        {
            if (rawImg == null) return;
            rawImg.color = isHighlighted ? Color.yellow : Color.white;
            rawImg.transform.localScale = isHighlighted ? new Vector3(1.2f, 1.2f, 1f) : Vector3.one;
        }

        private void SetTextHighlight(TextMeshProUGUI text, bool isHighlighted)
        {
            if (text == null) return;
            text.color = isHighlighted ? Color.yellow : Color.white;
            text.fontStyle = isHighlighted ? FontStyles.Bold : FontStyles.Normal;
        }

        private void SetIconHighlight(Image icon, bool isHighlighted)
        {
            if (icon == null) return;
            icon.color = isHighlighted ? Color.yellow : Color.white;
            icon.transform.localScale = isHighlighted ? new Vector3(1.2f, 1.2f, 1f) : Vector3.one;
        }

        private void SelectCurrentTool()
        {
            if (medicalKit == null)
            {
                medicalKit = Object.FindObjectOfType<PlayerMedicalKit>();
            }

            if (medicalKit != null)
            {
                medicalKit.EquipTool(currentHoverTool);
            }
            else
            {
                Debug.LogWarning("[RadialMenuUI] No se encontró el componente PlayerMedicalKit en la escena.");
            }
        }
    }
}
