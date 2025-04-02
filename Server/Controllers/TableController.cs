using Microsoft.AspNetCore.Mvc;
using Server.Services; 

namespace Server.Controllers;

[ApiController]
[Route("api/table")]
public class TableController : ControllerBase
{
    private readonly TableManager _tableManager;

    public TableController(TableManager tableManager)
    {
        _tableManager = tableManager;
    }

    // POST: /api/table/create
    [HttpPost("create")]
    public IActionResult CreateTable()
    {
        var tableId = _tableManager.CreateTable();
        return Ok(new { tableId });
    }

    // GET: /api/table/join/{tableId}
    [HttpGet("join/{tableId}")]
    public IActionResult JoinTable(string tableId)
    {
        if (_tableManager.TableExists(tableId))
            return Ok(new { success = true });

        return NotFound(new { error = "Table not found" });
    }
}
