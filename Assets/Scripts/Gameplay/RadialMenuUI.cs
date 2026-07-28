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

        [Header("Iconos/Imágenes de las 4 Secciones (Opcionales)")]
        public Image topIconImage;     // Norte: Guantes
        public Image rightIconImage;   // Este: Gasa
        public Image bottomIconImage;  // Sur: Estetoscopio
        public Image leftIconImage;    // Oeste: Guardar

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

            // Ocultar la UI al iniciar el juego
            SetPanelVisibility(false);
        }

        void Update()
        {
            // 1. Detección de Apertura (Presionar Q o Tab)
            if (IsToggleKeyPressedThisFrame())
            {
                OpenMenu();
            }

            // 2. Detección de Cierre (Soltar Q o Tab)
            if (IsToggleKeyReleasedThisFrame() && isOpen)
            {
                SelectCurrentTool();
                CloseMenu();
            }

            // 3. Mientras el menú esté abierto, calcular dirección del ratón
            if (isOpen)
            {
                CalculateDirection();

                // Si por alguna razón la tecla ya no está presionada, cerrar
                if (!IsToggleKeyHeld() && !Input.GetMouseButton(0))
                {
                    SelectCurrentTool();
                    CloseMenu();
                }
            }
        }

        public void OpenMenu()
        {
            isOpen = true;
            SetPanelVisibility(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void CloseMenu()
        {
            isOpen = false;
            SetPanelVisibility(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void SetPanelVisibility(bool visible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.blocksRaycasts = visible;
                canvasGroup.interactable = visible;
            }

            // Si el script no está en el mismo GameObject del panel, podemos alternar SetActive
            if (radialPanel != null && radialPanel != this.gameObject)
            {
                radialPanel.SetActive(visible);
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

            // Selección direccional por cuadrantes (Norte, Este, Sur, Oeste)
            if (angle >= 45f && angle < 135f)
            {
                currentHoverTool = MedicalToolType.Gloves;      // Norte: Guantes
            }
            else if (angle >= 315f || angle < 45f)
            {
                currentHoverTool = MedicalToolType.Gauze;       // Este: Gasa
            }
            else if (angle >= 225f && angle < 315f)
            {
                currentHoverTool = MedicalToolType.Stethoscope; // Sur: Estetoscopio
            }
            else
            {
                currentHoverTool = MedicalToolType.None;        // Oeste: Guardar
            }

            HighlightSection(currentHoverTool);
        }

        private void HighlightSection(MedicalToolType tool)
        {
            SetTextHighlight(topText, tool == MedicalToolType.Gloves);
            SetTextHighlight(rightText, tool == MedicalToolType.Gauze);
            SetTextHighlight(bottomText, tool == MedicalToolType.Stethoscope);
            SetTextHighlight(leftText, tool == MedicalToolType.None);

            SetIconHighlight(topIconImage, tool == MedicalToolType.Gloves);
            SetIconHighlight(rightIconImage, tool == MedicalToolType.Gauze);
            SetIconHighlight(bottomIconImage, tool == MedicalToolType.Stethoscope);
            SetIconHighlight(leftIconImage, tool == MedicalToolType.None);
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
            if (medicalKit != null)
            {
                medicalKit.EquipTool(currentHoverTool);
            }
        }
    }
}
