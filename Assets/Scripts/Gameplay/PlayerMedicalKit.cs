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
            // Inspección visual/feedback en tiempo real al mirar heridas o al paciente
            UpdateHoverInspection();

            // Interacción con clic izquierdo (o trigger de VR)
            if (Input.GetMouseButtonDown(0))
            {
                PerformInteraction();
            }
        }

        private void UpdateHoverInspection()
        {
            Ray ray;
            if (playerCamera != null)
            {
                ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
            }
            else
            {
                ray = new Ray(transform.position, transform.forward);
            }

            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionLayerMask))
            {
                // 1. Verificar si está apuntando a una Herida/Parte del Cuerpo (InjuryHandler)
                InjuryHandler injury = hit.collider.GetComponentInParent<InjuryHandler>();
                if (injury != null)
                {
                    string prompt = "";
                    if (currentlyEquippedTool == MedicalToolType.Stethoscope)
                    {
                        if (injury.bodyPart == BodyPart.Torso)
                            prompt = "[ Pecho del Paciente ]\n(Haz Clic para auscultar signos vitales)";
                        else
                            prompt = $"[ {injury.bodyPart} ]\n(El estetoscopio solo puede usarse en el Tronco)";
                    }
                    else
                    {
                        string statusText = "";
                        if (injury.bleedingLevel > 67f) statusText = "Hemorragia Abierta";
                        else if (injury.bleedingLevel > 33f) statusText = "Parcialmente Contenida";
                        else if (injury.bleedingLevel > 0f) statusText = "Sangrado Leve (Casi Controlada)";
                        else statusText = "Sin heridas visibles";

                        string status = injury.isTreated ? "Estable / Sin sangrado" : $"{statusText} ({injury.bleedingLevel:F0}%)";
                        string iName = string.IsNullOrEmpty(injury.injuryName) ? "Zona intacta" : injury.injuryName;
                        string actionPrompt = injury.isTreated ? "" : "\n(Usa Gasa para tratar)";
                        prompt = $"[ {injury.bodyPart}: {iName} ]\nEstado: {status}{actionPrompt}";
                    }

                    if (hud != null) hud.UpdateInteractionPrompt(prompt);
                    return;
                }

                // 2. Verificar si apuntó al Paciente (Fallback si el collider no tiene InjuryHandler)
                PatientState patient = hit.collider.GetComponentInParent<PatientState>();
                if (patient != null)
                {
                    string prompt = "[ Paciente Herido ]\n(Apunta a una parte específica del cuerpo)";
                    if (hud != null) hud.UpdateInteractionPrompt(prompt);
                    return;
                }
            }

            // Ocultar el texto de inspección al dejar de mirar el objetivo
            if (hud != null) hud.UpdateInteractionPrompt("");
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
                    if (hud != null) hud.ShowWarning("¡BIOSEGURIDAD!", "Guantes de médico equipados.", NotificationType.Info, 2f);
                }
                else
                {
                    if (hud != null) hud.ShowWarning("¡BIOSEGURIDAD!", "Guantes de médico retirados.", NotificationType.Warning, 2f);
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
                if (injury.isTreated)
                {
                    if (hud != null) hud.ShowWarning("¡ZONA ESTABLE!", "Esta parte del cuerpo no requiere tratamiento con gasa.", NotificationType.Warning, 2.5f);
                    return;
                }

                // Curar en porciones del 25% por cada aplicación de gasa
                bool success = injury.TryTreatWithGauze(hasGlovesEquipped, out string message, 25f);
                string title = success ? "¡GASA APLICADA!" : "¡ADVERTENCIA DE BIOSEGURIDAD!";
                if (success && injury.isTreated) title = "¡HERIDA SELLADA TOTALMENTE!";
                
                NotificationType type = success ? NotificationType.Info : NotificationType.Danger;
                if (hud != null) hud.ShowWarning(title, message, type, 3.5f);
            }
            else if (currentlyEquippedTool == MedicalToolType.Stethoscope)
            {
                if (injury.bodyPart == BodyPart.Torso)
                {
                    PatientState patient = injury.GetComponentInParent<PatientState>();
                    if (patient != null)
                    {
                        patient.AuscultateVitals();
                        if (hud != null) hud.ShowWarning("¡AUSCULTACIÓN REALIZADA!", "Signos vitales del paciente actualizados en el HUD.", NotificationType.Info, 2.5f);
                    }
                }
                else
                {
                    if (hud != null) hud.ShowWarning("¡USO INCORRECTO!", "El estetoscopio solo puede usarse en el pecho/tronco del paciente.", NotificationType.Warning, 3f);
                }
            }
            else
            {
                if (hud != null)
                {
                    hud.ShowWarning("¡HERRAMIENTA REQUERIDA!", "Selecciona una herramienta médica (Gasa o Estetoscopio) en el Menú Radial (Q/TAB).", NotificationType.Warning, 3f);
                }
            }
        }

        private void HandlePatientInteraction(PatientState patient)
        {
            if (currentlyEquippedTool == MedicalToolType.Stethoscope)
            {
                if (hud != null) hud.ShowWarning("¡USO INCORRECTO!", "Apunta específicamente al pecho/tronco del paciente para auscultar.", NotificationType.Warning, 3f);
            }
            else if (currentlyEquippedTool == MedicalToolType.None)
            {
                if (hud != null) hud.ShowWarning("¡BOTIQUÍN MÉDICO!", "Abre el menú radial (tecla Q o TAB) para seleccionar una herramienta médica.", NotificationType.Warning, 2.5f);
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
