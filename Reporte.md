# Reporte de Laboratorio: Arquitectura Multi-Nivel (N-Tier) y Patrón MVC en .NET

**Curso:** Introducción a la Programación y Computación 2  
**Modalidad:** Individual  
**Duración estimada:** 120 minutos

---

## Parte 1: Fundamentación Teórica y Análisis Crítico

### 1. El Tránsito hacia los Sistemas Distribuidos y Multi-Capa

#### La Limitación del Monolito Local

Cuando la interfaz de usuario, la lógica de negocio y el almacenamiento de datos residen **de forma exclusiva en una única máquina física aislada**, surgen tres problemas estructurales críticos:

- **Sincronización imposible:** Si dos usuarios acceden simultáneamente al sistema desde terminales distintas, no existe mecanismo para que los cambios de uno sean visibles para el otro en tiempo real. Cualquier modificación en los datos queda encapsulada localmente, lo que produce inconsistencias e información desactualizada.
- **Escalabilidad nula:** Al estar todo en un solo equipo, el rendimiento está limitado por el hardware de esa máquina. Si la carga de trabajo crece (más usuarios, más datos), la única solución es reemplazar el equipo completo (*scale-up*), lo cual es costoso y tiene un techo físico.
- **Punto único de fallo:** Si esa máquina falla, todo el sistema deja de funcionar. No hay redundancia ni tolerancia a fallos posible.

#### Distinción Crítica: Layers vs. Tiers

| Concepto | Definición | Naturaleza |
|---|---|---|
| **Layer (Capa Lógica)** | División **conceptual** del código dentro de una misma aplicación, que separa responsabilidades (presentación, lógica, datos). | **Lógica / Software** — puede residir en el mismo proceso o ejecutable. |
| **Tier (Nivel Físico)** | División **física** de la infraestructura donde cada componente se despliega en máquinas o procesos separados que se comunican a través de una red. | **Física / Hardware** — implica máquinas, servidores o contenedores distintos. |

> **Regla de oro:** Toda arquitectura de 3 Tiers tiene al menos 3 Layers, pero tener 3 Layers no implica que el sistema esté distribuido en 3 Tiers físicos.

#### Responsabilidades en la Arquitectura de 3 Niveles

**Nivel 1 — Capa de Presentación (Presentation Tier)**

Su misión exclusiva es **interactuar con el usuario final**: renderizar la interfaz gráfica, capturar entradas y mostrar resultados. No ejecuta lógica de negocio ni accede directamente a la base de datos. Las tecnologías comunes incluyen navegadores web (HTML/CSS/JavaScript), aplicaciones móviles (Android, iOS) y clientes de escritorio. En el contexto de ASP.NET Core MVC, las **Vistas (`.cshtml`)** pertenecen a este nivel.

**Nivel 2 — Capa de Aplicación o Negocio (Application Tier)**

Su misión exclusiva es **procesar las reglas de negocio**: validar datos, ejecutar cálculos, orquestar flujos de trabajo y servir de intermediario entre la presentación y los datos. Es el "cerebro" del sistema. Las tecnologías comunes son servidores de aplicaciones como **ASP.NET Core**, Node.js, Spring Boot o Django. Los **Controladores y Servicios** de MVC viven aquí.

**Nivel 3 — Capa de Datos (Data Tier)**

Su misión exclusiva es **persistir y servir datos** de forma confiable y eficiente: almacenar, recuperar, actualizar y eliminar registros. No contiene lógica de negocio. Las tecnologías comunes son sistemas gestores de bases de datos como **SQL Server**, PostgreSQL, MySQL u Oracle. Solo el Application Tier tiene permiso de comunicarse con este nivel.

#### Seguridad Perimetral: Por qué no exponer el puerto de la base de datos

Exponer públicamente el puerto de una base de datos (p. ej., el puerto 1433 de SQL Server o 5432 de PostgreSQL) a Internet es un **error crítico de seguridad** por las siguientes razones de ingeniería:

1. **Superficie de ataque directa:** Cualquier actor malicioso en Internet puede intentar ataques de fuerza bruta sobre las credenciales, explotar vulnerabilidades conocidas del motor de base de datos o ejecutar inyecciones SQL directas sin pasar por ninguna capa de validación.
2. **Ausencia de filtrado:** Al saltarse el Application Tier, se elimina la capa que valida, sanea y audita las solicitudes antes de que toquen los datos.
3. **Violación del principio de mínimo privilegio:** Los datos son el activo más valioso del sistema; deben estar protegidos por múltiples capas, no expuestos directamente.

