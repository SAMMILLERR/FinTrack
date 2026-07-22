using System.ComponentModel.DataAnnotations;

namespace FinTrack.Models;

public class Category
{
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Category Name is required")]
    [StringLength(50)]
    public string CategoryName { get; set; } = "";

    [Required]
    public int CategoryTypeId { get; set; }

    public string CategoryTypeName { get; set; } = "";

    public bool IsActive { get; set; } = true;
}