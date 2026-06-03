using Agrovesto.Backend.Core.Models.Businesses.Input;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Infrastructure.Logic.Services
{
    public interface IRepaymentEngine
    {
        List<RepaymentSchedule> GenerateSchedule(RepaymentEngineRequest request);
    }

    public class RepaymentEngine : IRepaymentEngine
    {
        public List<RepaymentSchedule> GenerateSchedule(RepaymentEngineRequest request)
        {
            var schedule = new List<RepaymentSchedule>();

            var installmentCount = request.Duration;
            var amountPerInstallment = request.TotalAmount / installmentCount;

            var currentDate = request.StartDate;

            for (int i = 1; i <= installmentCount; i++)
            {
                currentDate = AddFrequency(currentDate.Value, request.Frequency);

                var isWithinGrace = currentDate <= request.GracePeriod;

                schedule.Add(new RepaymentSchedule
                {
                    InstallmentNumber = i,
                    DueDate = currentDate.Value,
                    Amount = amountPerInstallment,
                    IsWithinGracePeriod = isWithinGrace
                });
            }

            return schedule;
        }

        private DateTimeOffset AddFrequency(DateTimeOffset date, RepaymentFrequency frequency)
        {
            return frequency switch
            {
                RepaymentFrequency.Weekly => date.AddDays(7),
                RepaymentFrequency.BiWeekly => date.AddDays(14),
                RepaymentFrequency.Monthly => date.AddMonths(1),
                RepaymentFrequency.Quarterly => date.AddMonths(3),
                RepaymentFrequency.Annually => date.AddYears(1),
                _ => date
            };
        }
    }
}
