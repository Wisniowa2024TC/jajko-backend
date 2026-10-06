using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JajkoBackend.Controllers;

[ApiController]
[Route("/api/orders")]
public class OrdersController : ControllerBase
{
    [HttpGet("example")]
    public async Task<IActionResult> Example()
    {
        return Ok("Hello world!");
    }
}