using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TechCore.Data;
using TechCore.Models;
using TechCore.ViewModels;

namespace TechCore.Controllers;

public class PayrollController : Controller
{
    private readonly ApplicationDbContext _context;

    public PayrollController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int? employeeId)
    {
        var query = _context.PayrollTransactions
            .Include(payroll => payroll.Employee)
            .AsNoTracking()
            .OrderByDescending(payroll => payroll.Date)
            .ThenByDescending(payroll => payroll.PayrollTransactionId)
            .AsQueryable();

        if (employeeId.HasValue)
        {
            query = query.Where(payroll => payroll.EmployeeId == employeeId.Value);
        }

        ViewBag.Employees = await BuildEmployeeSelectListAsync(employeeId);

        var payrollItems = await query.ToListAsync();
        return View(payrollItems);
    }

    public async Task<IActionResult> History(int employeeId)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.EmployeeId == employeeId);

        if (employee == null)
        {
            return NotFound();
        }

        var payrollItems = await _context.PayrollTransactions
            .Include(payroll => payroll.Employee)
            .AsNoTracking()
            .Where(payroll => payroll.EmployeeId == employeeId)
            .OrderByDescending(payroll => payroll.Date)
            .ToListAsync();

        ViewBag.EmployeeName = employee.FullName;
        ViewBag.EmployeeId = employee.EmployeeId;

        return View(payrollItems);
    }

    public async Task<IActionResult> Create()
    {
        await SetEmployeeRatesViewBagAsync();
        return View(await BuildFormViewModelAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PayrollFormViewModel viewModel)
    {
        await PopulateSelectListAsync(viewModel.EmployeeId);

        var employee = await _context.Employees.FirstOrDefaultAsync(item => item.EmployeeId == viewModel.EmployeeId);

        if (employee == null)
        {
            ModelState.AddModelError(nameof(viewModel.EmployeeId), "Selected employee was not found.");
        }

        if (ModelState.IsValid && employee != null)
        {
            var payroll = MapToEntity(viewModel, employee.DailyRate);
            _context.PayrollTransactions.Add(payroll);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        viewModel.Employees = await BuildEmployeeSelectListAsync(viewModel.EmployeeId);
        await SetEmployeeRatesViewBagAsync();
        return View(viewModel);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var payroll = await _context.PayrollTransactions
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.PayrollTransactionId == id);

        if (payroll == null)
        {
            return NotFound();
        }

        var employee = await _context.Employees.AsNoTracking().FirstOrDefaultAsync(item => item.EmployeeId == payroll.EmployeeId);

        var viewModel = new PayrollFormViewModel
        {
            PayrollTransactionId = payroll.PayrollTransactionId,
            EmployeeId = payroll.EmployeeId,
            Date = payroll.Date,
            DaysWorked = payroll.DaysWorked,
            Deduction = payroll.Deduction,
            GrossPay = payroll.GrossPay,
            NetPay = payroll.NetPay,
            EmployeeName = employee?.FullName,
            Employees = await BuildEmployeeSelectListAsync(payroll.EmployeeId)
        };

        await SetEmployeeRatesViewBagAsync();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PayrollFormViewModel viewModel)
    {
        if (id != viewModel.PayrollTransactionId)
        {
            return NotFound();
        }

        var employee = await _context.Employees.FirstOrDefaultAsync(item => item.EmployeeId == viewModel.EmployeeId);

        if (employee == null)
        {
            ModelState.AddModelError(nameof(viewModel.EmployeeId), "Selected employee was not found.");
        }

        if (ModelState.IsValid && employee != null)
        {
            var payroll = await _context.PayrollTransactions.FirstOrDefaultAsync(item => item.PayrollTransactionId == id);

            if (payroll == null)
            {
                return NotFound();
            }

            payroll.EmployeeId = viewModel.EmployeeId;
            payroll.Date = viewModel.Date;
            payroll.DaysWorked = viewModel.DaysWorked;
            payroll.Deduction = viewModel.Deduction;
            CalculatePayroll(payroll, employee.DailyRate);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        viewModel.Employees = await BuildEmployeeSelectListAsync(viewModel.EmployeeId);
        viewModel.EmployeeName = employee?.FullName;
        viewModel.GrossPay = viewModel.DaysWorked * (employee?.DailyRate ?? 0m);
        viewModel.NetPay = viewModel.GrossPay - viewModel.Deduction;
        await SetEmployeeRatesViewBagAsync();
        return View(viewModel);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var payroll = await _context.PayrollTransactions
            .Include(item => item.Employee)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.PayrollTransactionId == id);

        if (payroll == null)
        {
            return NotFound();
        }

        return View(payroll);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var payroll = await _context.PayrollTransactions.FindAsync(id);

        if (payroll == null)
        {
            return NotFound();
        }

        _context.PayrollTransactions.Remove(payroll);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task<PayrollFormViewModel> BuildFormViewModelAsync(int? employeeId = null)
    {
        return new PayrollFormViewModel
        {
            Date = DateTime.Today,
            Employees = await BuildEmployeeSelectListAsync(employeeId)
        };
    }

    private async Task<IEnumerable<SelectListItem>> BuildEmployeeSelectListAsync(int? selectedEmployeeId)
    {
        var employees = await _context.Employees
            .AsNoTracking()
            .OrderBy(employee => employee.LastName)
            .ThenBy(employee => employee.FirstName)
            .ToListAsync();

        return employees.Select(employee => new SelectListItem
        {
            Value = employee.EmployeeId.ToString(),
            Text = $"{employee.FullName} ({employee.Position})",
            Selected = selectedEmployeeId.HasValue && employee.EmployeeId == selectedEmployeeId.Value
        });
    }

    private async Task PopulateSelectListAsync(int? selectedEmployeeId)
    {
        ViewBag.Employees = await BuildEmployeeSelectListAsync(selectedEmployeeId);
    }

    private async Task SetEmployeeRatesViewBagAsync()
    {
        var rates = await _context.Employees
            .AsNoTracking()
            .ToDictionaryAsync(employee => employee.EmployeeId, employee => employee.DailyRate);

        ViewBag.EmployeeRates = JsonSerializer.Serialize(rates);
    }

    private static PayrollTransaction MapToEntity(PayrollFormViewModel viewModel, decimal dailyRate)
    {
        var payroll = new PayrollTransaction
        {
            EmployeeId = viewModel.EmployeeId,
            Date = viewModel.Date,
            DaysWorked = viewModel.DaysWorked,
            Deduction = viewModel.Deduction
        };

        CalculatePayroll(payroll, dailyRate);
        return payroll;
    }

    private static void CalculatePayroll(PayrollTransaction payroll, decimal dailyRate)
    {
        payroll.GrossPay = payroll.DaysWorked * dailyRate;
        payroll.NetPay = payroll.GrossPay - payroll.Deduction;
    }
}