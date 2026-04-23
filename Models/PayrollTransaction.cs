using System.ComponentModel.DataAnnotations;

namespace TechCore.Models;

public class PayrollTransaction
{
    public int PayrollTransactionId { get; set; }

    [Display(Name = "Employee")]
    [Required]
    public int EmployeeId { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Date")]
    public DateTime Date { get; set; } = DateTime.Today;

    [Range(0, int.MaxValue, ErrorMessage = "Days worked cannot be negative.")]
    [Display(Name = "Days Worked")]
    public int DaysWorked { get; set; }

    [DataType(DataType.Currency)]
    [Display(Name = "Gross Pay")]
    public decimal GrossPay { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Deduction cannot be negative.")]
    [DataType(DataType.Currency)]
    public decimal Deduction { get; set; }

    [DataType(DataType.Currency)]
    [Display(Name = "Net Pay")]
    public decimal NetPay { get; set; }

    public Employee? Employee { get; set; }
}