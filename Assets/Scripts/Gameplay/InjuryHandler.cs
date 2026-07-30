using UnityEngine;

namespace RescateVR.Gameplay
{
    public enum BodyPart
    {
        Head,
        Torso,
        LeftArm,
        RightArm,
        LeftLeg,
        RightLeg,
        Generic
    }

    /// <summary>
    /// Componente adjuntado a las Hitboxes del paciente para manejar las heridas regionales.
    /// </summary>
    public class InjuryHandler : MonoBehaviour
    {
        [Header("Región Anatómica")]
        [Tooltip("Parte del cuerpo a la que pertenece esta hitbox.")]
        public BodyPart bodyPart = BodyPart.Generic;

        [Header("Datos de la Herida")]
        public string injuryName = "Laceración sangrante";

        [Tooltip("Nivel de desangramiento/gravedad de la herida (100% = herida abierta sangrando al máximo, 0% = completamente curada)")]
        [Range(0f, 100f)]
        public float bleedingLevel = 100f;

        [Tooltip("Porcentaje de sangre que pierde el paciente por segundo si la herida está a nivel máximo (100%)")]
        public float maxBleedingRatePerSecond = 1.5f;

        [Header("Efectos Visuales")]
        [Tooltip("Partículas de sangre (opcional)")]
        public ParticleSystem bloodParticleEffect;

        [Tooltip("Renderer del parche/sangre para ocultar al curar (opcional)")]
        public Renderer bloodMeshRenderer;

        public bool isTreated => bleedingLevel <= 0f;

        private PatientState patientState;
        private float currentActiveRate = 0f;

        void Start()
        {
            patientState = GetComponentInParent<PatientState>();
            if (patientState == null)
            {
                patientState = Object.FindFirstObjectByType<PatientState>();
            }

            if (patientState != null)
            {
                patientState.RegisterInjury(this);
                if (bleedingLevel > 0f)
                {
                    currentActiveRate = (bleedingLevel / 100f) * maxBleedingRatePerSecond;
                    patientState.AddBleedingRate(currentActiveRate);
                }
            }

            UpdateVisuals();
        }

        /// <summary>
        /// Intenta tratar la herida con una gasa/vendaje.
        /// Verifica la norma de bioseguridad (guantes obligatorios).
        /// </summary>
        /// <param name="hasGloves">Indica si el jugador tiene los guantes de médico equipados</param>
        /// <param name="resultMessage">Mensaje de retroalimentación generado</param>
        /// <param name="healAmount">Cantidad en porcentaje a reducir del desangrado (Default: 100%)</param>
        /// <returns>Resultado del intento de tratamiento</returns>
        public bool TryTreatWithGauze(bool hasGloves, out string resultMessage, float healAmount = 100f)
        {
            if (bleedingLevel <= 0f)
            {
                resultMessage = $"{injuryName} ya está completamente sellada (Nivel de sangrado: 0%).";
                return true;
            }

            if (!hasGloves)
            {
                resultMessage = "¡ADVERTENCIA DE BIOSEGURIDAD! Debes equiparte los guantes de médico en el Menú Radial antes de tocar la herida.";
                return false;
            }

            // Reducir el nivel de desangramiento de la herida (ej: de 100 a 0)
            float previousLevel = bleedingLevel;
            bleedingLevel = Mathf.Max(0f, bleedingLevel - healAmount);

            // Calcular cuánto desangrado se redujo en PatientState
            float oldRate = (previousLevel / 100f) * maxBleedingRatePerSecond;
            float newRate = (bleedingLevel / 100f) * maxBleedingRatePerSecond;
            float rateReduction = oldRate - newRate;

            if (patientState != null && rateReduction > 0f)
            {
                patientState.ReduceBleedingRate(rateReduction);
                currentActiveRate = newRate;
            }

            UpdateVisuals();

            if (bleedingLevel <= 0f)
            {
                resultMessage = $"¡Herida curada y sellada con éxito! Nivel de sangrado: 0% ({injuryName})";
            }
            else
            {
                resultMessage = $"¡Gasa aplicada! Nivel de sangrado reducido a {bleedingLevel:F0}% ({injuryName})";
            }

            return true;
        }

        private void UpdateVisuals()
        {
            if (bloodParticleEffect != null)
            {
                if (isTreated) bloodParticleEffect.Stop();
                else if (!bloodParticleEffect.isPlaying) bloodParticleEffect.Play();
            }

            if (bloodMeshRenderer != null)
            {
                bloodMeshRenderer.enabled = !isTreated;
            }
        }
    }
}

