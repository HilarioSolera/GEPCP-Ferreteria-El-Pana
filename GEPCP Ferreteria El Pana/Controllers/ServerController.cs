using Microsoft.AspNetCore.Mvc;
using GEPCP_Ferreteria_El_Pana.Services;

namespace GEPCP_Ferreteria_El_Pana.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServerController : ControllerBase
    {
        private readonly IHostApplicationLifetime _lifetime;

        public ServerController(IHostApplicationLifetime lifetime)
        {
            _lifetime = lifetime;
        }

        [HttpPost("shutdown")]
        public IActionResult Shutdown()
        {
            Task.Run(async () =>
            {
                await Task.Delay(500);
                _lifetime.StopApplication();
            });

            return NoContent();
        }

        [HttpPost("ping")]
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            // Actualizar el timestamp del último ping recibido
            try
            {
                ServerMonitor.UpdateLastPing();
                return Ok(new { message = "Pong", time = DateTime.UtcNow });
            }
            catch
            {
                return Ok(new { message = "Pong" });
            }
        }
    }
}


