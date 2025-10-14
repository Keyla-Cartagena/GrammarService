using System.ComponentModel.DataAnnotations;

namespace GrammarService.Models
{
    /// <summary>
    /// Representa una gramática formal completa
    /// </summary>
    public class Grammar
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string StartSymbol { get; set; } = string.Empty;

        public ICollection<Production> Productions { get; set; } = new List<Production>();
    }
}