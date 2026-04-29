using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Models.Businesses.Input
{
  
    public class RepaymentBreakdownDto
    {
        public DateTime Date { get; set; }
        public decimal LoanAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public string MethodOfPayment { get; set; }
        public decimal BalanceRemaining { get; set; }
    }
    public class FinancingRepaymentSummaryDto
    {
        public decimal TotalExpectedRepaymentValue { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal RepaymentPercentage { get; set; }
    }
    public class InputBreakdownDto
    {
        public string InputName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalCost { get; set; }
    }
    public class InstallmentDto
    {
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string Reference { get; set; }
    }
    public class RepaymentScheduleDto
    {
        public int InstallmentNo { get; set; }
        public DateTime DueDate { get; set; }
        public decimal ExpectedAmount { get; set; }
        public bool IsPaid { get; set; }
    }
    public class LoanDetailDto
    {
        public string LoanName { get; set; }
        public decimal LoanAmount { get; set; }
        public DateTime DateIssued { get; set; }
        public DateTime DueDate { get; set; }
        public decimal InterestRate { get; set; }

        public List<InputBreakdownDto> Inputs { get; set; }
        public List<InstallmentDto> InstallmentsPaid { get; set; }
        public List<RepaymentScheduleDto> RepaymentSchedule { get; set; }
    }
    public class InputLoanSummaryDto
    {
        public decimal TotalLoansIssued { get; set; }
        public decimal TotalActiveLoans { get; set; }
        public decimal TotalRepaid { get; set; }
        public decimal TotalOutstandingBalance { get; set; }
    }
    public class FinancingBreakdownDto
    {
        public string LoanName { get; set; }
        public decimal Amount { get; set; }
        public DateTime DateIssued { get; set; }
        public DateTime DueDate { get; set; }
        public decimal RepaymentPercentage { get; set; }
        public string Status { get; set; }
    }
}
