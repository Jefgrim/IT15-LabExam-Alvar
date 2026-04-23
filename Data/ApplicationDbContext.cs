using Microsoft.EntityFrameworkCore;
using TechCore.Models;

namespace TechCore.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<PayrollTransaction> PayrollTransactions => Set<PayrollTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.Property(employee => employee.DailyRate).HasPrecision(18, 2);
        });

        modelBuilder.Entity<PayrollTransaction>(entity =>
        {
            entity.Property(payroll => payroll.GrossPay).HasPrecision(18, 2);
            entity.Property(payroll => payroll.Deduction).HasPrecision(18, 2);
            entity.Property(payroll => payroll.NetPay).HasPrecision(18, 2);

            entity.HasOne(payroll => payroll.Employee)
                .WithMany(employee => employee.PayrollTransactions)
                .HasForeignKey(payroll => payroll.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}