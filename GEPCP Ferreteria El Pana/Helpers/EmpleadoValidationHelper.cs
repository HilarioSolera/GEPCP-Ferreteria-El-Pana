using GEPCP_Ferreteria_El_Pana.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GEPCP_Ferreteria_El_Pana.Helpers
{
    /// <summary>
    /// Validaciones centralizadas para operaciones de empleados
    /// </summary>
    public static class EmpleadoValidationHelper
    {
        /// <summary>
        /// Valida que un empleado esté activo antes de permitir operaciones de deducciones/devengos
        /// </summary>
        /// <param name="empleado">El empleado a validar</param>
        /// <param name="modelState">ModelStateDictionary del controlador para agregar errores</param>
        /// <param name="operacion">Descripción de la operación (ej: "crear préstamo", "registrar vacación")</param>
        /// <returns>True si el empleado está activo, False si está inactivo</returns>
        public static bool ValidarEmpleadoActivo(Empleado? empleado, ModelStateDictionary modelState, string operacion = "realizar esta operación")
        {
            if (empleado == null)
            {
                modelState.AddModelError(string.Empty, "Empleado no encontrado.");
                return false;
            }

            if (!empleado.Activo)
            {
                modelState.AddModelError(string.Empty, 
                    $"No se puede {operacion} porque el empleado {empleado.PrimerApellido} {empleado.Nombre} está INACTIVO. " +
                    $"Debe activar al empleado antes de registrar deducciones o devengos.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Valida que la fecha de ingreso no sea posterior a una fecha de operación
        /// </summary>
        public static bool ValidarFechaContraIngreso(
            Empleado empleado, 
            DateTime fechaOperacion, 
            ModelStateDictionary modelState, 
            string nombreCampo,
            string tipoOperacion = "operación")
        {
            if (fechaOperacion < empleado.FechaIngreso)
            {
                modelState.AddModelError(nombreCampo, 
                    $"La fecha de {tipoOperacion} no puede ser anterior a la fecha de ingreso del empleado ({empleado.FechaIngreso:dd/MM/yyyy}).");
                return false;
            }
            return true;
        }
    }
}
