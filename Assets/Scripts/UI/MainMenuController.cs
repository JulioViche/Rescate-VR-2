using UnityEngine;
using UnityEngine.SceneManagement;
using RescateVR.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Paneles del Menú Principal")]
    [Tooltip("Panel principal con los botones Jugar, Opciones y Salir")]
    public GameObject mainMenuPanel;

    [Tooltip("Panel o GameObject que contiene el Menú de Opciones")]
    public GameObject optionsMenuPanel;

    [Tooltip("Panel de Créditos (opcional)")]
    public GameObject creditsPanel;

    public void OnPlayClicked()
    {
        SceneManager.LoadScene("Game");
    }

    /// <summary>
    /// Abre el menú de opciones desactivando el panel principal.
    /// Vincular al botón 'Opciones' (OnClick).
    /// </summary>
    public void OnOpenOptionsClicked()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (optionsMenuPanel != null) optionsMenuPanel.SetActive(true);
    }

    /// <summary>
    /// Cierra el menú de opciones y vuelve al menú principal.
    /// Vincular al botón 'Volver' de Opciones (OnClick).
    /// </summary>
    public void OnCloseOptionsClicked()
    {
        if (optionsMenuPanel != null) optionsMenuPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void OnQuitClicked()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}