**Buena práctica recomendada:** La base de datos debe residir en una **red privada (LAN/VPC)** sin acceso desde Internet. Solo el servidor de aplicaciones (Application Tier), que se encuentra en la misma red privada o conectado mediante reglas de *firewall* restrictivas, debe tener permiso de abrir conexiones al puerto de la base de datos. El acceso externo a la base de datos se bloquea a nivel de firewall perimetral y grupos de seguridad de red.

---

### 2. Desacoplamiento Lógico con el Patrón MVC

#### La Crisis del Código Espagueti

Mezclar sentencias SQL, lógica de negocio y etiquetas visuales dentro de un mismo archivo genera los siguientes impactos negativos:

- **Mantenimiento imposible:** Un cambio en la lógica de cálculo obliga a revisar archivos que también contienen HTML. Un ajuste visual puede romper accidentalmente una consulta SQL. La responsabilidad difusa hace que ningún desarrollador sepa con certeza qué parte del código le "pertenece".
- **Pruebas unitarias bloqueadas:** No se puede escribir un test para una función de cálculo si esa función está mezclada con `<div>` y cadenas SQL. Para probar algo, habría que levantar una base de datos y un motor de renderizado al mismo tiempo, convirtiendo cada prueba en una prueba de integración costosa.
- **Alta rotación de errores:** Un bug en la capa visual puede enmascarar un error lógico y viceversa, aumentando el tiempo de diagnóstico.
- **Imposibilidad de trabajo en equipo paralelo:** El diseñador de UI y el desarrollador de lógica no pueden trabajar en el mismo archivo simultáneamente sin conflictos constantes de control de versiones.

#### Separación de Preocupaciones (SoC): Los tres componentes de Reenskaug

**Modelo (Model)**

Representa el **estado y las reglas del dominio de negocio**: las entidades de datos (clases POCO/POJO), las reglas de validación y la lógica de acceso a datos (repositorios). El Modelo **no debe conocer cómo se muestran los datos** porque eso crearía acoplamiento con la interfaz. Si el día de mañana se cambia de una vista web a una app móvil, el Modelo no debería modificarse en absoluto. Su única responsabilidad es ser la fuente de verdad del sistema.

**Vista (View)**

Es una entidad **pasiva e inteligente a la vez**: pasiva porque no toma decisiones de negocio ni ejecuta lógica matemática por sí sola; inteligente porque sabe cómo renderizar los datos que recibe para el consumo humano. La Vista tiene **estrictamente prohibido** contener: sentencias SQL, lógica de negocio (cálculos, validaciones de reglas), llamadas directas a servicios externos, o cualquier código que altere el estado del sistema. En ASP.NET Core MVC, las Vistas solo pueden usar los datos que el Controlador les entrega a través del `ViewBag`, `ViewData` o el modelo fuertemente tipado.

**Controlador (Controller)**

Es el **intermediario táctico y director de orquesta**: recibe la petición HTTP del cliente, determina qué operación debe ejecutarse, invoca los métodos correspondientes del Modelo, y luego selecciona y devuelve la Vista adecuada con los datos procesados. No contiene lógica de negocio compleja (eso lo delega a servicios o al Modelo) ni lógica de presentación (eso lo delega a la Vista). Su rol es exclusivamente el de **enrutador y coordinador**.

#### Métricas de Ingeniería de Software: Alta Cohesión y Bajo Acoplamiento

El patrón MVC contribuye directamente a dos métricas fundamentales:

- **Alta Cohesión:** Cada componente tiene una responsabilidad única y bien definida. El Modelo agrupa todo lo relacionado con los datos del dominio; la Vista agrupa todo lo relacionado con la presentación; el Controlador agrupa toda la lógica de despacho. Esto significa que cada clase o archivo tiene una razón única para cambiar (*Principio de Responsabilidad Única — SRP*).

- **Bajo Acoplamiento:** Los tres componentes se comunican a través de interfaces o contratos bien definidos, no mediante dependencias directas entre implementaciones. La Vista no necesita saber de dónde vienen los datos; el Modelo no sabe cómo se renderizan; el Controlador actúa como la única conexión entre ellos. Esto permite reemplazar o modificar cualquier componente sin afectar a los demás, facilitando la escalabilidad y el testing independiente.

---

## Parte 2: Modelado del Ciclo de Vida y Enrutamiento Semántico

### 1. Mapeo Analítico de URLs

La plantilla de enrutamiento estándar de ASP.NET Core es:

```
{controller=Home}/{action=Index}/{id?}
```

