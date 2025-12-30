
//using Agrovesto.Backend.Core.CoreUtils;
//using Agrovesto.Backend.Core.Exceptions;
//using Agrovesto.Backend.Core.Interfaces.Repositories;
//using Agrovesto.Backend.Core.Models.Businesses;
//using Agrovesto.Backend.Core.Models.Market;
//using Agrovesto.Backend.Infrastructure.Entities.Common;
//using Agrovesto.Backend.Infrastructure.Logic.Repositories;
//using DocumentFormat.OpenXml.Bibliography;
//using DocumentFormat.OpenXml.InkML;
//using DocumentFormat.OpenXml.Spreadsheet;
//using Microsoft.Extensions.FileSystemGlobbing.Internal;
//using Org.BouncyCastle.Utilities.Collections;
//using SendGrid.Helpers.Mail;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.InteropServices;
//using System.Text;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;

//namespace Agrovesto.Backend.Infrastructure.Logic.Managers
//{
//    public interface IMarketManager
//    {
//        Task<long> CreateMarketAsync(MarketDto market, BusinessRegistrationArgs args);
//        Task<MarketDto?> GetMarketAsync(long marketId);
//    }
//    public class MarketManager :IMarketManager
//    {
//        private readonly IMarketRepository _marketRepo;
//        private readonly IBusinessRepository _businessRepository;
//        public MarketManager(IMarketRepository marketRepo, IBusinessRepository businessRepository)
//        {
//            _marketRepo = marketRepo;
//            _businessRepository = businessRepository;
//        }
//        public async Task<long> CreateMarketAsync(MarketDto market,
//       BusinessRegistrationArgs args)
//        {
//            var business = await _businessRepository.GetBusinessByCodeAsync(args.businessCode)
//              ?? throw new BaseException(Core.Enums.ExceptionType.NOTFOUND, "Business does not exist.");

//            var entity = new Market
//            {
//                Name = market.Name,
//                MarketType = market.MarketType.ToEnum<MarketType>(),
//                StateId = market.StateId,
//                LgaId = market.LgaId,
//                Latitude = market.Latitude,
//                Longitude = market.Longitude,
//                EstimatedCapacityKg = market.EstimatedCapacityKg,
//                Location = market.Location,
//                CreatedBy = args.UserId,//market.CreatedBy,
//                CreatedOn = DateTimeOffset.UtcNow,
//                BelongToBusinessCode=args.businessCode,
//                OrganizationCode=args.organizationCode,
//            };

//            if (market.MarketDays.Any() == true)
//            {
//                foreach (var day in market.MarketDays)
//                {
//                    entity.MarketDays.Add(new MarketDay
//                    {
//                        DayType = day.DayType.ToEnum<MarketDayType>(),
//                        DayOfWeek = day.DayOfWeek.ToEnum<DayOfWeek>(),
//                        CycleDay = day.CycleDay,
//                        IsActive = true
//                    });
//                }
//            }

//            if (market.CommodityIds?.Any() == true)
//            {
//                foreach (var commodity in market.CommodityIds)
//                {
//                    entity.MarketCommodities.Add(new MarketCommodity
//                    {
//                        CommodityId = commodity
//                    });
//                }
//            }

//            await _marketRepo.AddAsync(entity);
//            await _marketRepo.SaveChangesAsync();

//            return entity.Id;
//        }


//        public Task<MarketDto?> GetMarketAsync(long marketId)
//        {
//            return _marketRepo.GetByIdAsync(marketId);
//        }

//        public TransactionTrustResult ComputeTrustScore(
//    MarketTransaction txn,
//    decimal marketAveragePrice)
//        {
//            var result = new TransactionTrustResult();
//            int score = 0;

//            /* 1️⃣ Source Credibility (40) */
//            score += txn.Source switch
//            {
//                TransactionSource.Warehouse => 40,
//                TransactionSource.Agent => 30,
//                TransactionSource.Trader => 20,
//                _ => 10
//            };

//            /* 2️⃣ Price Consistency (25) */
//            var deviation = Math.Abs(txn.PricePerKg - marketAveragePrice)
//                            / marketAveragePrice;

//            if (deviation <= 0.05m) score += 25;
//            else if (deviation <= 0.15m) score += 15;
//            else
//            {
//                score += 5;
//                result.Flags.Add("Price outlier");
//            }

//            /* 3️⃣ Quantity Reasonableness (15) */
//            if (txn.QuantityKg > 0 && txn.QuantityKg <= 100_000)
//                score += 15;
//            else
//            {
//                score += 5;
//                result.Flags.Add("Unusual quantity");
//            }

//            /* 4️⃣ Actor Reputation (10) */
//            // Placeholder – replace with real reputation score
//            score += 8;

//            /* 5️⃣ Data Completeness (10) */
//            if (txn.CommodityVarietyId != null &&
//                txn.MarketDayId > 0 &&
//                txn.SellerId != null)
//                score += 10;
//            else
//                score += 5;

//            result.Score = Math.Min(score, 100);
//            return result;
//        }

//    }
//}
