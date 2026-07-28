using UnityEngine;
using UnityEngine.UI;

namespace RescateVR.UI
{
    /// <summary>
    /// Aplica automáticamente todas las configuraciones guardadas en PlayerPrefs (Volumen, Brillo y Sensibilidad)
    /// al iniciar cualquier escena (Main Menu o Gameplay), sin necesidad de abrir el Menú de Opciones.
    /// </summary>
    public class SettingsApplier : MonoBehaviour
    {
        [Header("Referencias (Auto-busca si están vacías)")]
        [Tooltip("Imagen UI transparente que cubre la pantalla para ajustar el brillo")]
        public Image brightnessOverlay;

        [Tooltip("Script FirstPersonLook de la cámara del jugador (solo en la escena Gameplay)")]
        public FirstPersonLook cameraLook;

        void Awake()
        {
            ApplySavedSettings();
        }

        /// <summary>
        /// Aplica inmediatamente el volumen, brillo y sensibilidad guardados en PlayerPrefs.
        /// </summary>
        public void ApplySavedSettings()
        {
            // 1. Aplicar Volumen General del Audio
            float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
            AudioListener.volume = savedVolume;

            // 2. Aplicar Brillo de Pantalla (Panel Overlay)
            if (brightnessOverlay == null)
            {
                brightnessOverlay = FindBrightnessOverlay();
            }

            if (brightnessOverlay != null)
            {
                // Asegurar que el overlay no bloquee la interacción de clics
                brightnessOverlay.raycastTarget = false;

                // Asegurar que el GameObject del brillo esté activo en la jerarquía del Canvas
                if (!brightnessOverlay.gameObject.activeSelf)
                {
                    brightnessOverlay.gameObject.SetActive(true);
                }

                float savedBrightness = PlayerPrefs.GetFloat("ScreenBrightness", 1.0f);
                float alpha = Mathf.Clamp01(1f - savedBrightness) * 0.7f;
                Color c = brightnessOverlay.color;
                brightnessOverlay.color = new Color(c.r, c.g, c.b, alpha);
            }

            // 3. Aplicar Sensibilidad de Cámara
            if (cameraLook == null)
            {
                cameraLook = Object.FindFirstObjectByType<FirstPersonLook>(FindObjectsInactive.Include);
            }

            if (cameraLook != null)
            {
                float savedSensitivity = PlayerPrefs.GetFloat("CameraSensitivity", 2.0f);
                cameraLook.sensitivity = savedSensitivity;
            }
        }

        private Image FindBrightnessOverlay()
        {
            if (brightnessOverlay != null) return brightnessOverlay;

            Image[] allImages = Resources.FindObjectsOfTypeAll<Image>();
            foreach (Image img in allImages)
            {
                if (img.gameObject.scene.isLoaded && img.gameObject.name.Equals("BrightnessOverlay", System.StringComparison.OrdinalIgnoreCase))
                {
                    return img;
                }
            }

            return null;
        }
    }
}
