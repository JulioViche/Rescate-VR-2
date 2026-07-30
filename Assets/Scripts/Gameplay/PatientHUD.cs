using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RescateVR.Gameplay
{
    public enum NotificationType
    {
        Info,     // Bueno / Éxito (Verde)
        Warning,  // Advertencia / Información (Amarillo)
        Danger    // Malo / Error de Bioseguridad (Rojo)
    }

    /// <summary>
    /// Gestiona la Interfaz de Usuario (HUD) flotante durante el rescate:
    /// Nivel de sangre, Temporizador de 5 minutos, Signos Vitales y Paneles de Victoria/Derrota.
    /// </summary>
    public class PatientHUD : MonoBehaviour
    {
        [Header("Referencia al Paciente")]
        public PatientState patientState;

        [Header("UI Nivel de Sangre y Temporizador")]
        public TextMeshProUGUI timerText;
        public Slider bloodSlider;
        public TextMeshProUGUI bloodText;
        public TextMeshProUGUI bloodPercentageText;

        [Header("UI Signos Vitales (Estetoscopio)")]
        public TextMeshProUGUI heartRateText;
        public TextMeshProUGUI respirationText;
        public TextMeshProUGUI bloodPressureText;

        [Header("UI Sliders de Signos Vitales (Opcionales)")]
        [Tooltip("Slider UI para el pulso (BPM)")]
        public Slider heartRateSlider;
        public Slider respirationSlider;
        public Slider bloodPressureSlider;

        [Header("Límites de Signos Vitales")]
        public float maxHeartRateBPM = 180f;
        public float maxRespirationRPM = 40f;
        public float maxBloodPressuremmHg = 180f;

        [Header("UI Bioseguridad / Herramienta Equipada")]
        [Tooltip("Texto UI que muestra únicamente el nombre de la herramienta equipada")]
        public TextMeshProUGUI equippedToolText;

        [Tooltip("Texto UI que muestra únicamente el estado independiente de los guantes ([Guantes Puestos] / [Sin Guantes])")]
        public TextMeshProUGUI glovesStatusText;

        [Tooltip("RawImage UI para mostrar el icono SVG/Textura de la herramienta equipada en el HUD")]
        public RawImage equippedToolImage;

        [Tooltip("Textura / SVG personalizada para el estado 'Sin Herramienta' (Botiquín)")]
        public Texture noToolTexture;

        [Header("UI Feedback de Inspección en Tiempo Real")]
        [Tooltip("Texto UI TMP en pantalla que muestra qué herida o parte del cuerpo estás mirando")]
        public TextMeshProUGUI interactionPromptText;

        [Header("UI Advertencias / Warnings")]
        [Tooltip("GameObject contenedor del panel/caja de Advertencias (Warnings)")]
        public GameObject warningPanel;

        [Tooltip("Texto UI para el Título de la Advertencia (Warning Title)")]
        public TextMeshProUGUI warningTitleText;

        [Tooltip("Texto UI para el Mensaje detallado de la Advertencia (Warning Message)")]
        public TextMeshProUGUI warningMessageText;

        [Header("Colores de Alertas (Bueno, Advertencia, Malo)")]
        public Color infoColor = new Color(0f, 0.95f, 0.4f, 1f);      // Verde (Bueno / Éxito)
        public Color warningColor = new Color(1f, 0.85f, 0.1f, 1f);   // Amarillo (Advertencia)
        public Color dangerColor = new Color(1f, 0.25f, 0.25f, 1f);    // Rojo (Malo / Bioseguridad)

        [Header("Paneles de Resultado final")]
        public GameObject victoryPanel;
        public GameObject defeatPanel;

        void Awake()
        {
            if (patientState == null)
            {
                patientState = Object.FindFirstObjectByType<PatientState>();
            }
        }

        void Start()
        {
            // Ocultar panel de advertencias al iniciar
            if (warningPanel != null)
            {
                warningPanel.SetActive(false);
            }

            // Configurar límites mínimos y máximos de los sliders automáticamente
            if (bloodSlider != null) { bloodSlider.minValue = 0f; bloodSlider.maxValue = 1f; }
            if (heartRateSlider != null) { heartRateSlider.minValue = 0f; heartRateSlider.maxValue = maxHeartRateBPM; }
            if (respirationSlider != null) { respirationSlider.minValue = 0f; respirationSlider.maxValue = maxRespirationRPM; }
            if (bloodPressureSlider != null) { bloodPressureSlider.minValue = 0f; bloodPressureSlider.maxValue = maxBloodPressuremmHg; }

            // Forzar actualización inicial de la interfaz
            if (patientState != null)
            {
                UpdateBloodUI(patientState.currentBlood, patientState.maxBlood);
                UpdateTimerUI(patientState.timeRemaining);
                UpdateVitalsUI(patientState.heartRateBPM, patientState.respirationRPM, patientState.systolicBP, patientState.diastolicBP);
            }

            // Forzar actualización inicial de la herramienta equipada al iniciar
            PlayerMedicalKit kit = Object.FindObjectOfType<PlayerMedicalKit>();
            if (kit != null)
            {
                UpdateEquippedToolUI(kit.currentlyEquippedTool, kit.hasGlovesEquipped);
            }
        }

        void Update()
        {
            if (patientState != null && !patientState.isDead && !patientState.isVictory && Time.timeScale > 0f)
            {
                UpdateTimerUI(patientState.timeRemaining);
            }
        }

        /// <summary>
        /// Muestra u oculta el HUD (usado al pausar/reanudar el juego).
        /// </summary>
        public void SetHUDVisible(bool visible)
        {
            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = visible ? 1f : 0f;
                cg.blocksRaycasts = visible;
            }
            else
            {
                gameObject.SetActive(visible);
            }
        }

        void OnEnable()
        {
            if (patientState == null)
            {
                patientState = Object.FindFirstObjectByType<PatientState>();
            }

            if (patientState != null)
            {
                patientState.OnBloodChanged.AddListener(UpdateBloodUI);
                patientState.OnTimeUpdated.AddListener(UpdateTimerUI);
                patientState.OnVitalsUpdated.AddListener(UpdateVitalsUI);
                patientState.OnPatientDied.AddListener(ShowDefeatScreen);
                patientState.OnMissionVictory.AddListener(ShowVictoryScreen);
            }

            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (defeatPanel != null) defeatPanel.SetActive(false);
            if (warningPanel != null) warningPanel.SetActive(false);
            if (warningTitleText != null) warningTitleText.text = "";
            if (warningMessageText != null) warningMessageText.text = "";
        }

        void OnDisable()
        {
            if (patientState != null)
            {
                patientState.OnBloodChanged.RemoveListener(UpdateBloodUI);
                patientState.OnTimeUpdated.RemoveListener(UpdateTimerUI);
                patientState.OnVitalsUpdated.RemoveListener(UpdateVitalsUI);
                patientState.OnPatientDied.RemoveListener(ShowDefeatScreen);
                patientState.OnMissionVictory.RemoveListener(ShowVictoryScreen);
            }
        }

        [ContextMenu("Probar HUD (Actualizar Signos Vitales)")]
        public void TestHUDVitals()
        {
            if (patientState != null)
            {
                UpdateVitalsUI(patientState.heartRateBPM, patientState.respirationRPM, patientState.systolicBP, patientState.diastolicBP);
                UpdateBloodUI(patientState.currentBlood, patientState.maxBlood);
            }
        }

        private bool hasWarnedCriticalBlood = false;

        public void UpdateBloodUI(float current, float max)
        {
            float pct = Mathf.Clamp01(current / max);
            if (bloodSlider != null) bloodSlider.value = pct;
            if (bloodPercentageText != null) bloodPercentageText.text = $"{current:F0}%";
            if (bloodText != null) bloodText.text = $"{current:F0}%";

            // Alerta roja crítica cuando el nivel de sangre cae por debajo del 10%
            float percentage = (current / max) * 100f;
            if (percentage <= 10f && percentage > 0f)
            {
                if (!hasWarnedCriticalBlood)
                {
                    hasWarnedCriticalBlood = true;
                    ShowWarning("¡PACIENTE EN ESTADO CRÍTICO!", "¡Nivel de sangre menor al 10%! Aplica gasas inmediatamente para detener la hemorragia.", NotificationType.Danger, 4.5f);
                }
            }
            else if (percentage > 15f)
            {
                hasWarnedCriticalBlood = false;
            }
        }

        public void UpdateTimerUI(float secondsRemaining)
        {
            if (timerText == null) return;
            
            // Multiplicar el tiempo visualmente por 5 según lo requerido
            float visualSeconds = secondsRemaining * 5f;
            
            int minutes = Mathf.FloorToInt(visualSeconds / 60f);
            int seconds = Mathf.FloorToInt(visualSeconds % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }

        public void UpdateVitalsUI(int bpm, int rpm, int sys, int dia)
        {
            // 1. Actualizar Textos TMP
            if (heartRateText != null) heartRateText.text = $"{bpm} BPM";
            if (respirationText != null) respirationText.text = $"{rpm} RPM";
            if (bloodPressureText != null) bloodPressureText.text = $"{sys}/{dia} mmHg";

            // 2. Actualizar Sliders UI
            if (heartRateSlider != null) heartRateSlider.value = Mathf.Clamp(bpm, 0f, maxHeartRateBPM);
            if (respirationSlider != null) respirationSlider.value = Mathf.Clamp(rpm, 0f, maxRespirationRPM);
            if (bloodPressureSlider != null) bloodPressureSlider.value = Mathf.Clamp(sys, 0f, maxBloodPressuremmHg);
        }

        public void UpdateEquippedToolUI(MedicalToolType tool, bool hasGloves)
        {
            // 1. Actualizar Texto de la Herramienta Equipada (Solo el nombre)
            if (equippedToolText != null)
            {
                equippedToolText.text = GetToolNameString(tool);
            }

            // 2. Actualizar Texto de Estado de Guantes ([Guantes Puestos] / [Sin Guantes])
            if (glovesStatusText != null)
            {
                glovesStatusText.text = hasGloves ? "<color=green>[Guantes Puestos]</color>" : "<color=red>[Sin Guantes]</color>";
            }

            // 3. Actualizar RawImage (SVG / Textura) en el HUD
            if (equippedToolImage != null)
            {
                Texture iconTexture = GetToolTexture(tool);
                if (iconTexture != null)
                {
                    equippedToolImage.texture = iconTexture;
                    equippedToolImage.enabled = true;
                }
                else
                {
                    equippedToolImage.enabled = false;
                }
            }
        }

        private string GetToolNameString(MedicalToolType tool)
        {
            switch (tool)
            {
                case MedicalToolType.Gloves: return "Guantes Médicos";
                case MedicalToolType.Gauze: return "Gasa / Vendaje";
                case MedicalToolType.Stethoscope: return "Estetoscopio";
                case MedicalToolType.Flashlight: return "Linterna";
                default: return "Seleccionar Herramienta";
            }
        }

        private Texture GetToolTexture(MedicalToolType tool)
        {
            switch (tool)
            {
                case MedicalToolType.Gloves: return Resources.Load<Texture>("tools/guantes");
                case MedicalToolType.Gauze: return Resources.Load<Texture>("tools/gasa");
                case MedicalToolType.Stethoscope: return Resources.Load<Texture>("tools/estetoscopio");
                case MedicalToolType.None: return noToolTexture != null ? noToolTexture : Resources.Load<Texture>("tools/botiquin");
                default: return noToolTexture != null ? noToolTexture : Resources.Load<Texture>("tools/botiquin");
            }
        }

        private Sprite GetToolSprite(MedicalToolType tool)
        {
            switch (tool)
            {
                case MedicalToolType.Gloves: return Resources.Load<Sprite>("tools/guantes");
                case MedicalToolType.Gauze: return Resources.Load<Sprite>("tools/gasa");
                case MedicalToolType.Stethoscope: return Resources.Load<Sprite>("tools/estetoscopio");
                case MedicalToolType.Flashlight: return Resources.Load<Sprite>("tools/botiquin");
                case MedicalToolType.None: return Resources.Load<Sprite>("tools/botiquin");
                default: return null;
            }
        }

        public void ShowWarning(string title, string message, NotificationType type = NotificationType.Warning, float duration = 3.0f)
        {
            Color targetColor = warningColor;
            switch (type)
            {
                case NotificationType.Info:
                    targetColor = infoColor;
                    break;
                case NotificationType.Warning:
                    targetColor = warningColor;
                    break;
                case NotificationType.Danger:
                    targetColor = dangerColor;
                    break;
            }

            if (warningTitleText != null)
            {
                warningTitleText.text = title;
                warningTitleText.color = targetColor;
            }

            if (warningMessageText != null)
            {
                warningMessageText.text = message;
                warningMessageText.color = targetColor;
            }

            if (warningPanel != null)
            {
                Image bgImage = warningPanel.GetComponent<Image>();
                if (bgImage != null)
                {
                    Color bgCol = targetColor;
                    bgCol.a = 0.2f;
                    bgImage.color = bgCol;
                }
                warningPanel.SetActive(true);
            }

            CancelInvoke(nameof(ClearWarning));
            Invoke(nameof(ClearWarning), duration);
        }

        public void ShowWarning(string title, string message, float duration)
        {
            ShowWarning(title, message, NotificationType.Warning, duration);
        }

        public void ShowWarning(string message, float duration = 3.0f)
        {
            ShowWarning("¡ADVERTENCIA!", message, NotificationType.Warning, duration);
        }

        /// <summary>
        /// Actualiza el texto de inspección/hover en tiempo real al mirar una herida o al paciente.
        /// </summary>
        public void UpdateInteractionPrompt(string text)
        {
            if (interactionPromptText != null)
            {
                interactionPromptText.text = text;
                interactionPromptText.gameObject.SetActive(!string.IsNullOrEmpty(text));
            }
        }

        private void ClearWarning()
        {
            if (warningPanel != null)
            {
                warningPanel.SetActive(false);
            }
            if (warningTitleText != null) warningTitleText.text = "";
            if (warningMessageText != null) warningMessageText.text = "";
        }

        private void ShowVictoryScreen()
        {
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void ShowDefeatScreen()
        {
            if (defeatPanel != null)
            {
                defeatPanel.SetActive(true);
            }
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>
        /// Reinicia la misión actual (vincular al botón 'Reiniciar' / 'Restart' del panel de derrota).
        /// </summary>
        public void OnRestartMission()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>
        /// Regresa al Menú Principal (vincular al botón 'Menú Principal' / 'Main Menu' de los paneles de resultado).
        /// </summary>
        public void OnGoToMainMenu()
        {
            Time.timeScale = 1f;
            if (Application.CanStreamedLevelBeLoaded("MainMenu"))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(0);
            }
        }
    }
}