| URL Entrante del Cliente | Clase Controladora Buscada | Método (Acción) Ejecutado | Parámetro `id` Inyectado |
|---|---|---|---|
| `https://ingenieria.usac.edu.gt/ControlAcademico/Login` | `ControlAcademicoController` | `Login` | *(Ninguno / Opcional)* |
| `https://ingenieria.usac.edu.gt/Estudiante/Historial/20260123` | `EstudianteController` | `Historial` | `20260123` |
| `https://ingenieria.usac.edu.gt/Asignacion/Detalle/10` | `AsignacionController` | `Detalle` | `10` |
| `https://ingenieria.usac.edu.gt/Home` | `HomeController` | `Index` *(valor por defecto)* | *(Ninguno / Opcional)* |

> **Nota de análisis:** El framework de ASP.NET Core aplica la convención de nombre `{NombreSegmento}Controller` al buscar la clase correspondiente. La URL `/ControlAcademico/Login` hace que el framework instancie `ControlAcademicoController` e invoque su método `Login()`. Para `/Home`, al no especificar acción, se usa el valor por defecto `Index` definido en la plantilla.

---

### 2. Diagramación del Flujo Interactivo

A continuación se describe el viaje completo de una petición HTTP desde el clic del usuario hasta la respuesta renderizada:

**Paso 1 — El usuario genera la petición (Cliente / Navegador)**  
El usuario hace clic en un botón o enlace en su navegador. El navegador construye una petición HTTP (GET o POST) con la URL correspondiente y la envía al servidor a través de Internet.

**Paso 2 — El motor de enrutamiento intercepta y analiza la URL (ASP.NET Core Router)**  
El middleware de enrutamiento de ASP.NET Core recibe la petición entrante y compara la URL contra las plantillas registradas (p. ej., `{controller}/{action}/{id?}`). Extrae los segmentos de la ruta y determina qué Controlador y qué método de Acción deben ejecutarse.

**Paso 3 — El Controlador recibe la petición y orquesta la respuesta (Controller)**  
El framework instancia el Controlador correspondiente e invoca el método de Acción identificado. El Controlador solicita al Modelo los datos necesarios (p. ej., consulta la lista de estudiantes en memoria o en la base de datos), aplica validaciones perimetrales básicas y prepara el objeto de datos que pasará a la Vista.

**Paso 4 — El Modelo provee los datos del dominio (Model)**  
El Modelo ejecuta la operación solicitada: recupera registros, aplica reglas de negocio, valida entidades y retorna los datos procesados al Controlador. En ningún momento sabe qué Vista los consumirá.

**Paso 5 — La Vista renderiza el HTML y el servidor responde (View → Cliente)**  
El Controlador selecciona la Vista apropiada y le inyecta los datos del Modelo. El motor de plantillas Razor (`.cshtml`) compila la Vista combinando el HTML estático con los datos dinámicos, generando el código HTML final. El servidor envía ese HTML como respuesta HTTP al navegador del usuario, quien lo renderiza en pantalla.

---

## Parte 3: Implementación Práctica — Sistema de Control Académico

### Estructura del Proyecto

```
ControlAcademicoMvc/
├── Controllers/
│   └── EstudianteController.cs
├── Models/
│   └── Estudiante.cs
├── Views/
│   └── Estudiante/
│       └── Listar.cshtml
├── wwwroot/
└── Program.cs
```

### Paso 1: Creación del espacio de trabajo

```bash
dotnet new webapp -o ControlAcademicoMvc
cd ControlAcademicoMvc
```

### Paso 2: Modelo de Dominio (`Models/Estudiante.cs`)

```csharp
// Archivo: Models/Estudiante.cs
// Entidad pura (POCO) — no contiene lógica de presentación ni acceso a datos
namespace ControlAcademicoMvc.Models;

public class Estudiante
{
    public int Carne { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public double Promedio { get; set; }
}
```

**Análisis de diseño:** La clase `Estudiante` es una entidad POCO (Plain Old CLR Object) que representa exclusivamente el dominio de negocio. No tiene referencias a `System.Web`, ni a clases de UI, ni a cadenas de conexión. Esto garantiza Alta Cohesión: su única razón de cambiar es si la definición de un "estudiante" cambia en el dominio.

### Paso 3: Controlador (`Controllers/EstudianteController.cs`)

