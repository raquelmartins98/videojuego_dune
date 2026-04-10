using System.Text.Json;
using System.Text.Json.Serialization;
using Dune.Domain.Interfaces;
using Dune.Domain.Models;

namespace Dune.Persistence.Service;

public class PersistenceService : IPersistenceService
{
    private readonly JsonSerializerOptions _options;

    public PersistenceService()
    {
        _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public async Task<(bool Success, string Message)> GuardarPartidaAsync(Partida partida, string ruta)
    {
        try
        {
            partida.UltimoGuardado = DateTime.Now;
            var json = JsonSerializer.Serialize(partida, _options);
            
            var directory = Path.GetDirectoryName(ruta);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            await File.WriteAllTextAsync(ruta, json);
            return (true, "Partida guardada correctamente");
        }
        catch (Exception ex)
        {
            return (false, $"Error al guardar: {ex.Message}");
        }
    }

    public async Task<(Partida? Partida, string Message)> CargarPartidaAsync(string ruta)
    {
        try
        {
            if (!File.Exists(ruta))
                return (null, "El archivo no existe");

            var json = await File.ReadAllTextAsync(ruta);
            
            if (string.IsNullOrWhiteSpace(json))
                return (null, "El archivo esta vacio");

            var partida = JsonSerializer.Deserialize<Partida>(json, _options);
            
            if (partida == null)
                return (null, "Error al deserializar el archivo");

            return (partida, "Partida cargada correctamente");
        }
        catch (JsonException ex)
        {
            return (null, $"Error de formato JSON: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (null, $"Error al cargar: {ex.Message}");
        }
    }

    public Task<List<string>> ListarPartidasAsync(string directorio)
    {
        try
        {
            if (!Directory.Exists(directorio))
                Directory.CreateDirectory(directorio);

            var archivos = Directory.GetFiles(directorio, "*.json")
                .Select(Path.GetFileName)
                .Where(f => f != null)
                .Cast<string>()
                .ToList();

            return Task.FromResult(archivos);
        }
        catch (Exception)
        {
            return Task.FromResult(new List<string>());
        }
    }
}
