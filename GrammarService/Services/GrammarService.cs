using GrammarService.Models;

namespace GrammarService.Services
{
    public class GrammarService
    {
        /// <summary>
        /// Calcula el conjunto FIRST de un símbolo no terminal
        /// </summary>
        public HashSet<string> ComputeFirst(Grammar grammar, string symbol)
        {
            var first = new HashSet<string>();
            var visited = new HashSet<string>();

            ComputeFirstRecursive(grammar, symbol, first, visited);

            return first;
        }

        private void ComputeFirstRecursive(Grammar grammar, string symbol, HashSet<string> first, HashSet<string> visited)
        {
            // Evitar ciclos infinitos
            if (visited.Contains(symbol))
                return;

            visited.Add(symbol);

            // Obtener todas las producciones del símbolo
            var productions = grammar.Productions.Where(p => p.NonTerminal == symbol);

            foreach (var prod in productions)
            {
                if (string.IsNullOrEmpty(prod.RightSide))
                {
                    first.Add("ε"); // Producción vacía
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

                    // Obtener el primer símbolo
                    var firstChar = trimmedAlt[0].ToString();

                    // Si es terminal (minúscula o símbolo especial)
                    if (char.IsLower(trimmedAlt[0]) || !char.IsLetter(trimmedAlt[0]))
                    {
                        first.Add(firstChar);
                    }
                    // Si es no terminal (mayúscula)
                    else if (char.IsUpper(trimmedAlt[0]))
                    {
                        ComputeFirstRecursive(grammar, firstChar, first, visited);
                    }
                }
            }
        }

        /// <summary>
        /// Calcula el conjunto FOLLOW de un símbolo no terminal
        /// </summary>
        public HashSet<string> ComputeFollow(Grammar grammar, string symbol)
        {
            var follow = new HashSet<string>();

            // Regla 1: $ está en FOLLOW del símbolo inicial
            if (symbol == grammar.StartSymbol)
            {
                follow.Add("$");
            }

            // Buscar todas las producciones donde aparece el símbolo
            foreach (var prod in grammar.Productions)
            {
                var alternatives = prod.RightSide.Split(',', StringSplitOptions.RemoveEmptyEntries);

                foreach (var alt in alternatives)
                {
                    var trimmedAlt = alt.Trim();
                    int index = trimmedAlt.IndexOf(symbol);

                    if (index >= 0)
                    {
                        // Si hay símbolos después
                        if (index + 1 < trimmedAlt.Length)
                        {
                            var nextChar = trimmedAlt[index + 1].ToString();

                            // Si el siguiente es terminal
                            if (char.IsLower(trimmedAlt[index + 1]) || !char.IsLetter(trimmedAlt[index + 1]))
                            {
                                follow.Add(nextChar);
                            }
                            // Si el siguiente es no terminal
                            else if (char.IsUpper(trimmedAlt[index + 1]))
                            {
                                var firstOfNext = ComputeFirst(grammar, nextChar);
                                foreach (var f in firstOfNext)
                                {
                                    if (f != "ε")
                                        follow.Add(f);
                                }

                                // Si FIRST del siguiente contiene ε, agregar FOLLOW del no terminal actual
                                if (firstOfNext.Contains("ε"))
                                {
                                    if (prod.NonTerminal != symbol)
                                    {
                                        var followOfProduction = ComputeFollow(grammar, prod.NonTerminal);
                                        follow.UnionWith(followOfProduction);
                                    }
                                }
                            }
                        }
                        // Si está al final de la producción
                        else
                        {
                            if (prod.NonTerminal != symbol)
                            {
                                var followOfProduction = ComputeFollow(grammar, prod.NonTerminal);
                                follow.UnionWith(followOfProduction);
                            }
                        }
                    }
                }
            }

            return follow;
        }

        /// <summary>
        /// Calcula el conjunto PREDICT de una producción específica
        /// </summary>
        public HashSet<string> ComputePredict(Grammar grammar, Production production)
        {
            var predict = new HashSet<string>();

            // PREDICT = FIRST(α) si ε no está en FIRST(α)
            // PREDICT = (FIRST(α) - {ε}) ∪ FOLLOW(A) si ε está en FIRST(α)

            if (string.IsNullOrEmpty(production.RightSide))
            {
                // Producción vacía: PREDICT = FOLLOW(A)
                predict.UnionWith(ComputeFollow(grammar, production.NonTerminal));
            }
            else
            {
                var alternatives = production.RightSide.Split(',', StringSplitOptions.RemoveEmptyEntries);

                foreach (var alt in alternatives)
                {
                    var trimmedAlt = alt.Trim();

                    if (string.IsNullOrEmpty(trimmedAlt))
                    {
                        predict.UnionWith(ComputeFollow(grammar, production.NonTerminal));
                        continue;
                    }

                    var firstChar = trimmedAlt[0].ToString();

                    // Terminal
                    if (char.IsLower(trimmedAlt[0]) || !char.IsLetter(trimmedAlt[0]))
                    {
                        predict.Add(firstChar);
                    }
                    // No terminal
                    else if (char.IsUpper(trimmedAlt[0]))
                    {
                        var firstOfSymbol = ComputeFirst(grammar, firstChar);

                        foreach (var f in firstOfSymbol)
                        {
                            if (f != "ε")
                                predict.Add(f);
                        }

                        // Si contiene ε, agregar FOLLOW
                        if (firstOfSymbol.Contains("ε"))
                        {
                            predict.UnionWith(ComputeFollow(grammar, production.NonTerminal));
                        }
                    }
                }
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