```csharp
// Archivo: Controllers/EstudianteController.cs
using Microsoft.AspNetCore.Mvc;
using ControlAcademicoMvc.Models;

namespace ControlAcademicoMvc.Controllers;

public class EstudianteController : Controller
{
    // Almacenamiento simulado en memoria (simula el Data Tier / Tier 3)
    private static readonly List<Estudiante> _baseDatosMemoria = new()
    {
        new Estudiante { Carne = 2026012, Nombre = "Fernando Velásquez", Promedio = 91.5 },
        new Estudiante { Carne = 2026045, Nombre = "María Mercedes", Promedio = 84.0 }
    };

    // GET: /Estudiante/Listar
    // Skinny Controller: solo extrae datos y delega a la Vista
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
```

**Análisis de Skinny Controller:** Ambos métodos cumplen la restricción de no superar 20 líneas de código. `Listar()` tiene exactamente 3 líneas efectivas: delega inmediatamente sin cálculos. `Registrar()` solo valida el contrato mínimo del dato entrante y delega el almacenamiento. Ninguno contiene SQL en texto plano ni lógica de renderizado.

### Paso 4: Vista (`Views/Estudiante/Listar.cshtml`)

```html
@model IEnumerable<ControlAcademicoMvc.Models.Estudiante>
@{
    ViewData["Title"] = "Listado de Estudiantes";
}

<h2>@ViewData["Title"]</h2>

<table>
    <thead>
        <tr>
            <th>Carné</th>
            <th>Nombre</th>
            <th>Promedio</th>
        </tr>
    </thead>
    <tbody>
        @foreach (var estudiante in Model)
        {
            <tr>
                <td>@estudiante.Carne</td>
                <td>@estudiante.Nombre</td>
                <td>@estudiante.Promedio</td>
            </tr>
        }
    </tbody>
</table>
```

**Análisis de Vista pasiva:** La Vista no realiza cálculos, no invoca servicios y no contiene sentencias SQL. Solo itera sobre la colección que el Controlador le entregó e imprime los valores. El uso de `@model` fuertemente tipado garantiza verificación en tiempo de compilación.

### Paso 5: Configuración del enrutamiento (`Program.cs`)

```csharp
var builder = WebApplication.CreateBuilder(args);

// Registro de servicios MVC con soporte para Vistas
builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

// Plantilla de enrutamiento jerárquico estándar
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

---

## Parte 4: Auditoría y Control de Calidad

### 1. Prueba de Cohesión (GET `/Estudiante/Listar`)

Al acceder a la ruta `GET /Estudiante/Listar`, el sistema retorna la lista de estudiantes renderizada. La verificación confirma que:

- El Controlador **no calculó variables internas** (no promedió, no filtró, no formateó fechas).
- El Controlador **no mezcló sentencias SQL** ni cadenas de conexión en su código.
- La respuesta proviene exclusivamente del objeto `_baseDatosMemoria` pasado directamente a `View()`.

✅ **Resultado:** El controlador aprueba la prueba de cohesión. Su único rol fue recuperar y despachar los datos.

### 2. Evaluación de Antipatrones (Fat Controller)

Revisión del archivo `EstudianteController.cs`:

| Método | Líneas de código efectivas | ¿Supera el límite de 20? |
|---|---|---|
| `Listar()` | 3 líneas | ❌ No (cumple) |
| `Registrar()` | 7 líneas | ❌ No (cumple) |

✅ **Resultado:** Ningún método supera las 20 líneas. No se detectó el antipatrón de *Fat Controller*. El proyecto aplica correctamente el principio de *Skinny Controllers*.

**Verificación de desacoplamiento:**
- El Modelo `Estudiante.cs` no tiene ninguna referencia a `Microsoft.AspNetCore` ni a clases de la Vista.
- La Vista `Listar.cshtml` no tiene bloques `@{ /* lógica compleja */ }` ni llamadas a servicios externos.
- El Controlador no tiene HTML embebido en strings ni retorna `Content("<html>...")` construido manualmente.

✅ **Conclusión:** El proyecto implementa correctamente la separación de preocupaciones del patrón MVC.

---

## Referencias Bibliográficas

> Facultad de Ingeniería, USAC. (2026). *Sesión 11: Modelado Base y Arquitecturas de Despliegue. Evolución de Sistemas Distribuidos, Fundamentos del Modelo Cliente-Servidor y Diseño Físico Multi-Capas (N-Tier)*. Laboratorio del curso Introducción a la Programación y Computación 2. Guatemala.

> Facultad de Ingeniería, USAC. (2026). *Sesión 12: Arquitectura y Componentes del Patrón MVC. Desacoplamiento Lógico de Software, Ciclo de Vida de las Peticiones y Enrutamiento en Aplicaciones Interactivas Modernas*. Laboratorio del curso Introducción a la Programación y Computación 2. Guatemala.
