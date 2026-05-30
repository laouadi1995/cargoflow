using Microsoft.AspNetCore.Mvc;
using CargoFlow.Api.Data;
using CargoFlow.Api.Models;

[ApiController]
[Route("api/[controller]")]
public class OperationController : ControllerBase
{
    private readonly AppDbContext _context;

    public OperationController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("take")]
    public IActionResult Take(Operation op)
    {
        var truck = _context.Trucks.Find(op.TruckId);

        if (truck == null) return NotFound();

        truck.Status = "Taken";

        _context.Operations.Add(op);
        _context.SaveChanges();

        return Ok(op);
    }

    [HttpPost("leave")]
    public IActionResult Leave(Operation op)
    {
        var truck = _context.Trucks.Find(op.TruckId);

        if (truck == null) return NotFound();

        truck.Status = "Available";

        _context.Operations.Add(op);
        _context.SaveChanges();

        return Ok(op);
    }
}