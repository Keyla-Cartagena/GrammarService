using GrammarService.Data;
using GrammarService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GrammarService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GrammarController : ControllerBase
    {
        private readonly GrammarContext _context;
        private readonly Services.GrammarService _service;

        public GrammarController(GrammarContext context, Services.GrammarService service)
        {
            _context = context;
            _service = service;
        }

        /// <summary>
        /// Crear una nueva gramática
        /// POST /api/grammar
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddGrammar([FromBody] Grammar grammar)
        {
            if (grammar == null)
                return BadRequest("La gramática no puede ser nula");

            if (string.IsNullOrEmpty(grammar.StartSymbol))
                return BadRequest("El símbolo inicial es requerido");

            // Asignar el GrammarId a cada producción
            foreach (var prod in grammar.Productions)
            {
                prod.GrammarId = grammar.Id;
            }

            _context.Grammars.Add(grammar);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGrammar), new { id = grammar.Id }, grammar);
        }

        /// <summary>
        /// Obtener todas las gramáticas
        /// GET /api/grammar
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var grammars = await _context.Grammars
                .Include(g => g.Productions)
                .ToListAsync();

            return Ok(grammars);
        }

        /// <summary>
        /// Obtener una gramática por ID
        /// GET /api/grammar/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetGrammar(string id)
        {
            var grammar = await _context.Grammars
                .Include(g => g.Productions)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (grammar == null)
                return NotFound($"Gramática con ID '{id}' no encontrada");

            return Ok(grammar);
        }

        /// <summary>
        /// Calcular conjunto FIRST
        /// GET /api/grammar/{id}/first
        /// </summary>
        [HttpGet("{id}/first")]
        public async Task<IActionResult> GetFirst(string id)
        {
            var grammar = await _context.Grammars
                .Include(g => g.Productions)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (grammar == null)
                return NotFound($"Gramática con ID '{id}' no encontrada");

            var result = new Dictionary<string, HashSet<string>>();

            // Obtener todos los no terminales únicos
            var nonTerminals = grammar.Productions
                .Select(p => p.NonTerminal)
                .Distinct();

            foreach (var nt in nonTerminals)
            {
                result[nt] = _service.ComputeFirst(grammar, nt);
            }

            return Ok(result);
        }

        /// <summary>
        /// Calcular conjunto FOLLOW
        /// GET /api/grammar/{id}/follow
        /// </summary>
        [HttpGet("{id}/follow")]
        public async Task<IActionResult> GetFollow(string id)
        {
            var grammar = await _context.Grammars
                .Include(g => g.Productions)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (grammar == null)
                return NotFound($"Gramática con ID '{id}' no encontrada");

            var result = new Dictionary<string, HashSet<string>>();

            // Obtener todos los no terminales únicos
            var nonTerminals = grammar.Productions
                .Select(p => p.NonTerminal)
                .Distinct();

            foreach (var nt in nonTerminals)
            {
                result[nt] = _service.ComputeFollow(grammar, nt);
            }

            return Ok(result);
        }

        /// <summary>
        /// Calcular conjunto PREDICT
        /// GET /api/grammar/{id}/predict
        /// </summary>
        [HttpGet("{id}/predict")]
        public async Task<IActionResult> GetPredict(string id)
        {
            var grammar = await _context.Grammars
                .Include(g => g.Productions)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (grammar == null)
                return NotFound($"Gramática con ID '{id}' no encontrada");

            var result = new Dictionary<string, Dictionary<string, HashSet<string>>>();

            foreach (var prod in grammar.Productions)
            {
                if (!result.ContainsKey(prod.NonTerminal))
                {
                    result[prod.NonTerminal] = new Dictionary<string, HashSet<string>>();
                }

                result[prod.NonTerminal][prod.RightSide] = _service.ComputePredict(grammar, prod);
            }

            return Ok(result);
        }

        /// <summary>
        /// Eliminar una gramática
        /// DELETE /api/grammar/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGrammar(string id)
        {
            var grammar = await _context.Grammars
                .Include(g => g.Productions)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (grammar == null)
                return NotFound($"Gramática con ID '{id}' no encontrada");

            _context.Grammars.Remove(grammar);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}