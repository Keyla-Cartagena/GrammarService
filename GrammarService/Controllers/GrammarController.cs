using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GrammarService.Models;
using GrammarService.Services;
using GrammarService.Data;

namespace GrammarService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GrammarController : ControllerBase
    {
        private readonly GrammarService.Services.GrammarService _grammarService;
        private readonly ILogger<GrammarController> _logger;
        private readonly GrammarContext _context;

        public GrammarController(
            ILogger<GrammarController> logger,
            GrammarContext context)
        {
            _grammarService = new GrammarService.Services.GrammarService();
            _logger = logger;
            _context = context;
        }

        [HttpGet("{id}/follow")]
        public async Task<IActionResult> GetFollow(string id)
        {
            try
            {
                _logger.LogInformation($"Calculando FOLLOW para gramática {id}");

                var grammar = await GetGrammarByIdAsync(id);

                if (grammar == null)
                {
                    _logger.LogWarning($"Gramática {id} no encontrada");
                    return NotFound(new { error = "Gramática no encontrada" });
                }

                _grammarService.ClearCache();

                var results = new Dictionary<string, HashSet<string>>();
                var nonTerminals = grammar.Productions
                    .Select(p => p.NonTerminal)
                    .Distinct()
                    .ToList();

                foreach (var nt in nonTerminals)
                {
                    var follow = _grammarService.ComputeFollow(grammar, nt);
                    results[nt] = follow;
                }

                _logger.LogInformation($"FOLLOW calculado exitosamente para {nonTerminals.Count} no terminales");

                return Ok(new
                {
                    grammarId = id,
                    follows = results.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.OrderBy(x => x).ToList()
                    )
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, $"Error de operación inválida calculando FOLLOW para gramática {id}");
                return BadRequest(new
                {
                    error = "Error al calcular FOLLOW",
                    message = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, $"Argumento inválido al calcular FOLLOW para gramática {id}");
                return BadRequest(new
                {
                    error = "Parámetros inválidos",
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error inesperado calculando FOLLOW para gramática {id}");
                return StatusCode(500, new
                {
                    error = "Error interno del servidor",
                    message = "Ocurrió un error procesando la solicitud. Por favor, contacta al administrador.",
                    requestId = HttpContext.TraceIdentifier
                });
            }
        }

        [HttpGet("{id}/first")]
        public async Task<IActionResult> GetFirst(string id)
        {
            try
            {
                _logger.LogInformation($"Calculando FIRST para gramática {id}");

                var grammar = await GetGrammarByIdAsync(id);

                if (grammar == null)
                {
                    return NotFound(new { error = "Gramática no encontrada" });
                }

                _grammarService.ClearCache();

                var results = new Dictionary<string, HashSet<string>>();
                var nonTerminals = grammar.Productions
                    .Select(p => p.NonTerminal)
                    .Distinct()
                    .ToList();

                foreach (var nt in nonTerminals)
                {
                    var first = _grammarService.ComputeFirst(grammar, nt);
                    results[nt] = first;
                }

                return Ok(new
                {
                    grammarId = id,
                    firsts = results.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.OrderBy(x => x).ToList()
                    )
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, $"Error calculando FIRST para gramática {id}");
                return BadRequest(new
                {
                    error = "Error al calcular FIRST",
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error inesperado calculando FIRST para gramática {id}");
                return StatusCode(500, new
                {
                    error = "Error interno del servidor",
                    message = "Ocurrió un error procesando la solicitud.",
                    requestId = HttpContext.TraceIdentifier
                });
            }
        }

        [HttpGet("{id}/predict")]
        public async Task<IActionResult> GetPredict(string id)
        {
            try
            {
                _logger.LogInformation($"Calculando PREDICT para gramática {id}");

                var grammar = await GetGrammarByIdAsync(id);

                if (grammar == null)
                {
                    return NotFound(new { error = "Gramática no encontrada" });
                }

                _grammarService.ClearCache();

                var results = new List<object>();

                foreach (var production in grammar.Productions)
                {
                    var predict = _grammarService.ComputePredict(grammar, production);
                    results.Add(new
                    {
                        nonTerminal = production.NonTerminal,
                        production = production.RightSide,
                        predict = predict.OrderBy(x => x).ToList()
                    });
                }

                return Ok(new
                {
                    grammarId = id,
                    predictions = results
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, $"Error calculando PREDICT para gramática {id}");
                return BadRequest(new
                {
                    error = "Error al calcular PREDICT",
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error inesperado calculando PREDICT para gramática {id}");
                return StatusCode(500, new
                {
                    error = "Error interno del servidor",
                    message = "Ocurrió un error procesando la solicitud.",
                    requestId = HttpContext.TraceIdentifier
                });
            }
        }

        /// <summary>
        /// Obtiene una gramática desde la base de datos por su ID (string)
        /// </summary>
        private async Task<Grammar?> GetGrammarByIdAsync(string id)
        {
            try
            {
                // Validar que el ID no esté vacío
                if (string.IsNullOrWhiteSpace(id))
                {
                    _logger.LogWarning("ID de gramática vacío o nulo");
                    return null;
                }

                // Consultar la base de datos
                // El ID es string, no Guid
                var grammar = await _context.Grammars
                    .Include(g => g.Productions)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(g => g.Id == id);

                if (grammar == null)
                {
                    _logger.LogWarning($"Gramática con ID {id} no encontrada");
                    return null;
                }

                // Validar que tenga producciones
                if (!grammar.Productions.Any())
                {
                    _logger.LogWarning($"Gramática {id} no tiene producciones");
                    return null;
                }

                // Validar que tenga símbolo inicial
                if (string.IsNullOrWhiteSpace(grammar.StartSymbol))
                {
                    _logger.LogWarning($"Gramática {id} no tiene símbolo inicial definido");
                    return null;
                }

                return grammar;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error obteniendo gramática {id} de la base de datos");
                throw;
            }
        }
    }
}