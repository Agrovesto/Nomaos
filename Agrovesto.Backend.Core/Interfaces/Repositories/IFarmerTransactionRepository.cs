using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Models.Businesses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Repositories
{
    public interface IFarmerTransactionRepository
    {
        Task<List<FarmerSalesTrackerModel>> FilterFarmerTransactionForDownload(string? businessCode, string? farmerId, string? state, string? marketDay, string? marketName, string? cropName, DateTime? startDate, DateTime? endDate);
        Task<PagedListResponse<List<FarmerSalesTrackerModel>>> GetFarmerSalesTransactionsByCooperativeAsync(long cooperativeId, int pageNumber = 1, int pageSize = 10, string? transactionType = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, string? location = null);
        Task<PagedListResponse<List<FarmerSalesTrackerModel>>> GetTransactionByAgent(string agentId, int pageNumber = 1, int pageSize = 10, string? transactionType = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, string? location = null);
        Task<PagedListResponse<List<FarmerSalesTrackerModel>>> GetTransactionByBusiness(string businessCode, int pageNumber = 1, int pageSize = 10, string? transactionType = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, string? location = null);

        //TODO: get farmer transactions by userId
        //TODO: log farmer transaction
        //TODO: get all farmertransaction by agent
        //TODO: get all farmertransaction by cooperative
        //TODO: get all farmertransaction
        //TODO: filter transaction (pagesiez,pagenumber, transactiontype, duration,location
        //TODO: calculate total transaction volume
        //TODO: calculate total transaction volumen based on transaction type
        //TODO: calculate transaction performance (daily, weekly, monthly, yearly)
        //TODO: get most transacted product
        //TODO: get most transacted product by farmer
        //TODO: get most transacted product by location
        Task<PagedListResponse<List<FarmerSalesTrackerModel>>> GetTransactionByFarmer(string farmerId, int pageNumber = 1, int pageSize = 10, string? transactionType = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, string? location = null);
        Task<PagedListResponse<List<FarmerSalesTrackerModel>>> GetTransactions(int pageNumber = 1, int pageSize = 10, string? transactionType = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, string? location = null);
        Task<PagedListResponse<List<FarmerSalesTrackerModel>>> GetTransactionsByBusinessCode(string businessCode, int pageNumber = 1, int pageSize = 10, string? transactionType = null, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, string? location = null);
        Task<(bool status, FarmerSalesTrackerModel data)> LogTransaction(FarmerSalesTrackerModel data);
    }
}
