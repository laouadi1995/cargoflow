using Microsoft.AspNetCore.Mvc;
using CargoFlow.Api.Data;
using CargoFlow.Api.Models;

[ApiController]
[Route("api/[controller]")]
public class TrucksController : ControllerBase
{
    private readonly AppDbContext _context;

    public TrucksController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(_context.Trucks.Where(t => t.Status == "Available"));
    }
}