using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SexShopAPI.Data;
using SexShopAPI.Models;

namespace SexShopAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;

    public ProductsController(ApplicationDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    // GET: api/Products
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
    {
        try 
        {
            return await _context.Products.ToListAsync();
        }
        catch (Exception ex)
        {
            var conn = _config.GetConnectionString("DefaultConnection") ?? "MISSING";
            var maskedConn = conn.Length > 15 ? conn.Substring(0, 15) + "..." : conn;
            return StatusCode(500, new { error = ex.Message, detail = ex.InnerException?.Message, connectionUsed = maskedConn });
        }
    }

    // GET: api/Products/ping
    [HttpGet("ping")]
    public IActionResult Ping([FromServices] IConfiguration config)
    {
        var conn = config.GetConnectionString("DefaultConnection") ?? "MISSING";
        
        // Diagnostics for connection string type
        string dbType = "Unknown";
        if (conn.StartsWith("Data Source=") || conn.Contains("Server=") && conn.Contains("Database="))
        {
            dbType = "SQL Server";
        }
        else if (conn.StartsWith("Host=") || conn.StartsWith("Server=") && conn.Contains("Port=") && conn.Contains("Database="))
        {
            dbType = "PostgreSQL";
        }
        else if (conn.StartsWith("Filename="))
        {
            dbType = "SQLite";
        }
        else if (conn.StartsWith("postgresql://") || conn.StartsWith("postgres://"))
        {
            dbType = "PostgreSQL (URI)";
        }

        return Ok(new { 
            status = "API is reachable", 
            connPrefix = conn.Length > 10 ? conn.Substring(0, 10) : conn,
            connLength = conn.Length,
            dbType = dbType, // Added diagnostic
            timestamp = DateTime.UtcNow 
        });
    }

    // GET: api/Products/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
        {
            return NotFound();
        }

        return product;
    }

    // POST: api/Products
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<Product>> PostProduct(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetProduct", new { id = product.Id }, product);
    }

    // PUT: api/Products/5
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> PutProduct(int id, Product product)
    {
        if (id != product.Id)
        {
            return BadRequest();
        }

        _context.Entry(product).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ProductExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // DELETE: api/Products/5
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool ProductExists(int id)
    {
        return _context.Products.Any(e => e.Id == id);
    }
}
