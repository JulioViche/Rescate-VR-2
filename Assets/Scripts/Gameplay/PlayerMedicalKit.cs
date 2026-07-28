using UnityEngine;

namespace RescateVR.Gameplay
{
    public enum MedicalToolType
    {
        None,
        Gloves,        // Guantes médicos (Bioseguridad)
        Gauze,         // Gasa / Vendaje (Curación de hemorragia)
        Stethoscope,   // Estetoscopio (Auscultación de signos vitales)
        Flashlight     // Linterna (Inspección de pupilas/vía aérea)
    }

    /// <summary>
    /// Administra el equipo médico cargado por el jugador, los guantes de bioseguridad equipados
    /// y la interacción de herramientas con el paciente.
    /// </summary>
    public class PlayerMedicalKit : MonoBehaviour
    {
        [Header("Estado de Herramientas")]
        public MedicalToolType currentlyEquippedTool = MedicalToolType.None;
        public bool hasGlovesEquipped = false;

        [Header("Referencias de UI e Interacción")]
        public PatientHUD hud;
        public Camera playerCamera;
        public float interactionDistance = 3.5f;
        public LayerMask interactionLayerMask = ~0; // Todo por defecto

        void Awake()
        {
            if (hud == null)
            {
                hud = Object.FindObjectOfType<PatientHUD>();
            }
        }

        void Start()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (hud == null)
            {
                hud = Object.FindObjectOfType<PatientHUD>();
            }

            UpdateHUDToolStatus();
        }

        void Update()
        {
            // Interacción con clic izquierdo (o trigger de VR)
            if (Input.GetMouseButtonDown(0))
            {
                PerformInteraction();
            }
        }

        /// <summary>
        /// Equipa una herramienta médica desde el Menú Radial UI o botones.
        /// </summary>
        public void EquipTool(MedicalToolType tool)
        {
            if (tool == MedicalToolType.Gloves)
            {
                // Alternar estado (Toggle: Poner / Quitar guantes)
                hasGlovesEquipped = !hasGlovesEquipped;

                if (hasGlovesEquipped)
                {
                    if (hud != null) hud.ShowWarning("¡Guantes de médico puestos!", 2f);
                }
                else
                {
                    if (hud != null) hud.ShowWarning("¡Guantes de médico retirados!", 2f);
                }

                currentlyEquippedTool = tool;
            }
            else
            {
                currentlyEquippedTool = tool;
            }

            UpdateHUDToolStatus();
        }

        // Métodos públicos sencillos para vincular en botones UI (OnClick):
        public void SelectGloves() => EquipTool(MedicalToolType.Gloves);
        public void SelectGauze() => EquipTool(MedicalToolType.Gauze);
        public void SelectStethoscope() => EquipTool(MedicalToolType.Stethoscope);
        public void SelectNone() => EquipTool(MedicalToolType.None);

        /// <summary>
        /// Realiza un raycast desde la cámara/cursor para aplicar la herramienta equipada sobre el paciente o sus heridas.
        /// </summary>
        public void PerformInteraction()
        {
            Ray ray;
            if (playerCamera != null)
            {
                ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            }
            else
            {
                ray = new Ray(transform.position, transform.forward);
            }

            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionLayerMask))
            {
                // 1. Verificar si apuntó a una Herida (InjuryHandler)
                InjuryHandler injury = hit.collider.GetComponentInParent<InjuryHandler>();
                if (injury != null)
                {
                    HandleInjuryInteraction(injury);
                    return;
                }

                // 2. Verificar si apuntó al Paciente (PatientState)
                PatientState patient = hit.collider.GetComponentInParent<PatientState>();
                if (patient != null)
                {
                    HandlePatientInteraction(patient);
                    return;
                }
            }
        }

        private void HandleInjuryInteraction(InjuryHandler injury)
        {
            if (currentlyEquippedTool == MedicalToolType.Gauze)
            {
                bool success = injury.TryTreatWithGauze(hasGlovesEquipped, out string message);
                if (hud != null) hud.ShowWarning(message, 3f);
            }
            else
            {
                if (hud != null)
                {
                    hud.ShowWarning("Para curar esta herida, selecciona la Gasa/Vendaje en el menú radial (Q).", 3f);
                }
            }
        }

        private void HandlePatientInteraction(PatientState patient)
        {
            if (currentlyEquippedTool == MedicalToolType.Stethoscope)
            {
                patient.AuscultateVitals();
                if (hud != null) hud.ShowWarning("Auscultación realizada: Signos vitales actualizados en HUD.", 2.5f);
            }
            else if (currentlyEquippedTool == MedicalToolType.None)
            {
                if (hud != null) hud.ShowWarning("Abre el menú radial (tecla Q) para seleccionar una herramienta médica.", 2.5f);
            }
        }

        private void UpdateHUDToolStatus()
        {
            if (hud != null)
            {
                hud.UpdateEquippedToolUI(currentlyEquippedTool, hasGlovesEquipped);
            }
        }
    }
}
