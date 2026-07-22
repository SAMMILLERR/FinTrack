using System.ComponentModel.DataAnnotations;

namespace FinTrack.Models;

public class User
{
    public int UserId { get; set; }

public string FirstName { get; set; } = "";

public string LastName { get; set; } = "";

public string Email { get; set; } = "";

public string PasswordHash { get; set; } = "";

public int RoleId { get; set; }

public string RoleName { get; set; } = "";

public bool IsActive { get; set; }

public DateTime CreatedOn { get; set; }
}