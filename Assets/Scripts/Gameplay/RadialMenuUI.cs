using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RescateVR.Gameplay
{
    /// <summary>
    /// Gestiona el Menú Radial UI flotante del Botiquín para seleccionar herramientas direccionalmente.
    /// Tecla de acceso predeterminada: Q
    /// </summary>
    public class RadialMenuUI : MonoBehaviour
    {
        [Header("Referencias")]
        public PlayerMedicalKit medicalKit;
        public GameObject radialPanel;

        [Header("Teclas de Acceso")]
        public KeyCode toggleKey = KeyCode.Q;

        [Header("Secciones Radiales (Textos o Destacados UI)")]
        public TextMeshProUGUI topText;     // Norte: Guantes
        public TextMeshProUGUI rightText;   // Este: Gasa
        public TextMeshProUGUI bottomText;  // Sur: Estetoscopio
        public TextMeshProUGUI leftText;    // Oeste: Ninguna

        private bool isOpen = false;
        private MedicalToolType currentHoverTool = MedicalToolType.None;

        void Start()
        {
            if (medicalKit == null)
            {
                medicalKit = Object.FindFirstObjectByType<PlayerMedicalKit>();
            }

            if (radialPanel != null)
            {
                radialPanel.SetActive(false);
            }
        }

        void Update()
        {
            // Abrir menú al presionar Q
            if (Input.GetKeyDown(toggleKey))
            {
                OpenMenu();
            }

            // Cerrar menú al soltar Q o seleccionar
            if (Input.GetKeyUp(toggleKey) && isOpen)
            {
                SelectCurrentHoverTool();
                CloseMenu();
            }

            if (isOpen)
            {
                CalculateDirectionalSelection();
            }
        }

        public void OpenMenu()
        {
            isOpen = true;
            if (radialPanel != null) radialPanel.SetActive(true);
        }

        public void CloseMenu()
        {
            isOpen = false;
            if (radialPanel != null) radialPanel.SetActive(false);
        }

        private void CalculateDirectionalSelection()
        {
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Vector2 mousePos = Input.mousePosition;
            Vector2 dir = mousePos - screenCenter;

            if (dir.magnitude < 30f) // Zona muerta central
            {
                currentHoverTool = MedicalToolType.None;
                HighlightSection(currentHoverTool);
                return;
            }

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            // Dividir en 4 sectores cuadrantes
            if (angle >= 45f && angle < 135f)
            {
                currentHoverTool = MedicalToolType.Gloves;      // Norte
            }
            else if (angle >= 315f || angle < 45f)
            {
                currentHoverTool = MedicalToolType.Gauze;       // Este
            }
            else if (angle >= 225f && angle < 315f)
            {
                currentHoverTool = MedicalToolType.Stethoscope; // Sur
            }
            else
            {
                currentHoverTool = MedicalToolType.None;        // Oeste
            }

            HighlightSection(currentHoverTool);
        }

        private void HighlightSection(MedicalToolType tool)
        {
            SetTextHighlight(topText, tool == MedicalToolType.Gloves);
            SetTextHighlight(rightText, tool == MedicalToolType.Gauze);
            SetTextHighlight(bottomText, tool == MedicalToolType.Stethoscope);
            SetTextHighlight(leftText, tool == MedicalToolType.None);
        }

        private void SetTextHighlight(TextMeshProUGUI text, bool isHighlighted)
        {
            if (text == null) return;
            text.color = isHighlighted ? Color.yellow : Color.white;
            text.fontSize = isHighlighted ? 22 : 18;
        }

        private void SelectCurrentHoverTool()
        {
            if (medicalKit != null)
            {
                medicalKit.EquipTool(currentHoverTool);
            }
        }
    }
}
