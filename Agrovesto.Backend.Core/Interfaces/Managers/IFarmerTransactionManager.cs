using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Businesses.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agrovesto.Backend.Core.Interfaces.Managers
{
    public interface IFarmerTransactionManager
    {
        Task<PagedResult<InputObligationDto>> GetFarmerInputObligations(ListQueryParams args);
        Task<PagedListResponse<List<FarmerSalesTrackerModel>>> GetFarmerSalesTransactionsByCooperativeAsync(TransactionQueryParams query);
        Task<PagedListResponse<List<FarmerSalesTrackerModel>>> GetFarmersTransactionByAgent(TransactionQueryParams query);

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
        Task<PagedListResponse<List<FarmerSalesTrackerModel>>> GetFarmerTransactions(TransactionQueryParams query);
        Task<PagedListResponse<List<FarmerSalesTrackerModel>>> GetTransactionByFarmer(long farmerId, TransactionQueryParams query);
        Task<PagedListResponse<List<FarmerSalesTrackerModel>>> GetTransactionByFarmerByUserId(TransactionQueryParams query);
        Task<PagedListResponse<List<FarmerSalesTrackerModel>>> GetTransactionsByBusinessCode(TransactionQueryParams query);
        Task<FarmerSalesTrackerModel> LogOfflineTransaction(List<FarmerSalesTrackerModel> request, TransactionQueryParams args,  long farmerId );
        //Task<FarmerSalesTrackerModel> LogOfflineTransaction(FarmerSalesTrackerModel model, TransactionQueryParams args, long farmerId);
    }
}
