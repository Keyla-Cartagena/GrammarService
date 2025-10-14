using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GrammarService.Models
{
    /// <summary>
    /// Representa una producción de una gramática: NonTerminal -> RightSide
    /// </summary>
    public class Production
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string NonTerminal { get; set; } = string.Empty;

        [Required]
        public string RightSide { get; set; } = string.Empty;

        [ForeignKey("Grammar")]
        public string GrammarId { get; set; } = string.Empty;

        [JsonIgnore]
        public Grammar? Grammar { get; set; }
    }
}