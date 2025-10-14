using GrammarService.Models;
using System.Text.RegularExpressions;

namespace GrammarService.Services
{
    public class GrammarService
    {
        // Cache para evitar recalcular conjuntos
        private Dictionary<string, HashSet<string>> _firstCache = new();
        private Dictionary<string, HashSet<string>> _followCache = new();

        /// <summary>
        /// Limpia el cache antes de calcular nuevos conjuntos
        /// </summary>
        public void ClearCache()
        {
            _firstCache.Clear();
            _followCache.Clear();
        }

        /// <summary>
        /// Calcula el conjunto FIRST de un símbolo no terminal
        /// </summary>
        public HashSet<string> ComputeFirst(Grammar grammar, string symbol)
        {
            // Verificar cache
            if (_firstCache.ContainsKey(symbol))
                return new HashSet<string>(_firstCache[symbol]);

            var first = new HashSet<string>();
            var visited = new HashSet<string>();

            try
            {
                ComputeFirstRecursive(grammar, symbol, first, visited);
                _firstCache[symbol] = new HashSet<string>(first);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error calculando FIRST de '{symbol}': {ex.Message}", ex);
            }

            return first;
        }

        private void ComputeFirstRecursive(Grammar grammar, string symbol, HashSet<string> first, HashSet<string> visited)
        {
            // Evitar ciclos infinitos
            if (visited.Contains(symbol))
                return;

            visited.Add(symbol);

            // Obtener todas las producciones del símbolo
            var productions = grammar.Productions.Where(p => p.NonTerminal == symbol).ToList();

            if (!productions.Any())
            {
                throw new InvalidOperationException($"No se encontraron producciones para el símbolo '{symbol}'");
            }

            foreach (var prod in productions)
            {
                if (string.IsNullOrEmpty(prod.RightSide))
                {
                    first.Add("ε");
                    continue;
                }

                // Analizar cada alternativa separada por comas
                var alternatives = prod.RightSide.Split(',', StringSplitOptions.RemoveEmptyEntries);

                foreach (var alt in alternatives)
                {
                    var trimmedAlt = alt.Trim();

                    if (string.IsNullOrEmpty(trimmedAlt))
                    {
                        first.Add("ε");
                        continue;
                    }

                    // Procesar la secuencia de símbolos
                    bool allHaveEpsilon = true;

                    for (int i = 0; i < trimmedAlt.Length && allHaveEpsilon; i++)
                    {
                        var currentSymbol = trimmedAlt[i].ToString();

                        // Si es terminal
                        if (IsTerminal(trimmedAlt[i]))
                        {
                            first.Add(currentSymbol);
                            allHaveEpsilon = false;
                        }
                        // Si es no terminal
                        else if (IsNonTerminal(trimmedAlt[i]))
                        {
                            var firstOfSymbol = new HashSet<string>();
                            ComputeFirstRecursive(grammar, currentSymbol, firstOfSymbol, visited);

                            // Agregar todo excepto ε
                            foreach (var f in firstOfSymbol.Where(x => x != "ε"))
                            {
                                first.Add(f);
                            }

                            // Si no contiene ε, detenerse
                            if (!firstOfSymbol.Contains("ε"))
                            {
                                allHaveEpsilon = false;
                            }
                        }
                    }

                    // Si todos los símbolos pueden derivar ε
                    if (allHaveEpsilon)
                    {
                        first.Add("ε");
                    }
                }
            }
        }

        /// <summary>
        /// Calcula el conjunto FOLLOW de un símbolo no terminal
        /// </summary>
        public HashSet<string> ComputeFollow(Grammar grammar, string symbol)
        {
            // Limpiar cache para nueva gramática
            if (_followCache.Count == 0)
            {
                try
                {
                    ComputeAllFollows(grammar);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error calculando FOLLOW: {ex.Message}", ex);
                }
            }

            return _followCache.ContainsKey(symbol)
                ? new HashSet<string>(_followCache[symbol])
                : new HashSet<string>();
        }

