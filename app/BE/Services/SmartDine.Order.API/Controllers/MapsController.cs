using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace SmartDine.Order.API.Controllers;

[ApiController]
[Route("api/v1/maps")]
public class MapsController : ControllerBase
{
    private readonly string _dataRoot;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public MapsController(IConfiguration config, IWebHostEnvironment env)
    {
        var configured = config["Maps:DataRoot"];
        if (!string.IsNullOrEmpty(configured))
        {
            _dataRoot = Path.GetFullPath(configured);
        }
        else
        {
            _dataRoot = Path.GetFullPath(
                Path.Combine(env.ContentRootPath, "..", "..", "..", "..", "map-server", "data", "maps"));
        }
    }

    /// <summary>GET /api/v1/maps — List all maps (meta only)</summary>
    [HttpGet]
    public IActionResult GetAll()
    {
        if (!Directory.Exists(_dataRoot))
            return Ok(Array.Empty<object>());

        var dirs = Directory.GetDirectories(_dataRoot);
        var list = new List<object>();

        foreach (var dir in dirs)
        {
            var metaPath = Path.Combine(dir, "meta.json");
            if (!System.IO.File.Exists(metaPath)) continue;

            try
            {
                var json = System.IO.File.ReadAllText(metaPath);
                var meta = JsonSerializer.Deserialize<JsonElement>(json, JsonOpts);
                var id = Path.GetFileName(dir);
                var dict = new Dictionary<string, object?> { ["id"] = id };
                foreach (var prop in meta.EnumerateObject())
                    dict[prop.Name] = prop.Value.ValueKind switch
                    {
                        JsonValueKind.Number => prop.Value.GetDouble(),
                        JsonValueKind.String => prop.Value.GetString(),
                        JsonValueKind.Object => JsonSerializer.Deserialize<JsonElement>(prop.Value.GetRawText()),
                        _ => prop.Value.ToString()
                    };
                list.Add(dict);
            }
            catch
            {
                // skip corrupt meta
            }
        }

        return Ok(list);
    }

    /// <summary>GET /api/v1/maps/{id} — Get single map meta</summary>
    [HttpGet("{id}")]
    public IActionResult GetById(string id)
    {
        var metaPath = Path.Combine(_dataRoot, id, "meta.json");
        if (!System.IO.File.Exists(metaPath))
            return NotFound(new { error = "Map not found" });

        var json = System.IO.File.ReadAllText(metaPath);
        var meta = JsonSerializer.Deserialize<JsonElement>(json, JsonOpts);
        var dict = new Dictionary<string, object?> { ["id"] = id };
        foreach (var prop in meta.EnumerateObject())
            dict[prop.Name] = prop.Value.ValueKind switch
            {
                JsonValueKind.Number => prop.Value.GetDouble(),
                JsonValueKind.String => prop.Value.GetString(),
                JsonValueKind.Object => JsonSerializer.Deserialize<JsonElement>(prop.Value.GetRawText()),
                _ => prop.Value.ToString()
            };

        return Ok(dict);
    }
}
