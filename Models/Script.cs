using System.ComponentModel.DataAnnotations;

namespace EduScriptAI.Models
{
    public class Script
    {
        public int Id { get; set; }
        
        [Required]
        public string Keywords { get; set; } = string.Empty;
        
        [Required]
        public string Level { get; set; } = string.Empty;
        
        [Required]
        public string Type { get; set; } = string.Empty;
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
    }
}
