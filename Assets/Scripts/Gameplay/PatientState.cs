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

        [Header("Eventos de Emergencia (RCP)")]
        [Tooltip("Tiempo en segundos que tiene el jugador para hacer RCP antes de que muera")]
        public float timeAllowedForCPR = 10f;
        [Tooltip("Clicks necesarios en el pecho para salvar el paro respiratorio")]
        public int requiredCprClicks = 5;

        [HideInInspector] public bool isInRespiratoryArrest = false;
        [HideInInspector] public float arrestTimer = 0f;
        [HideInInspector] public int currentCprClicks = 0;
        
        private float arrestCheckTimer = 0f;
        private float arrestImmunityTimer = 10f; // 10s de inmunidad al iniciar

        [Header("Audio: Respiración")]
        [Tooltip("AudioSource adjunto al paciente (preferiblemente en la cabeza o pecho)")]
        public AudioSource breathingAudioSource;
        
        [Tooltip("Audio para respiración normal o lenta (RPM < 25)")]
        public AudioClip normalBreathingClip;
        [Tooltip("Volumen independiente para la respiración normal")]
        [Range(0f, 1f)] public float normalVolume = 0.4f;
        [Tooltip("Pitch (Tono/Velocidad) para la respiración normal")]
        [Range(0.1f, 3f)] public float normalPitch = 1f;

        [Tooltip("Audio para respiración agitada o taquipnea (RPM > 27)")]
        public AudioClip heavyBreathingClip;
        [Tooltip("Volumen independiente para la respiración agitada")]
        [Range(0f, 1f)] public float heavyVolume = 1f;
        [Tooltip("Pitch (Tono/Velocidad) para la respiración agitada")]
        [Range(0.1f, 3f)] public float heavyPitch = 1f;

        [Tooltip("Tiempo en segundos para hacer la transición suave (Crossfade) entre audios")]
        public float audioFadeDuration = 1.5f;

        private AudioSource audioSourceHeavy;
        private float targetVolumeNormal = 0f;
        private float targetVolumeHeavy = 0f;
        private bool isHeavyBreathing = false;
        private bool wasAudioPaused = false;

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
            // Inicializar AudioSources para el Crossfade
            if (breathingAudioSource != null)
            {
                breathingAudioSource.clip = normalBreathingClip;
                breathingAudioSource.loop = true;
                breathingAudioSource.volume = 0f; // Empieza en 0 y hace fade-in en el Update
                breathingAudioSource.pitch = normalPitch;
                breathingAudioSource.Play();

                // Crear un segundo AudioSource dinámico en el MISMO GameObject para evitar desfases 3D (espaciales)
                audioSourceHeavy = breathingAudioSource.gameObject.AddComponent<AudioSource>();
                audioSourceHeavy.spatialBlend = breathingAudioSource.spatialBlend;
                audioSourceHeavy.minDistance = breathingAudioSource.minDistance;
                audioSourceHeavy.maxDistance = breathingAudioSource.maxDistance;
                audioSourceHeavy.rolloffMode = breathingAudioSource.rolloffMode;
                audioSourceHeavy.clip = heavyBreathingClip;
                audioSourceHeavy.loop = true;
                audioSourceHeavy.volume = 0f;
                audioSourceHeavy.pitch = heavyPitch;
                audioSourceHeavy.Play();
            }

            // Notificar estado inicial
            OnBloodChanged?.Invoke(currentBlood, maxBlood);
            OnTimeUpdated?.Invoke(timeRemaining);
            OnVitalsUpdated?.Invoke(heartRateBPM, respirationRPM, systolicBP, diastolicBP);
        }

        void Update()
        {
            bool currentlyPaused = (isDead || isVictory || isPaused || Time.timeScale == 0f);

            // Manejar pausa automática de los sonidos de respiración
            if (currentlyPaused && !wasAudioPaused)
            {
                if (breathingAudioSource != null) breathingAudioSource.Pause();
                if (audioSourceHeavy != null) audioSourceHeavy.Pause();
                wasAudioPaused = true;
            }
            else if (!currentlyPaused && wasAudioPaused)
            {
                if (breathingAudioSource != null) breathingAudioSource.UnPause();
                if (audioSourceHeavy != null) audioSourceHeavy.UnPause();
                wasAudioPaused = false;
            }

            if (currentlyPaused) return;

            // 1. Procesar Paro Respiratorio
            if (isInRespiratoryArrest)
            {
                respirationRPM = 0; // Forzar asfixia
                arrestTimer -= Time.deltaTime;
                
                // Forzar actualización del HUD para mostrar RPM en 0 si está auscultando
                OnVitalsUpdated?.Invoke(heartRateBPM, respirationRPM, systolicBP, diastolicBP);

                if (arrestTimer <= 0f)
                {
                    Die();
                    return;
                }
            }
            else
            {
                // Solo si no está en paro verificamos probabilidad
                if (arrestImmunityTimer > 0f)
                {
                    arrestImmunityTimer -= Time.deltaTime;
                }
                else
                {
                    arrestCheckTimer += Time.deltaTime;
                    if (arrestCheckTimer >= 5f)
                    {
                        arrestCheckTimer = 0f;
                        
                        // Buscar daño del Torso
                        float torsoInternalDamage = 0f;
                        foreach (var injury in registeredInjuries)
                        {
                            if (injury.bodyPart == BodyPart.Torso)
                            {
                                torsoInternalDamage = injury.internalInjuryLevel;
                                break;
                            }
                        }

                        // Si el daño del torso es alto, lanzar el "dado" (máx 15% de chance cada 5s)
                        if (torsoInternalDamage > 20f)
                        {
                            float chance = (torsoInternalDamage / 100f) * 0.15f; 
                            if (UnityEngine.Random.value < chance)
                            {
                                TriggerRespiratoryArrest();
                                return; // Sale del frame para evitar curas cruzadas
                            }
                        }
                    }
                }
            }

            // 2. Procesar desangrado en tiempo real
            if (currentBleedingRate > 0f)
            {
                currentBlood -= currentBleedingRate * Time.deltaTime;
                currentBlood = Mathf.Clamp(currentBlood, 0f, maxBlood);
                
#if UNITY_EDITOR
                if (Application.isPlaying)
                {
                    OnBloodChanged?.Invoke(currentBlood, maxBlood);
                }
#else
                OnBloodChanged?.Invoke(currentBlood, maxBlood);
#endif

                // Alterar signos vitales por pérdida de sangre (Shock)
                UpdateVitalsBasedOnBloodLoss();

                if (currentBlood <= 0f)
                {
                    Die();
                    return;
                }
            }

            UpdateBreathingAudio();

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

            // Asignar los valores calculados basados puramente en las fórmulas (sin aleatoriedad)
            heartRateBPM = Mathf.Clamp(Mathf.RoundToInt(hr), 0, 200);
            respirationRPM = Mathf.Clamp(Mathf.RoundToInt(resp), 0, 45);
            
            systolicBP = Mathf.Clamp(Mathf.RoundToInt(sys), 0, 180);
            diastolicBP = Mathf.Clamp(Mathf.RoundToInt(dia), 0, 120);

            if (isInRespiratoryArrest) respirationRPM = 0; // Forzar de nuevo por si se actualizó
        }

        private void TriggerRespiratoryArrest()
        {
            isInRespiratoryArrest = true;
            arrestTimer = timeAllowedForCPR;
            currentCprClicks = 0;
            respirationRPM = 0;
            OnVitalsUpdated?.Invoke(heartRateBPM, respirationRPM, systolicBP, diastolicBP);

            // Avisar globalmente al jugador con una alerta roja
            PatientHUD hud = FindObjectOfType<PatientHUD>();
            if (hud != null)
            {
                hud.ShowWarning("¡PARO RESPIRATORIO!", "¡El paciente dejó de respirar!\n(Equipa Guantes, suelta herramientas y haz RCP rápido en el pecho)", NotificationType.Danger, 5f);
            }
        }

        public bool ApplyCPR()
        {
            if (!isInRespiratoryArrest) return false;

            currentCprClicks++;
            if (currentCprClicks >= requiredCprClicks)
            {
                isInRespiratoryArrest = false;
                arrestImmunityTimer = 15f; // 15s de inmunidad para que el jugador respire
                UpdateVitalsBasedOnBloodLoss(); // Restaurar signos
                return true; // CPR Completado con éxito
            }
            return false; // Aún requiere más clics
        }

        private void UpdateBreathingAudio()
        {
            if (breathingAudioSource == null || audioSourceHeavy == null) return;

            // Si está muerto o en paro (0 RPM), apagar sonido
            if (respirationRPM <= 0 || isDead)
            {
                targetVolumeNormal = 0f;
                targetVolumeHeavy = 0f;
            }
            else
            {
                // Usar márgenes separados para evitar saltos (Histéresis)
                // Umbral medio (25 - 27 RPM)
                if (respirationRPM < 25)
                {
                    targetVolumeNormal = normalVolume;
                    targetVolumeHeavy = 0f;
                    
                    // Si veníamos de respiración agitada, reiniciamos el tiempo del clip normal
                    if (isHeavyBreathing)
                    {
                        isHeavyBreathing = false;
                        breathingAudioSource.time = 0f;
                        if (!breathingAudioSource.isPlaying) breathingAudioSource.Play();
                    }
                }
                else if (respirationRPM > 27)
                {
                    targetVolumeNormal = 0f;
                    targetVolumeHeavy = heavyVolume;
                    
                    // Si veníamos de respiración normal, reiniciamos el tiempo del clip agitado
                    if (!isHeavyBreathing)
                    {
                        isHeavyBreathing = true;
                        audioSourceHeavy.time = 0f;
                        if (!audioSourceHeavy.isPlaying) audioSourceHeavy.Play();
                    }
                }
                // Si está entre 25 y 27, mantiene el estado actual
            }

            // Suavizar el volumen de ambos audios gradualmente (Crossfade)
            if (audioFadeDuration > 0f)
            {
                breathingAudioSource.volume = Mathf.MoveTowards(breathingAudioSource.volume, targetVolumeNormal, Time.deltaTime / audioFadeDuration);
                audioSourceHeavy.volume = Mathf.MoveTowards(audioSourceHeavy.volume, targetVolumeHeavy, Time.deltaTime / audioFadeDuration);
            }
            else
            {
                breathingAudioSource.volume = targetVolumeNormal;
                audioSourceHeavy.volume = targetVolumeHeavy;
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
