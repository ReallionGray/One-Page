namespace OnePage.Hr;

// Tax computation engine
public static class TaxCalculator
{
    public static decimal CalculateTax(decimal grossPay, string taxCode, string countryCode = "US")
    {
        // Simplified tax calculation - in production, this would use actual tax tables
        var taxRate = GetTaxRate(taxCode, countryCode);
        return grossPay * taxRate;
    }

    public static decimal GetTaxRate(string taxCode, string countryCode)
    {
        // Simplified tax rates - in production, use actual tax brackets
        return taxCode.ToUpper() switch
        {
            "STANDARD" => 0.20m,
            "MARRIED" => 0.18m,
            "SINGLE" => 0.22m,
            "HEAD" => 0.15m,
            _ => 0.20m
        };
    }

    public static decimal CalculateProgressiveTax(decimal grossPay, string countryCode = "US")
    {
        // Simplified progressive tax calculation
        return grossPay switch
        {
            <= 10000 => grossPay * 0.10m,
            <= 40000 => 1000 + (grossPay - 10000) * 0.12m,
            <= 85000 => 4600 + (grossPay - 40000) * 0.22m,
            <= 160000 => 14500 + (grossPay - 85000) * 0.24m,
            _ => 33000 + (grossPay - 160000) * 0.32m
        };
    }
}

// Pension calculation engine
public static class PensionCalculator
{
    public static decimal CalculatePensionContribution(decimal grossPay, decimal pensionRate, bool employerMatch = true, decimal employerMatchRate = 0.05m)
    {
        var employeeContribution = grossPay * pensionRate;
        var employerContribution = employerMatch ? grossPay * employerMatchRate : 0;
        return employeeContribution + employerContribution;
    }

    public static decimal CalculateEmployeePension(decimal grossPay, decimal pensionRate)
    {
        return grossPay * pensionRate;
    }

    public static decimal CalculateEmployerMatch(decimal grossPay, decimal matchRate)
    {
        return grossPay * matchRate;
    }
}

// Payroll advance calculations
public static class PayrollAdvanceCalculator
{
    public static decimal CalculateAdvanceAmount(decimal monthlySalary, decimal maxAdvancePercentage = 0.5m)
    {
        return monthlySalary * maxAdvancePercentage;
    }

    public static decimal CalculateAdvanceRepayment(decimal advanceAmount, int payrollPeriods, decimal interestRate = 0)
    {
        if (interestRate == 0)
        {
            return advanceAmount / payrollPeriods;
        }
        // Simple interest calculation
        var totalInterest = advanceAmount * interestRate;
        return (advanceAmount + totalInterest) / payrollPeriods;
    }
}
