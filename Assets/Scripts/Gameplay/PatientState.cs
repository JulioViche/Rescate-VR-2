using System;
using System.Collections.Generic;
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

        // Lista de todas las hitboxes/heridas registradas
        private List<InjuryHandler> registeredInjuries = new List<InjuryHandler>();

        public void RegisterInjury(InjuryHandler injury)
        {
            if (!registeredInjuries.Contains(injury))
            {
                registeredInjuries.Add(injury);
            }
        }

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
            
            // Valores base sanos
            int baseHR = 75;
            int baseResp = 16;
            int baseSys = 120;
            int baseDia = 80;

            // Recopilar impacto por región anatómica
            float headBleeding = 0f;
            float torsoBleeding = 0f;
            float extremitiesBleeding = 0f;

            foreach (var injury in registeredInjuries)
            {
                if (injury.bodyPart == BodyPart.Head) headBleeding += injury.bleedingLevel;
                else if (injury.bodyPart == BodyPart.Torso) torsoBleeding += injury.bleedingLevel;
                else extremitiesBleeding += injury.bleedingLevel; // Brazos y piernas
            }

            // Daño en la cabeza afecta drásticamente la respiración
            int respImpact = (int)(headBleeding * 0.1f) + (bloodPercentage < 0.7f ? 5 : 0) + (bloodPercentage < 0.4f ? 10 : 0);
            
            // Daño en el torso (órganos vitales) provoca caída severa de presión
            int bpDrop = (int)(torsoBleeding * 0.25f) + (bloodPercentage < 0.7f ? 15 : 0) + (bloodPercentage < 0.4f ? 30 : 0);
            
            // Sangrado general y de extremidades obliga al corazón a latir más rápido para compensar (Taquicardia)
            int hrRise = (int)(extremitiesBleeding * 0.2f) + (int)(torsoBleeding * 0.1f) + (bloodPercentage < 0.7f ? 25 : 0) + (bloodPercentage < 0.4f ? 55 : 0);

            // Si el shock es MUY severo (<20%), el corazón empieza a fallar (bradicardia)
            if (bloodPercentage < 0.2f)
            {
                hrRise = -20;
                respImpact = -10; // Falla respiratoria
            }

            // Aplicar fluctuaciones aleatorias leves para realismo médico
            heartRateBPM = Mathf.Clamp(baseHR + hrRise + UnityEngine.Random.Range(-5, 6), 30, 180);
            respirationRPM = Mathf.Clamp(baseResp + respImpact + UnityEngine.Random.Range(-2, 3), 0, 45);
            
            systolicBP = Mathf.Clamp(baseSys - bpDrop + UnityEngine.Random.Range(-4, 5), 40, 180);
            diastolicBP = Mathf.Clamp(baseDia - (bpDrop / 2) + UnityEngine.Random.Range(-3, 4), 20, 120);
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
