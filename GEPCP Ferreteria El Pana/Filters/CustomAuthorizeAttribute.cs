using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GEPCP_Ferreteria_El_Pana.Filters
{
    public class CustomAuthorizeAttribute : ActionFilterAttribute
    {
        private readonly string[] _roles;

        public CustomAuthorizeAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var usuario = context.HttpContext.Session.GetString("Usuario");
            var rol = context.HttpContext.Session.GetString("Rol");

            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(rol) || !_roles.Contains(rol))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}