using UnityEngine;
using RescateVR.Gameplay;

namespace RescateVR.Audio
{
    /// <summary>
    /// Gestiona la música de fondo de la escena de juego.
    /// Pausa la música cuando se abre el menú de opciones (Time.timeScale = 0)
    /// y cambia a la música respectiva en las pantallas de victoria/derrota.
    /// </summary>
    public class GameMusicManager : MonoBehaviour
    {
        [Header("Configuración de Audio")]
        public AudioSource musicSource;
        [Tooltip("AudioSource opcional para el sonido ambiental (se silenciará en menús)")]
        public AudioSource ambientSource;

        [Header("Canciones")]
        public AudioClip gameplayMusic;
        public AudioClip victoryMusic;
        public AudioClip defeatMusic;
        
        [Header("Sonido Ambiental (Opcional)")]
        public AudioClip ambientSound;

        private PatientState patientState;
        private bool isGameOver = false;

        void Start()
        {
            patientState = FindObjectOfType<PatientState>();
            
            // Suscribirnos a los eventos de fin de juego del paciente
            if (patientState != null)
            {
                patientState.OnPatientDied.AddListener(PlayDefeatMusic);
                patientState.OnMissionVictory.AddListener(PlayVictoryMusic);
            }

            // Iniciar la música de Gameplay
            if (musicSource != null && gameplayMusic != null)
            {
                musicSource.clip = gameplayMusic;
                musicSource.loop = true;
                musicSource.Play();
            }

            // Iniciar el Sonido Ambiental
            if (ambientSource != null && ambientSound != null)
            {
                ambientSource.clip = ambientSound;
                ambientSource.loop = true;
                ambientSource.Play();
            }
        }

        void Update()
        {
            // Si el juego NO ha terminado, controlamos la pausa de la música
            // basándonos en si el PauseManager detuvo el tiempo (Time.timeScale == 0).
            if (!isGameOver)
            {
                if (Time.timeScale == 0f)
                {
                    if (musicSource != null && musicSource.isPlaying) musicSource.Pause();
                    if (ambientSource != null && ambientSource.isPlaying) ambientSource.Pause();
                }
                else if (Time.timeScale > 0f)
                {
                    if (musicSource != null && !musicSource.isPlaying) musicSource.UnPause();
                    if (ambientSource != null && !ambientSource.isPlaying) ambientSource.UnPause();
                }
            }
        }

        private void PlayDefeatMusic()
        {
            isGameOver = true;
            
            if (ambientSource != null) ambientSource.Stop(); // Apagar ambiente
            
            if (musicSource != null && defeatMusic != null)
            {
                musicSource.Stop();
                musicSource.clip = defeatMusic;
                musicSource.loop = false; // La música de derrota suele ser de una sola vez
                musicSource.Play();
            }
        }

        private void PlayVictoryMusic()
        {
            isGameOver = true;
            
            if (ambientSource != null) ambientSource.Stop(); // Apagar ambiente

            if (musicSource != null && victoryMusic != null)
            {
                musicSource.Stop();
                musicSource.clip = victoryMusic;
                musicSource.loop = false; // La música de victoria suele ser de una sola vez
                musicSource.Play();
            }
        }

        void OnDestroy()
        {
            // Limpieza de eventos
            if (patientState != null)
            {
                patientState.OnPatientDied.RemoveListener(PlayDefeatMusic);
                patientState.OnMissionVictory.RemoveListener(PlayVictoryMusic);
            }
        }
    }
}
