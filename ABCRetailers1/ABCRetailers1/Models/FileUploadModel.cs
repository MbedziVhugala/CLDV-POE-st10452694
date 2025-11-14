using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models
{
    public class FileUploadModel
    {
        [Required]
        public IFormFile ProofOfPayment { get; set; } = null!;

        public string? OrderId { get; set; }

        public string? CustomerName { get; set; }
    }
}