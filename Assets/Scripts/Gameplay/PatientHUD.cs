using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RescateVR.Gameplay
{
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
        public TextMeshProUGUI bloodPercentageText;

        [Header("UI Signos Vitales (Estetoscopio)")]
        public TextMeshProUGUI heartRateText;
        public TextMeshProUGUI respirationText;
        public TextMeshProUGUI bloodPressureText;

        [Header("UI Sliders de Signos Vitales (Opcionales)")]
        [Tooltip("Slider UI para el pulso (BPM)")]
        public Slider heartRateSlider;

        [Tooltip("Slider UI para la respiración (RPM)")]
        public Slider respirationSlider;

        [Tooltip("Slider UI para la presión sistólica (mmHg)")]
        public Slider bloodPressureSlider;

        [Header("Valores Máximos para Sliders de Signos Vitales")]
        public float maxHeartRateBPM = 200f;
        public float maxRespirationRPM = 50f;
        public float maxBloodPressuremmHg = 200f;

        [Header("UI Bioseguridad / Herramienta Equipada")]
        public TextMeshProUGUI equippedToolText;
        public TextMeshProUGUI warningMessageText;

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

        public void UpdateBloodUI(float current, float max)
        {
            float pct = Mathf.Clamp01(current / max);
            if (bloodSlider != null) bloodSlider.value = pct;
            if (bloodPercentageText != null) bloodPercentageText.text = $"{current:F0}%";
        }

        public void UpdateTimerUI(float secondsRemaining)
        {
            if (timerText == null) return;
            int minutes = Mathf.FloorToInt(secondsRemaining / 60f);
            int seconds = Mathf.FloorToInt(secondsRemaining % 60f);
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

        public void UpdateEquippedToolUI(string toolName, bool hasGloves)
        {
            if (equippedToolText != null)
            {
                string glovesStatus = hasGloves ? "<color=green>[Guantes Puestos]</color>" : "<color=red>[Sin Guantes]</color>";
                equippedToolText.text = $"Herramienta: {toolName} {glovesStatus}";
            }
        }

        public void ShowWarning(string message, float duration = 2.5f)
        {
            if (warningMessageText != null)
            {
                warningMessageText.text = message;
                CancelInvoke(nameof(ClearWarning));
                Invoke(nameof(ClearWarning), duration);
            }
        }

        private void ClearWarning()
        {
            if (warningMessageText != null) warningMessageText.text = "";
        }

        private void ShowVictoryScreen()
        {
            if (victoryPanel != null) victoryPanel.SetActive(true);
        }

        private void ShowDefeatScreen()
        {
            if (defeatPanel != null) defeatPanel.SetActive(true);
        }
    }
}
