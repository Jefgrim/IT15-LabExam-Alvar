using System.ComponentModel.DataAnnotations;

namespace TechCore.Models;

public class Employee
{
    public int EmployeeId { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Position { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Department { get; set; } = string.Empty;

    [Range(0.01, 999999999)]
    [DataType(DataType.Currency)]
    [Display(Name = "Daily Rate")]
    public decimal DailyRate { get; set; }

    public ICollection<PayrollTransaction> PayrollTransactions { get; set; } = new List<PayrollTransaction>();

    public string FullName => $"{FirstName} {LastName}";
}