using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JajkoBackend.Controllers;

[ApiController]
[Route("/api/items")]
public class ItemsController : ControllerBase
{
    private readonly AppDbContext db;

    public ItemsController(AppDbContext db)
    {
        this.db = db;
    }
    
    [HttpGet("{category}")]
    public async Task<IActionResult> Example(int category)
    {
        return Ok("Hello world!");
    }
}