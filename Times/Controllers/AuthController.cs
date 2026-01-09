using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Times.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        [HttpGet("login")]
        public IActionResult Login()
        {
            return Ok("Login successful");
        }
    }
}