        /// <summary>
        /// Calcula todos los conjuntos FOLLOW de una vez para evitar recursión infinita
        /// </summary>
        private void ComputeAllFollows(Grammar grammar)
        {
            // Inicializar conjuntos FOLLOW vacíos
            var follows = new Dictionary<string, HashSet<string>>();
            var nonTerminals = grammar.Productions.Select(p => p.NonTerminal).Distinct().ToList();

            foreach (var nt in nonTerminals)
            {
                follows[nt] = new HashSet<string>();
            }

            // Regla 1: $ en FOLLOW del símbolo inicial
            if (!string.IsNullOrEmpty(grammar.StartSymbol))
            {
                follows[grammar.StartSymbol].Add("$");
            }

            // Iterar hasta que no haya cambios (punto fijo)
            bool changed = true;
            int iterations = 0;
            const int maxIterations = 100; // Prevenir ciclos infinitos

            while (changed && iterations < maxIterations)
            {
                changed = false;
                iterations++;

                foreach (var prod in grammar.Productions)
                {
                    var alternatives = prod.RightSide?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

                    foreach (var alt in alternatives)
                    {
                        var trimmedAlt = alt.Trim();

                        if (string.IsNullOrEmpty(trimmedAlt))
                            continue;

                        // Buscar cada símbolo no terminal en la producción
                        for (int i = 0; i < trimmedAlt.Length; i++)
                        {
                            var currentSymbol = trimmedAlt[i].ToString();

                            // Solo procesar no terminales
                            if (!IsNonTerminal(trimmedAlt[i]))
                                continue;

                            if (!follows.ContainsKey(currentSymbol))
                                follows[currentSymbol] = new HashSet<string>();

                            // Símbolos después del actual
                            bool allCanBeEpsilon = true;

                            for (int j = i + 1; j < trimmedAlt.Length; j++)
                            {
                                var nextSymbol = trimmedAlt[j].ToString();

                                if (IsTerminal(trimmedAlt[j]))
                                {
                                    // Agregar terminal a FOLLOW
                                    if (follows[currentSymbol].Add(nextSymbol))
                                        changed = true;
                                    allCanBeEpsilon = false;
                                    break;
                                }
                                else if (IsNonTerminal(trimmedAlt[j]))
                                {
                                    // Agregar FIRST del siguiente (sin ε)
                                    var firstOfNext = ComputeFirst(grammar, nextSymbol);

                                    foreach (var f in firstOfNext.Where(x => x != "ε"))
                                    {
                                        if (follows[currentSymbol].Add(f))
                                            changed = true;
                                    }

                                    // Si no tiene ε, detenerse
                                    if (!firstOfNext.Contains("ε"))
                                    {
                                        allCanBeEpsilon = false;
                                        break;
                                    }
                                }
                            }

                            // Si está al final o todos pueden ser ε
                            if (allCanBeEpsilon && prod.NonTerminal != currentSymbol)
                            {
                                if (!follows.ContainsKey(prod.NonTerminal))
                                    follows[prod.NonTerminal] = new HashSet<string>();

                                // Agregar FOLLOW del lado izquierdo
                                var sizeBefore = follows[currentSymbol].Count;
                                follows[currentSymbol].UnionWith(follows[prod.NonTerminal]);

                                if (follows[currentSymbol].Count > sizeBefore)
                                    changed = true;
                            }
                        }
                    }
                }
            }

            if (iterations >= maxIterations)
            {
                throw new InvalidOperationException("Se alcanzó el límite de iteraciones calculando FOLLOW. Posible gramática inválida.");
            }

            _followCache = follows;
        }

        /// <summary>
        /// Calcula el conjunto PREDICT de una producción específica
        /// </summary>
        public HashSet<string> ComputePredict(Grammar grammar, Production production)
        {
            var predict = new HashSet<string>();

            try
            {
                if (string.IsNullOrEmpty(production.RightSide))
                {
                    predict.UnionWith(ComputeFollow(grammar, production.NonTerminal));
                    return predict;
                }

                var alternatives = production.RightSide.Split(',', StringSplitOptions.RemoveEmptyEntries);

                foreach (var alt in alternatives)
                {
                    var trimmedAlt = alt.Trim();

                    if (string.IsNullOrEmpty(trimmedAlt))
                    {
                        predict.UnionWith(ComputeFollow(grammar, production.NonTerminal));
                        continue;
                    }

                    bool allHaveEpsilon = true;

                    for (int i = 0; i < trimmedAlt.Length && allHaveEpsilon; i++)
                    {
                        var currentSymbol = trimmedAlt[i].ToString();

                        if (IsTerminal(trimmedAlt[i]))
                        {
                            predict.Add(currentSymbol);
                            allHaveEpsilon = false;
                        }
                        else if (IsNonTerminal(trimmedAlt[i]))
                        {
                            var firstOfSymbol = ComputeFirst(grammar, currentSymbol);

                            foreach (var f in firstOfSymbol.Where(x => x != "ε"))
                            {
                                predict.Add(f);
                            }

                            if (!firstOfSymbol.Contains("ε"))
                            {
                                allHaveEpsilon = false;
                            }
                        }
                    }

                    if (allHaveEpsilon)
                    {
                        predict.UnionWith(ComputeFollow(grammar, production.NonTerminal));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error calculando PREDICT de producción '{production.NonTerminal} -> {production.RightSide}': {ex.Message}", ex);
            }

            return predict;
        }

        /// <summary>
        /// Verifica si un símbolo es terminal (minúscula o no letra)
        /// </summary>
        private bool IsTerminal(char symbol)
        {
            return char.IsLower(symbol) || !char.IsLetter(symbol);
        }

        /// <summary>
        /// Verifica si un símbolo es no terminal (mayúscula)
        /// </summary>
        private bool IsNonTerminal(char symbol)
        {
            return char.IsUpper(symbol);
        }
    }
}