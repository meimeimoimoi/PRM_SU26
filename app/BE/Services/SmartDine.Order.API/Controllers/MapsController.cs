using System.Text.Json;
using System.Text.Json.Serialization;
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
                Path.Combine(env.ContentRootPath, "data", "maps"));
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

    /// <summary>POST /api/v1/maps — Save a new map (from draw-map editor)</summary>
    [HttpPost]
    public IActionResult Create([FromBody] CreateMapRequest req)
    {
        var id = Guid.NewGuid().ToString("N").Substring(0, 12);
        var mapDir = Path.Combine(_dataRoot, id);
        Directory.CreateDirectory(mapDir);

        var meta = new
        {
            floorSize = req.FloorSize,
            resolution = req.Resolution,
            robotStart = new
            {
                x = req.RobotStartWorldX,
                y = req.RobotStartWorldY,
                theta = req.RobotStartWorldTheta
            },
            totalObjects = req.Objects?.Count ?? 0,
            createdAt = DateTime.UtcNow.ToString("o")
        };

        System.IO.File.WriteAllText(
            Path.Combine(mapDir, "meta.json"),
            JsonSerializer.Serialize(meta, JsonOpts));

        if (!string.IsNullOrEmpty(req.Graph))
            System.IO.File.WriteAllText(Path.Combine(mapDir, "graph.json"), req.Graph);

        if (!string.IsNullOrEmpty(req.Waypoints))
            System.IO.File.WriteAllText(Path.Combine(mapDir, "waypoints.txt"), req.Waypoints);

        return Ok(new { id, message = "Map saved successfully" });
    }

    /// <summary>GET /api/v1/maps/{id}/files — Download all map files for sidecar.
    /// Returns JSON with: meta, graph, waypoints, mapYaml, mapPgmBase64.</summary>
    [HttpGet("{id}/files")]
    public IActionResult GetFiles(string id)
    {
        var mapDir = Path.Combine(_dataRoot, id);
        if (!Directory.Exists(mapDir))
            return NotFound(new { error = "Map not found" });

        var result = new MapFilesResponse();

        // meta.json
        var metaPath = Path.Combine(mapDir, "meta.json");
        if (System.IO.File.Exists(metaPath))
        {
            var json = System.IO.File.ReadAllText(metaPath);
            result.Meta = JsonSerializer.Deserialize<JsonElement>(json, JsonOpts);
        }

        // graph.json
        var graphPath = Path.Combine(mapDir, "graph.json");
        if (System.IO.File.Exists(graphPath))
        {
            var json = System.IO.File.ReadAllText(graphPath);
            result.Graph = JsonSerializer.Deserialize<JsonElement>(json, JsonOpts);
        }

        // waypoints.txt
        var wpPath = Path.Combine(mapDir, "waypoints.txt");
        if (System.IO.File.Exists(wpPath))
            result.Waypoints = System.IO.File.ReadAllText(wpPath);

        // map.yaml
        var yamlPath = Path.Combine(mapDir, "map.yaml");
        if (System.IO.File.Exists(yamlPath))
            result.MapYaml = System.IO.File.ReadAllText(yamlPath);

        // map.pgm → base64
        var pgmPath = Path.Combine(mapDir, "map.pgm");
        if (System.IO.File.Exists(pgmPath))
            result.MapPgmBase64 = Convert.ToBase64String(System.IO.File.ReadAllBytes(pgmPath));

        return Ok(result);
    }
}

public class MapFilesResponse
{
    [JsonPropertyName("meta")]
    public JsonElement? Meta { get; set; }

    [JsonPropertyName("graph")]
    public JsonElement? Graph { get; set; }

    [JsonPropertyName("waypoints")]
    public string? Waypoints { get; set; }

    [JsonPropertyName("mapYaml")]
    public string? MapYaml { get; set; }

    [JsonPropertyName("mapPgmBase64")]
    public string? MapPgmBase64 { get; set; }
}

public class CreateMapRequest
{
    [JsonPropertyName("floorSize")]
    public double FloorSize { get; set; }

    [JsonPropertyName("resolution")]
    public double Resolution { get; set; }

    [JsonPropertyName("robot_start_world_x")]
    public double RobotStartWorldX { get; set; }

    [JsonPropertyName("robot_start_world_y")]
    public double RobotStartWorldY { get; set; }

    [JsonPropertyName("robot_start_world_theta")]
    public double RobotStartWorldTheta { get; set; }

    [JsonPropertyName("objects")]
    public List<object>? Objects { get; set; }

    [JsonPropertyName("graph")]
    public string? Graph { get; set; }

    [JsonPropertyName("waypoints")]
    public string? Waypoints { get; set; }
}
