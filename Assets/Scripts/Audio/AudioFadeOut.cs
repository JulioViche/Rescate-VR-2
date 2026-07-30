using UnityEngine;
using System.Collections;

namespace RescateVR.Audio
{
    /// <summary>
    /// Hace un Fade Out (desvanecimiento de volumen) automático de un AudioSource.
    /// Útil para que la música o efectos no terminen de golpe.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioFadeOut : MonoBehaviour
    {
        [Tooltip("Tiempo a esperar antes de empezar a bajar el volumen")]
        public float delayBeforeFade = 3f;

        [Tooltip("Tiempo que tardará el volumen en llegar a cero (Fade Out)")]
        public float fadeDuration = 2f;

        private AudioSource[] audioSources;
        private float[] originalVolumes;

        void OnEnable()
        {
            // Obtener todos los AudioSources unidos a este panel
            audioSources = GetComponents<AudioSource>();
            originalVolumes = new float[audioSources.Length];

            for (int i = 0; i < audioSources.Length; i++)
            {
                originalVolumes[i] = audioSources[i].volume;
            }
            
            // Iniciar el fade out
            StartCoroutine(FadeOutCoroutine());
        }

        private IEnumerator FadeOutCoroutine()
        {
            // Usamos WaitForSecondsRealtime para que funcione incluso si el juego está pausado (Time.timeScale = 0)
            yield return new WaitForSecondsRealtime(delayBeforeFade);

            float currentTime = 0;

            while (currentTime < fadeDuration)
            {
                // Usar unscaledDeltaTime por si el tiempo del juego está congelado
                currentTime += Time.unscaledDeltaTime;
                
                // Reducir el volumen gradualmente de todos los audios
                for (int i = 0; i < audioSources.Length; i++)
                {
                    audioSources[i].volume = Mathf.Lerp(originalVolumes[i], 0f, currentTime / fadeDuration);
                }
                
                yield return null; // Esperar al siguiente frame
            }

            // Asegurarnos de que queden en 0 y detener los audios
            for (int i = 0; i < audioSources.Length; i++)
            {
                audioSources[i].volume = 0f;
                audioSources[i].Stop();

                // Restaurar el volumen original por si el objeto se vuelve a apagar/prender en el futuro
                audioSources[i].volume = originalVolumes[i];
            }
        }
    }
}
