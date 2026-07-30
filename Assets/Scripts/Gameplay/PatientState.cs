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
            // Variables Base del Modelo Matemático
            float vSangre = 1f - (currentBlood / maxBlood); // 0.0 (sano) a 1.0 (desangrado total)
            
            float sumInternas = 0f;
            foreach (var injury in registeredInjuries)
            {
                sumInternas += injury.internalInjuryLevel;
            }
            float vInternas = Mathf.Clamp01(sumInternas / 100f); // 0.0 a 1.0 (tope de 100% como daño interno crítico)

            float vTiempo = Mathf.Clamp((missionDuration - timeRemaining) / 60f, 0f, 10f); // Minutos transcurridos

            // Valores base sanos
            float baseHR = 75f;
            float baseResp = 16f;
            float baseSys = 120f;
            float baseDia = 80f;

            // Fórmulas Médicas
            float hr = baseHR + (vSangre * 60f) + (vInternas * 30f) + (vTiempo * 5f);
            float resp = baseResp + (vSangre * 15f) + (vInternas * 10f) + (vTiempo * 2f);
            float sys = baseSys - (vSangre * 60f) - (vInternas * 20f) - (vTiempo * 5f);
            float dia = baseDia - (vSangre * 40f) - (vInternas * 15f) - (vTiempo * 3f);

            if (vInternas > 0.8f) // Colapso mecánico de la respiración por trauma masivo
            {
                resp -= (vInternas - 0.8f) * 50f;
            }

            // Cuando la sangre es menor a 20%, los órganos comienzan a apagarse
            float bloodPercentage = currentBlood / maxBlood;
            if (bloodPercentage < 0.2f)
            {
                // Multiplicador que va de 1.0 (en 20% sangre) hasta 0.0 (en 0% sangre)
                float deathFade = bloodPercentage / 0.2f; 
                hr *= deathFade;
                resp *= deathFade;
                sys *= deathFade;
                dia *= deathFade;
            }

            // Aplicar fluctuaciones aleatorias leves (se detienen si llega a 0 absoluto)
            bool isDead = bloodPercentage <= 0.01f;
            heartRateBPM = Mathf.Clamp(Mathf.RoundToInt(hr) + (!isDead ? UnityEngine.Random.Range(-3, 4) : 0), 0, 200);
            respirationRPM = Mathf.Clamp(Mathf.RoundToInt(resp) + (!isDead ? UnityEngine.Random.Range(-2, 3) : 0), 0, 45);
            
            systolicBP = Mathf.Clamp(Mathf.RoundToInt(sys) + (!isDead ? UnityEngine.Random.Range(-3, 4) : 0), 0, 180);
            diastolicBP = Mathf.Clamp(Mathf.RoundToInt(dia) + (!isDead ? UnityEngine.Random.Range(-2, 3) : 0), 0, 120);
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
