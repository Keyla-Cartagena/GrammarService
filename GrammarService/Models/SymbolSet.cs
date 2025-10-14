namespace GrammarService.Models
{
    /// <summary>
    /// Representa los conjuntos First, Follow y Predict de un símbolo no terminal
    /// </summary>
    public class SymbolSet
    {
        public string NonTerminal { get; set; } = string.Empty;
        public HashSet<string> First { get; set; } = new();
        public HashSet<string> Follow { get; set; } = new();
        public HashSet<string> Predict { get; set; } = new();
    }
}