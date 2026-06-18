// Archivo: Controllers/EstudianteController.cs
using Microsoft.AspNetCore.Mvc;
using ActividadEnClase.Models;

namespace ActividadEnClase.Controllers;

public class EstudianteController : Controller
{
    // Almacenamiento simulado en memoria (simula el Data Tier / Tier 3)
    private static readonly List<Estudiante> _baseDatosMemoria = new()
    {
        new Estudiante { Carne = 2026012, Nombre = "Fernando Velásquez", Promedio = 91.5 },
        new Estudiante { Carne = 2026045, Nombre = "María Mercedes", Promedio = 84.0 }
    };

    // GET: /Estudiante/Listar
    public IActionResult Listar()
    {
        return View(_baseDatosMemoria);
    }

    // POST: /Estudiante/Registrar
    [HttpPost]
    public IActionResult Registrar([FromBody] Estudiante nuevoEstudiante)
    {
        if (nuevoEstudiante.Carne <= 0 || string.IsNullOrEmpty(nuevoEstudiante.Nombre))
        {
            return BadRequest(new { mensaje = "Datos del estudiante inválidos." });
        }

        _baseDatosMemoria.Add(nuevoEstudiante);
        return Created($"/Estudiante/Historial/{nuevoEstudiante.Carne}", nuevoEstudiante);
    }
}
