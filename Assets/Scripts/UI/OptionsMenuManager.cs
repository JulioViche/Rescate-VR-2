using UnityEngine;
using UnityEngine.UI;

namespace RescateVR.UI
{
    /// <summary>
    /// Gestiona el Menú de Opciones (Volumen, Sensibilidad de Cámara, Brillo y Créditos).
    /// </summary>
    public class OptionsMenuManager : MonoBehaviour
    {
        [Header("Referencias de UI (Sliders)")]
        [Tooltip("Slider UI para controlar el volumen general del audio (0 a 1)")]
        public Slider volumeSlider;

        [Tooltip("Slider UI para controlar la sensibilidad de la cámara del jugador (0.5 a 10)")]
        public Slider sensitivitySlider;

        [Tooltip("Slider UI para ajustar el brillo de la pantalla (0.2 a 1)")]
        public Slider brightnessSlider;

        [Header("Referencias de Cámara y Escena")]
        [Tooltip("Script FirstPersonLook de la cámara del jugador para modificar su sensibilidad")]
        public FirstPersonLook cameraLook;

        [Tooltip("Imagen UI transparente de color negro que cubre la pantalla para simular el brillo (opcional)")]
        public Image brightnessOverlay;

        [Header("Panel de Créditos")]
        [Tooltip("GameObject del Panel o Imagen de Créditos")]
        public GameObject creditsPanel;

        void OnEnable()
        {
            // Liberar ratón para interactuar con los Sliders y Botones
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Asegurar que el propio CanvasGroup del menú de opciones permita interacción
            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.blocksRaycasts = true;
                cg.interactable = true;
            }
        }

        void Update()
        {
            // Si el jugador presiona Escape estando en Opciones, cerrar Opciones o Créditos
            bool escapePressed = Input.GetKeyDown(KeyCode.Escape);
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                escapePressed |= UnityEngine.InputSystem.Keyboard.current[UnityEngine.InputSystem.Key.Escape].wasPressedThisFrame;
            }

            if (escapePressed)
            {
                if (creditsPanel != null && creditsPanel.activeInHierarchy)
                {
                    CloseCredits();
                }
                else
                {
                    CloseOptionsMenu();
                }
            }
        }

        void Awake()
        {
            if (cameraLook == null)
            {
                cameraLook = Object.FindFirstObjectByType<FirstPersonLook>(FindObjectsInactive.Include);
            }

            // Inicializar sliders en el 50% de su rango antes de que se muestre la UI en pantalla
            InitializeSliderValues();
        }

        void Start()
        {
            // Ocultar créditos al iniciar
            if (creditsPanel != null)
            {
                creditsPanel.SetActive(false);
            }

            // Suscribir eventos OnValueChanged de los Sliders
            if (volumeSlider != null) volumeSlider.onValueChanged.AddListener(SetVolume);
            if (sensitivitySlider != null) sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
            if (brightnessSlider != null) brightnessSlider.onValueChanged.AddListener(SetBrightness);
        }

        private void InitializeSliderValues()
        {
            // 1. Sensibilidad de Cámara: El valor actual (2.0) queda exactamente en el 50% (mitad del rango 0.1 a 4.0)
            float currentCamSens = (cameraLook != null) ? cameraLook.sensitivity : 2.0f;
            float savedSensitivity = PlayerPrefs.GetFloat("CameraSensitivity", currentCamSens);

            if (sensitivitySlider != null)
            {
                sensitivitySlider.minValue = 0.1f;
                sensitivitySlider.maxValue = currentCamSens * 2.0f; // 50% exacto para 2.0
                sensitivitySlider.value = savedSensitivity;
            }
            SetSensitivity(savedSensitivity);

            // 2. Volumen General del Juego: Predeterminado al 100% (1.0) para coincidir con el audio nativo al iniciar
            float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
            if (volumeSlider != null)
            {
                volumeSlider.minValue = 0f;
                volumeSlider.maxValue = 1f;
                volumeSlider.value = savedVolume;
            }
            SetVolume(savedVolume);

            // 3. Brillo de Pantalla: Predeterminado a 1.0 (Brillo Normal / Alfa = 0) para coincidir 100% con la pantalla inicial sin saltos de luz
            float savedBrightness = PlayerPrefs.GetFloat("ScreenBrightness", 1.0f);
            if (brightnessSlider != null)
            {
                brightnessSlider.minValue = 0f;
                brightnessSlider.maxValue = 1f;
                brightnessSlider.value = savedBrightness;
            }
            SetBrightness(savedBrightness);
        }

        /// <summary>
        /// Modifica el volumen general del juego (0.0 a 1.0).
        /// </summary>
        public void SetVolume(float value)
        {
            AudioListener.volume = value;
            PlayerPrefs.SetFloat("MasterVolume", value);
        }

        /// <summary>
        /// Modifica la sensibilidad de rotación de la cámara del jugador.
        /// </summary>
        public void SetSensitivity(float value)
        {
            if (cameraLook != null)
            {
                cameraLook.sensitivity = value;
            }
            PlayerPrefs.SetFloat("CameraSensitivity", value);
        }

        /// <summary>
        /// Modifica la opacidad de la imagen de brillo (solo si está asignada en el Inspector).
        /// </summary>
        public void SetBrightness(float value)
        {
            if (brightnessOverlay == null)
            {
                GameObject overlayGO = GameObject.Find("BrightnessOverlay");
                if (overlayGO != null)
                {
                    brightnessOverlay = overlayGO.GetComponent<Image>();
                }
            }

            if (brightnessOverlay != null)
            {
                float alpha = Mathf.Clamp01(1f - value) * 0.7f;
                Color c = brightnessOverlay.color;
                brightnessOverlay.color = new Color(c.r, c.g, c.b, alpha);
            }
            PlayerPrefs.SetFloat("ScreenBrightness", value);
        }

        /// <summary>
        /// Muestra el panel de Créditos.
        /// </summary>
        public void OpenCredits()
        {
            if (creditsPanel != null)
            {
                creditsPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Oculta el panel de Créditos.
        /// </summary>
        public void CloseCredits()
        {
            if (creditsPanel != null)
            {
                creditsPanel.SetActive(false);
            }
        }

        [Header("Navegación de UI (Botón Volver)")]
        [Tooltip("El GameObject del propio panel de Opciones")]
        public GameObject optionsMenuPanel;

        [Tooltip("El Panel anterior al que se volverá (ej: PauseMenu o MainMenuPanel)")]
        public GameObject parentPanelToReturn;

        /// <summary>
        /// Cierra el menú de opciones y regresa al panel anterior (PauseMenu o MainMenu).
        /// </summary>
        public void CloseOptionsMenu()
        {
            if (optionsMenuPanel != null)
            {
                optionsMenuPanel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }

            // Si existe PauseManager en la escena, restaurar el menú de pausa correctamente
            PauseManager pm = Object.FindFirstObjectByType<PauseManager>(FindObjectsInactive.Include);
            if (pm != null)
            {
                pm.ShowPauseMenu();
            }
            else if (parentPanelToReturn != null)
            {
                parentPanelToReturn.SetActive(true);
            }
        }
    }
}
