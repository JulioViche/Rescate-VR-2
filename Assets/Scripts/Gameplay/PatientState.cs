using System;
using UnityEngine;
using UnityEngine.Events;

namespace RescateVR.Gameplay
{
    /// <summary>
    /// Administra los signos vitales, el nivel de sangre, temporizador de la misión (5 min) 
    /// y las condiciones de victoria/derrota del paciente herido.
    /// </summary>
    public class PatientState : MonoBehaviour
    {
        [Header("Configuración de Sangre y Salud")]
        [Tooltip("Porcentaje máximo de sangre/salud (100%)")]
        [Range(0f, 100f)]
        public float maxBlood = 100f;

        [Tooltip("Nivel actual de sangre del paciente (deslizable en tiempo real desde el Inspector)")]
        [Range(0f, 100f)]
        public float currentBlood = 100f;

        [Tooltip("Tasa de desangrado acumulada (% de sangre perdido por segundo)")]
        public float currentBleedingRate = 0f;

#if UNITY_EDITOR
        private void OnValidate()
        {
            currentBlood = Mathf.Clamp(currentBlood, 0f, maxBlood);
            if (Application.isPlaying)
            {
                OnBloodChanged?.Invoke(currentBlood, maxBlood);
            }
        }
#endif

        [Header("Signos Vitales")]
        public int heartRateBPM = 75;      // Pulso cardíaco (Normal: 60-100)
        public int respirationRPM = 16;     // Ritmo respiratorio (Normal: 12-20)
        public int systolicBP = 120;        // Presión sistólica
        public int diastolicBP = 80;        // Presión diastólica

        [Header("Misión y Temporizador (5 Minutos)")]
        [Tooltip("Duración de la misión hasta que llega la ambulancia (segundos). Default: 300s (5 min)")]
        public float missionDuration = 300f;

        [Tooltip("Tiempo restante para la llegada de la ambulancia")]
        public float timeRemaining;
        public float TimeRemaining => timeRemaining;

        [Header("Estado del Juego")]
        public bool isDead = false;
        public bool isVictory = false;
        public bool isPaused = false;

        [Header("Eventos de Unity (Asignables en el Inspector o vía C#)")]
        public UnityEvent<float, float> OnBloodChanged;      // (currentBlood, maxBlood)
        public UnityEvent<float> OnTimeUpdated;              // (timeRemaining)
        public UnityEvent<int, int, int, int> OnVitalsUpdated; // (bpm, rpm, sys, dia)
        public UnityEvent OnPatientDied;                     // Evento al morir (Derrota)
        public UnityEvent OnMissionVictory;                  // Evento al llegar ambulancia (Victoria)

        void Awake()
        {
            currentBlood = maxBlood;
            timeRemaining = missionDuration;
        }

        void Start()
        {
            // Notificar estado inicial
            OnBloodChanged?.Invoke(currentBlood, maxBlood);
            OnTimeUpdated?.Invoke(timeRemaining);
            OnVitalsUpdated?.Invoke(heartRateBPM, respirationRPM, systolicBP, diastolicBP);
        }

        void Update()
        {
            if (isDead || isVictory || isPaused || Time.timeScale == 0f) return;

            // 1. Procesar desangrado en tiempo real
            if (currentBleedingRate > 0f)
            {
                currentBlood -= currentBleedingRate * Time.deltaTime;
                currentBlood = Mathf.Clamp(currentBlood, 0f, maxBlood);
                OnBloodChanged?.Invoke(currentBlood, maxBlood);

                // Alterar signos vitales por pérdida de sangre (Shock)
                UpdateVitalsBasedOnBloodLoss();

                if (currentBlood <= 0f)
                {
                    Die();
                    return;
                }
            }

            // 2. Procesar temporizador de la ambulancia (5 minutos)
            timeRemaining -= Time.deltaTime;
            timeRemaining = Mathf.Max(0f, timeRemaining);
            OnTimeUpdated?.Invoke(timeRemaining);

            if (timeRemaining <= 0f)
            {
                TriggerVictory();
            }
        }

        /// <summary>
        /// Agrega una tasa de desangrado acumulada por una herida abierta.
        /// </summary>
        public void AddBleedingRate(float rate)
        {
            currentBleedingRate += rate;
        }

        /// <summary>
        /// Reduce la tasa de desangrado al tratar una herida.
        /// </summary>
        public void ReduceBleedingRate(float rate)
        {
            currentBleedingRate = Mathf.Max(0f, currentBleedingRate - rate);
        }

        /// <summary>
        /// Auscultar / medir los signos vitales con el estetoscopio. Notifica los datos actualizados a la UI.
        /// </summary>
        [ContextMenu("Probar Auscultación (Vitals)")]
        public void AuscultateVitals()
        {
            OnVitalsUpdated?.Invoke(heartRateBPM, respirationRPM, systolicBP, diastolicBP);
        }

        private void UpdateVitalsBasedOnBloodLoss()
        {
            float bloodPercentage = currentBlood / maxBlood;

            if (bloodPercentage < 0.4f) // Shock Severo
            {
                heartRateBPM = UnityEngine.Random.Range(130, 160);
                respirationRPM = UnityEngine.Random.Range(28, 35);
                systolicBP = 70;
                diastolicBP = 40;
            }
            else if (bloodPercentage < 0.7f) // Shock Moderado
            {
                heartRateBPM = UnityEngine.Random.Range(100, 125);
                respirationRPM = UnityEngine.Random.Range(22, 27);
                systolicBP = 95;
                diastolicBP = 60;
            }
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;
            Debug.Log("[PatientState] El paciente ha fallecido por desangrado. ¡DERROTA!");
            OnPatientDied?.Invoke();
        }

        private void TriggerVictory()
        {
            if (isVictory || isDead) return;
            isVictory = true;
            Debug.Log("[PatientState] ¡Llegó la ambulancia! El paciente sobrevivió. ¡VICTORIA!");
            OnMissionVictory?.Invoke();
        }
    }
}
