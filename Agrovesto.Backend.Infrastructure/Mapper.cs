using Agrovesto.Backend.Core.CoreUtils;
using Agrovesto.Backend.Core.Models.Businesses;
using Agrovesto.Backend.Core.Models.Common;
using Agrovesto.Backend.Core.Models.Common.WalletServiceRequest;
using Agrovesto.Backend.Core.Models.Payment;
using Agrovesto.Backend.Infrastructure.Entities.Businesses;
using Agrovesto.Backend.Infrastructure.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Amazon.S3.Util.S3EventNotification;

namespace Agrovesto.Backend.Infrastructure
{
    public static class Mapper
    {
        #region payout
        public static Payout Map(this PayoutModel model)
        {
            if(model ==null)
            {
                return null;
            }
            return new Payout
            {
                Amount = model.Amount,
                CreatedOn = model.CreatedOn,
                DestinationAccountName = model.DestinationAccountName,
                DestinationAccountNumber = model.DestinationAccountNumber,
                DestinationBank=model.DestinationBank,
                IsSuccessfulTransaction=model.IsSuccessfulTransaction,
                SourceAccountName = model.SourceAccountName,
                SourceAccountNumber = model.SourceAccountNumber,
                SourceBankCode=model.SourceBankCode,
                UserId=model.UserId,
                Reference=model.Reference
            };
        }

        public static PayoutModel Map(this Payout model)
        {
            if (model == null)
            {
                return null;
            }
            return new PayoutModel
            {Id=model.Id,
                Amount = model.Amount,
                CreatedOn = model.CreatedOn,
                DestinationAccountName = model.DestinationAccountName,
                DestinationAccountNumber = model.DestinationAccountNumber,
                DestinationBank = model.DestinationBank,
                IsSuccessfulTransaction = model.IsSuccessfulTransaction,
                SourceAccountName = model.SourceAccountName,
                SourceAccountNumber = model.SourceAccountNumber,
                SourceBankCode = model.SourceBankCode,
                UserId = model.UserId,
                Reference=model.Reference
            };
        }
        #endregion payout
        #region paymentProvider
        public static PaymentProvider Map(this PaymentProviderModel model)
        {
            if (model == null)
            {
                return null;
            }
            return new PaymentProvider()
            {
                Code = model.Code,
                Name = model.Name,
                ProviderId = model.ProviderId,
                PaymentProviderParameters = model.PaymentProviderParameters.Select(x => x.Map()).ToList(),
                Status = model.Status.ToString()
            };
        }

        public static PaymentProviderModel Map(this PaymentProvider entity)
        {
            if (entity == null)
            {
                return null;
            }
            return new PaymentProviderModel()
            {
                Code = entity.Code,
                Name = entity.Name,
                ProviderId = entity.ProviderId,
                PaymentProviderParameters = entity.PaymentProviderParameters.Select(x => x.Map()).ToList(),
                Status = entity.Status.ToEnum<Core.Models.Payment.ProviderStatus>()
            };
        }
        public static PaymentProviderParameter Map(this ProviderParameterModel model)
        {
            return new PaymentProviderParameter()
            {
                Description = model.Description,
                Key = model.Key,
                ParameterId = model.ParameterId,
                PaymentProviderProviderId = model.ProviderId,
                Value = model.Value,
                Env = model.Env.ToString()
            };
        }
        public static ProviderParameterModel Map(this PaymentProviderParameter model)
        {
            return new ProviderParameterModel()
            {
                Description = model.Description,
                Key = model.Key,
                ParameterId = model.ParameterId,
                ProviderId = model.PaymentProviderProviderId,
                Value = model.Value,
                Env = model.Env.ToEnum<Env>()
            };
        }

        #endregion 
        #region FarmMgt
        public static FarmPlotDetailsModel Map(this FarmPlotDetails entity)
        {
            if (entity == null)
            {
                return null;
            }
            return new FarmPlotDetailsModel
            {
                FarmState = entity.FarmState,
                FarmerId = entity.FarmerId,
                FarmerName = entity.FarmerName,
                FarmLga = entity.FarmLga,
                FarmLocation = entity.FarmLocation,
                Id = entity.Id,
                Latitude1 = entity.Latitude1,
                Latitude2 = entity.Latitude2,
                Latitude3 = entity.Latitude3,
                Latitude4 = entity.Latitude4,
                Longitude1 = entity.Longitude1,
                Longitude2 = entity.Longitude2,
                Longitude3 = entity.Longitude3,
                Longitude4 = entity.Longitude4,
                AreaInDegress = entity.AreaInDegress,
                AreaInSqMetre = entity.AreaInSqMetre,
                AreaInHectare = entity.AreaInHectare,
                CoordinateTakenBy = entity.CoordinateTakenBy,
                CoordinateTakenByUserId = entity.CoordinateTakenByUserId,
                BelongToBusinessCode = entity.BelongToBusinessCode,
                CreatedOn = entity.CreatedOn,
            };
        }

        public static FarmPlotDetails Map(this FarmPlotDetailsModel model)
        {
            if (model == null)
            {
                return null;
            }
            return new FarmPlotDetails
            {
                FarmState = model.FarmState,
                FarmerId = model.FarmerId,
                FarmerName = model.FarmerName,
                FarmLga = model.FarmLga,
                FarmLocation = model.FarmLocation,
                Latitude1 = model.Latitude1,
                Latitude2 = model.Latitude2,
                Latitude3 = model.Latitude3,
                Latitude4 = model.Latitude4,
                Longitude1 = model.Longitude1,
                Longitude2 = model.Longitude2,
                Longitude3 = model.Longitude3,
                Longitude4 = model.Longitude4,
                BelongToBusinessCode = model.BelongToBusinessCode,
                AreaInDegress = model.AreaInDegress,
                AreaInSqMetre = model.AreaInSqMetre,
                AreaInHectare = model.AreaInHectare,
                CoordinateTakenBy = model.CoordinateTakenBy,
                CoordinateTakenByUserId = model.CoordinateTakenByUserId,
                CreatedOn = model.CreatedOn,
            };
        }
        public static FarmActivityTypeModel Map(this FarmActivityType entity)
        {
            if (entity == null)
            {
                return null;
            }
            return new FarmActivityTypeModel
            {
                Description = entity.Description,
                Id = entity.Id,
                Name = entity.Name,
                BelongToBusinessCode = entity.BelongToBusinessCode,
            };
        }

        public static FarmActivityType Map(this FarmActivityTypeModel model)
        {
            if (model == null)
            {
                return null;
            }
            return new FarmActivityType
            {
                Description = model.Description,
                Name = model.Name,

                BelongToBusinessCode = model.BelongToBusinessCode,
            };
        }

        public static FarmVisitationActivityModel Map(this FarmVisitationActivity entity)
        {
            if (entity == null)
            {
                return null;
            }

            return new FarmVisitationActivityModel
            {
                FarmActivityId = entity.FarmActivityId,
                CreatedBy = entity.CreatedBy,
                CreatedOn = entity.CreatedOn,
                Description
                = entity.Description,
                FarmerName = entity.FarmerName,
                FarmerUserId = entity.FarmerUserId,
                Id = entity.Id,
                OfficerInChargeId = entity.OfficerInChargeId,
                Recommendation = entity.Recommendation,
                CooperativeId = entity.CooperativeId,
                BelongToBusinessCode = entity.BelongToBusinessCode,
                OfficerName = entity.OfficerName,
                FarmerLabourCost = entity.FarmerLabourCost,
                InputCost = entity.InputCost,
                FarmerTransportCost = entity.FarmerTransportCost,
                OtherCost = entity.OtherCost,
                TotalCost = entity.TotalCost,
                InputQuantity = entity.InputQuantity,
                InputType = entity.InputType,

            };
        }

        public static FarmVisitationActivity Map(this FarmVisitationActivityModel model)
        {
            if (model == null)
            {
                return null;
            }

            return new FarmVisitationActivity
            {
                FarmActivityId = model.FarmActivityId,
                CreatedBy = model.CreatedBy,
                CreatedOn = model.CreatedOn,
                Description
                = model.Description,
                FarmerName = model.FarmerName,
                FarmerUserId = model.FarmerUserId,
                CooperativeId = model.CooperativeId,
                BelongToBusinessCode = model.BelongToBusinessCode,
                OfficerInChargeId = model.OfficerInChargeId,
                Recommendation = model.Recommendation,
                OfficerName = model.OfficerName,
                FarmerLabourCost = model.FarmerLabourCost,
                InputCost = model.InputCost,
                FarmerTransportCost = model.FarmerTransportCost,
                OtherCost = model.OtherCost,
                TotalCost = model.TotalCost,
                InputQuantity = model.InputQuantity,
                InputType = model.InputType,
            };
        }
        #endregion FarmMgt

        #region Bundle
        public static BundleSubscriptionInterestModel Map(this BundleSubscriptionInterest entity)
        {
            if (entity == null)
            {
                return null;
            }
            return new BundleSubscriptionInterestModel
            {
                OfficerInChargeUserId = entity.OfficerInChargeUserId,
                OnBoardedOn = entity.CreatedOn,
                CommissionPercentage = entity.CommissionPercentage,
                CommissionPrice = entity.CommissionPrice,
                EquityPlusCommission = entity.EquityPlusCommission,
                ShouldAddCommission = entity.ShouldAddCommission,
                Status = entity.PaymentStatus.ToEnum<EquityPaymentStatus>(),
                EquitySubscriptionStatus = entity.SubscriptionStatus.ToEnum<InputFinancingStatus>(),
                EquityPaymentBalanceDeficit = entity.EquityPaymentBalanceDeficit,
                ExpectedEquityPricePerBundle = entity.ExpectedEquityPricePerBundle,
                ExpectedTotalEquityPrice = entity.ExpectedTotalEquityPrice,
                FarmerName = entity.FarmerName,
                PaymentDate = entity.PaymentDate,
                FarmerUserId = entity.FarmerUserId,
                InterestedBundleId = entity.InterestedBundleId,
                InterestedBundleName = entity.InterestedBundleName,
                IsCommissionPaidOut = entity.IsCommissionPaidOut,
                Id = entity.Id,
                QuantityOfInterest = entity.QuantityOfInterest,
                TotalEquityPaid = entity.TotalEquityPaid,
                TotalQuantityPaidFor = entity.TotalQuantityPaidFor,
                BelongToBusinessCode = entity.BelongToBusinessCode,
                Reference = entity.Reference,
                Location = entity.Location,
                CooperativeId = entity.CooperativeId,
                PaymentStatus = entity.PaymentStatus,
                SubscriptionStatus=entity.SubscriptionStatus,
                
               MaturityDate=entity.MaturityDate
            };
        }

        public static BundleSubscriptionInterest Map(this BundleSubscriptionInterestModel model)
        {
            if (model == null)
            {
                return null;
            }
            return new BundleSubscriptionInterest
            {
                PaymentStatus = Enum.GetName(typeof(EquityPaymentStatus), model.Status),
                SubscriptionStatus = Enum.GetName(typeof(InputFinancingStatus), model.EquitySubscriptionStatus),
                OfficerInChargeUserId = model.OfficerInChargeUserId,
                CreatedOn = model.OnBoardedOn,
                CommissionPercentage = model.CommissionPercentage,
                CommissionPrice = model.CommissionPrice,
                EquityPlusCommission = model.EquityPlusCommission,
                ShouldAddCommission = model.ShouldAddCommission,
                EquityPaymentBalanceDeficit = model.EquityPaymentBalanceDeficit,
                ExpectedEquityPricePerBundle = model.ExpectedEquityPricePerBundle,
                ExpectedTotalEquityPrice = model.ExpectedTotalEquityPrice,
                FarmerName = model.FarmerName,
                PaymentDate = model.PaymentDate,
                FarmerUserId = model.FarmerUserId,
                InterestedBundleId = model.InterestedBundleId,
                InterestedBundleName = model.InterestedBundleName,
                IsCommissionPaidOut = model.IsCommissionPaidOut,
                QuantityOfInterest = model.QuantityOfInterest,
                TotalEquityPaid = model.TotalEquityPaid,
                TotalQuantityPaidFor = model.TotalQuantityPaidFor,
                BelongToBusinessCode = model.BelongToBusinessCode,
                Reference = model.Reference,
                Location = model.Location,
                CooperativeId = model.CooperativeId,
                MaturityDate=model.MaturityDate
            };
        }

        public static BundleDisbursementModel Map(this BundleSubscriptionDisbursement entity)
        {
            if (entity == null)
            {
                return null;
            }
            return new BundleDisbursementModel
            {
                OfficerInChargeUserId = entity.OfficerInChargeUserId,
                DisbursedBy = entity.DisbursedBy,
                DisbursedOn = entity.DisbursedOn,
                InputAlreadyCollected = entity.InputAlreadyCollected,
                InputCollectedByPictureOf = entity.InputCollectedByPictureOf,
                // Status = entity.PaymentStatus.ToEnum<EquityPaymentStatus>(),           
                FarmerName = entity.FarmerName,
                FarmerUserId = entity.FarmerUserId,
                InterestedBundleId = entity.InterestedBundleId,
                InterestedBundleName = entity.InterestedBundleName,
                Id = entity.Id,
                TotalQuantityPaidFor = entity.TotalQuantityPaidFor,
                BelongToBusinessCode = entity.BelongToBusinessCode,
                Reference = entity.Reference,
                Location = entity.Location,
                CooperativeId = entity.CooperativeId,
            };
        }

        public static BundleSubscriptionDisbursement Map(this BundleDisbursementModel model)
        {
            if (model == null)
            {
                return null;
            }
            return new BundleSubscriptionDisbursement
            {

                OfficerInChargeUserId = model.OfficerInChargeUserId,
                DisbursedBy = model.DisbursedBy,
                DisbursedOn = model.DisbursedOn,
                InputAlreadyCollected = model.InputAlreadyCollected,
                InputCollectedByPictureOf = model.InputCollectedByPictureOf,
                // Status = entity.PaymentStatus.ToEnum<EquityPaymentStatus>(),           
                FarmerName = model.FarmerName,
                FarmerUserId = model.FarmerUserId,
                InterestedBundleId = model.InterestedBundleId,
                InterestedBundleName = model.InterestedBundleName,

                TotalQuantityPaidFor = model.TotalQuantityPaidFor,
                BelongToBusinessCode = model.BelongToBusinessCode,
                Reference = model.Reference,
                Location = model.Location,
                CooperativeId = model.CooperativeId,
            };
        }




        public static BundleModel Map(this Bundle entity)
        {
            if (entity == null)
            {
                return null;
            }
            return new BundleModel
            {
                Description = entity.Description,
                TotalBundlePackageDisbursed = entity.TotalBundlePackageDisbursed,
                BundlePrice = entity.BundlePrice,
                Id = entity.Id,
                Name = entity.Name,
                TotalBundlePackagePacked = entity.TotalBundlePackagePacked,
                TotalBundlePackageRequested = entity.TotalBundlePackageRequested,
                AvailableBundlePackagePacked = entity.AvailableBundlePackagePacked,
                BelongToBusinessCode = entity.BelongToBusinessCode,
                CreatedBy = entity.CreatedBy,
                OnBoardedOn = entity.CreatedOn,
                BundlePackage = entity.BundlePackage.Select(x => x.Map()).ToList(),
                BundleEquityPlusCommission = entity.BundleEquityPlusCommission,
                CommissionPrice = entity.CommissionPrice,
                ShouldAddCommission = entity.ShouldAddCommission,
                BundleEquityPrice = entity.BundleEquityPrice,
                WarehouseId = entity.WarehouseId,
                OtherBundlePaymentCosts= entity.OtherBundlePaymentCosts.Select(x => x.Map()).ToList(),
                MaturityDate=entity.MaturityDate
            };
        }

        public static Bundle Map(this BundleModel model)
        {
            if (model == null)
            {
                return null;
            }
            return new Bundle
            {
                Description = model.Description,
                TotalBundlePackageDisbursed = model.TotalBundlePackageDisbursed,
                BundlePrice = model.BundlePrice,
                BelongToBusinessCode = model.BelongToBusinessCode,
                CreatedBy = model.CreatedBy,
                CreatedOn = model.OnBoardedOn,
                BundlePackage = model.BundlePackage.Select(x => x.Map()).ToList(),
                OtherBundlePaymentCosts = model.OtherBundlePaymentCosts.Select(x => x.Map()).ToList(),
                Name = model.Name,
                TotalBundlePackagePacked = model.TotalBundlePackagePacked,
                TotalBundlePackageRequested = model.TotalBundlePackageRequested,
                AvailableBundlePackagePacked = model.AvailableBundlePackagePacked,
                BundleEquityPlusCommission = model.BundleEquityPlusCommission,
                CommissionPrice = model.CommissionPrice,
                ShouldAddCommission = model.ShouldAddCommission,
                BundleEquityPrice = model.BundleEquityPrice,
                WarehouseId = model.WarehouseId,
                MaturityDate= model.MaturityDate
            };
        }
        public static BundleItemsModel Map(this BundleItems entity)
        {
            if (entity == null)
            {
                return null;
            }
            return new BundleItemsModel
            {
                BundleId = entity.BundleId,
                Id = entity.Id,
                Name = entity.Name,
                Unit = entity.Unit,
                WarehouseInputInventoryId = entity.WarehouseInputInventoryId,
            };
        }

        public static BundleItems Map(this BundleItemsModel model)
        {
            if (model == null)
            {
                return null;
            }
            return new BundleItems
            {
                BundleId = model.BundleId,
                Name = model.Name,
                Unit = model.Unit,
                WarehouseInputInventoryId = model.WarehouseInputInventoryId,
            };
        }
        public static OtherBundlePaymentCostModel Map(this OtherBundlePaymentCost entity)
        {
            if (entity == null)
            {
                return null;
            }
            return new OtherBundlePaymentCostModel
            {
                BundleId = entity.BundleId,
                Id = entity.Id,
                Name = entity.Name,Amount = entity.Amount,
                Description = entity.Description,
            };
        }

        public static OtherBundlePaymentCost Map(this OtherBundlePaymentCostModel model)
        {
            if (model == null)
            {
                return null;
            }
            return new OtherBundlePaymentCost
            {
                BundleId = model.BundleId,
                Name = model.Name,
                Amount = model.Amount,
                Description= model.Description,
            };
        }
        #endregion Bundle

        #region warehouse

        public static WarehouseUserModel Map(this WarehouseUser entity)
        {
            if (entity == null)
            {
                return null;
            }
            return new WarehouseUserModel
            {
                State = entity.State,
                BelongToBusinessCode = entity.BelongToBusinessCode,
                Id = entity.Id,
                Lga = entity.Lga,
                Location = entity.Location,
                Name = entity.Name,
                PhoneNumber = entity.PhoneNumber,
                UserId = entity.UserId,
                WarehouseId = entity.WarehouseId
            };
        }

        public static WarehouseUser Map(this WarehouseUserModel model)
        {
            if (model == null)
            {
                return null;
            }
            return new WarehouseUser
            {
                State = model.State,
                BelongToBusinessCode = model.BelongToBusinessCode,
                Lga = model.Lga,
                Location = model.Location,
                Name = model.Name,
                PhoneNumber = model.PhoneNumber,
                UserId = model.UserId,
                WarehouseId = model.WarehouseId
            };
        }
        public static WarehouseInputInventoryModel Map(this WarehouseInputInventory entity)
        {
            if (entity == null)
            {
                return null;
            }
            return new WarehouseInputInventoryModel
            {
                IsAvailable = entity.IsAvailable,
                Size = entity.Size,
                InventoryStockedOn = entity.InventoryStockedOn,
                UnitStockedUp = entity.UnitStockedUp,
                Brand = entity.Brand,
                Id = entity.Id,
                InputDescription = entity.InputDescription,
                InputName = entity.InputName,
                ManagedByUserId = entity.ManagedByUserId,
                ModifiedOn = entity.ModifiedOn,
                Price = entity.Price,
                Type = entity.Type,
                UnitAdded = entity.UnitAdded,
                UnitBalance = entity.UnitBalance,
                UnitDepleted = entity.UnitDepleted,
                BelongToBusinessCode = entity.BelongToBusinessCode,
                WarehouseId = entity.WarehouseId,
                InputComingFrom = entity.InputComingFrom,
                Status = entity.TransitStatus.ToEnum<InputTransitStatus>(),
                DeliveredTo = entity.DeliveredTo,
                ReceivedBy = entity.ReceivedBy,
                Reference = entity.Reference,
                TransitStatus = entity.TransitStatus
            };
        }

        public static WarehouseInputInventory Map(this WarehouseInputInventoryModel model)
        {
            if (model == null)
            {
                return null;
            }
            return new WarehouseInputInventory
            {
                IsAvailable = model.IsAvailable,
                Size = model.Size,
                InventoryStockedOn = model.InventoryStockedOn,
                UnitStockedUp = model.UnitStockedUp,
                Brand = model.Brand,
                InputDescription = model.InputDescription,
                InputName = model.InputName,
                ManagedByUserId = model.ManagedByUserId,
                ModifiedOn = model.ModifiedOn,
                Price = model.Price,
                Type = model.Type,
                UnitAdded = model.UnitAdded,
                UnitBalance = model.UnitBalance,
                UnitDepleted = model.UnitDepleted,
                BelongToBusinessCode = model.BelongToBusinessCode,
                WarehouseId = model.WarehouseId,
                InputComingFrom = model.InputComingFrom,
                TransitStatus = Enum.GetName(typeof(InputTransitStatus), model.Status),
                DeliveredTo = model.DeliveredTo,
                ReceivedBy = model.ReceivedBy,
                Reference = model.Reference
            };
        }

        public static Warehouse Map(this WarehouseModel model)
        {
            if (model == null)
            {
                return null;
            }
            return new Warehouse
            {
                HasbeenAssignedManager = model.HasbeenAssignedManager,
                BelongToBusinessCode = model.BelongToBusinessCode,
                CreatedOn = model.CreatedOn,
                Lga = model.Lga,
                State = model.State,
                Location = model.Location,
                ManagedByUserId = model.ManagedByUserId,
                Name = model.Name,
                Capacity = model.Capacity,
            };
        }

        public static WarehouseModel Map(this Warehouse entity)
        {
            if (entity == null)
            {
                return null;
            }
            return new WarehouseModel
            {
                HasbeenAssignedManager = entity.HasbeenAssignedManager,
                BelongToBusinessCode = entity.BelongToBusinessCode,
                CreatedOn = entity.CreatedOn,
                Lga = entity.Lga,
                State = entity.State,
                Location = entity.Location,
                ManagedByUserId = entity.ManagedByUserId,
                Name = entity.Name,
                Id = entity.Id,
                Capacity = entity.Capacity
            };
        }

        public static InputDeliveryTrackerModel Map(this InputDeliveryTracker entity)
        {
            if (entity == null)
            {
                return null;
            }
            return new InputDeliveryTrackerModel
            {
                BusinessCode = entity.BusinessCode,
                Id = entity.Id,
                WarehouseId = entity.WarehouseId,
                Status = entity.Status.ToEnum<InputTransitStatus>(),
                DeliveredTo = entity.DeliveredTo,
                Reference = entity.Reference,
                WarehouseInputInventoryId = entity.WarehouseInputInventoryId,
                DeliveredBy = entity.DeliveredBy,
                DeliveryContact = entity.DeliveryContact,
                DeliveryDate = entity.DeliveryDate,
                DeliveryLocation = entity.DeliveryLocation,
                PickUpDate = entity.PickUpDate,
                QuantityDelivered = entity.QuantityDelivered,
                InputName = entity.InputName,
                LogisticNumber = entity.LogisticNumber,
                LogisticType = entity.LogisticType.ToEnum<LogisticType>(),
                PickupLocation = entity.PickupLocation,
                QuantityLoaded = entity.QuantityLoaded,
                QuantityReceived = entity.QuantityReceived,
                TransitStatus = entity.Status,//Enum.GetName(typeof(InputTransitStatus), entity.Status),
            };
        }

        public static InputDeliveryTracker Map(this InputDeliveryTrackerModel model)
        {
            if (model == null)
            {
                return null;
            }
            return new InputDeliveryTracker
            {
                BusinessCode = model.BusinessCode,

                WarehouseId = model.WarehouseId,
                Status = Enum.GetName(typeof(InputTransitStatus), model.Status),
                DeliveredTo = model.DeliveredTo,
                Reference = model.Reference,
                WarehouseInputInventoryId = model.WarehouseInputInventoryId,
                DeliveredBy = model.DeliveredBy,
                DeliveryContact = model.DeliveryContact,
                DeliveryDate = model.DeliveryDate,
                DeliveryLocation = model.DeliveryLocation,
                PickUpDate = model.PickUpDate,
                QuantityDelivered = model.QuantityDelivered,
                InputName = model.InputName,
                LogisticNumber = model.LogisticNumber,
                LogisticType = Enum.GetName(typeof(LogisticType), model.LogisticType),

                PickupLocation = model.PickupLocation,
                QuantityLoaded = model.QuantityLoaded,
                QuantityReceived = model.QuantityReceived,

            };
        }




        #endregion warehouse
        #region userMgt
        public static FarmerImagesModel Map(this FarmerImages entities)
        {
            if (entities == null) { return null; }
            return new FarmerImagesModel
            {
                BelongToBusinessCode = entities.BelongToBusinessCode,
                FarmerUserId = entities.FarmerUserId,
                Id = entities.Id,
                ImageName = entities.ImageName,
                ImageKey = entities.IMageKey,
            };
        }
        public static FarmerImages Map(this FarmerImagesModel model)
        {
            if (model == null) { return null; }
            return new FarmerImages
            {
                BelongToBusinessCode = model.BelongToBusinessCode,
                FarmerUserId = model.FarmerUserId,
                ImageName = model.ImageName,
                IMageKey = model.ImageKey,
            };
        }
        public static FarmerModel Map(this Farmer entities)
        {
            if (entities == null) { return null; }
            return new FarmerModel
            {
                Id = entities.Id,
                Location = entities.Location,
                Name = entities.Name,
                Email = entities.Email,
                PhoneNumber = entities.PhoneNumber,
                UserId = entities.UserId,
                OnBoardedOn = entities.OnBoardedOn,

                DateOfBirth = entities.DateOfBirth,
                Gender = entities.Gender,
                BelongToBusinessCode = entities.BelongToBusinessCode,
                State = entities.State,
                Lga = entities.Lga,
                OnboardedByUserId = entities.OnboardedByUserId,
                BelongToCooperativeId = entities.BelongToCooperativeId
            };
        }

        public static Farmer Map(this FarmerModel model)
        {
            if (model == null) { return null; }
            return new Farmer
            {
                //Id = model.Id,
                Location = model.Location,
                Name = model.Name,
                Email = model.Email,
                UserId = model.UserId,
                OnBoardedOn = model.OnBoardedOn,
                BelongToBusinessCode = model.BelongToBusinessCode,
                State = model.State,
                Lga = model.Lga,
                OnboardedByUserId = model.OnboardedByUserId,
                PhoneNumber = model.PhoneNumber,
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                BelongToCooperativeId = model.BelongToCooperativeId
            };
        }

        public static CooperativeModel Map(this Cooperative entities)
        {
            if (entities == null) { return null; }
            return new CooperativeModel
            {
                Id = entities.Id,
                Location = entities.Location,
                Name = entities.Name,
                Email = entities.Email,
                PhoneNumber = entities.ContactPerson,
                UserId = entities.UserId,
                OnBoardedOn = entities.OnBoardedOn,
                State = entities.State,
                Lga = entities.Lga,
                BelongToBusinessCode = entities.BelongToBusinessCode,
                ContactPerson = entities.ContactPerson,
                IsAssignedToAgentId = entities.IsAssignedToAgentId,
                HasbeenAssignedAgent = entities.HasbeenAssignedAgent
            };
        }

        public static Cooperative Map(this CooperativeModel model)
        {
            if (model == null) { return null; }
            return new Cooperative
            {
                // Id = model.Id,
                Location = model.Location,
                Name = model.Name,
                Email = model.Email,
                ContactPerson = model.PhoneNumber,
                UserId = model.UserId,
                State = model.State,
                Lga = model.Lga,
                BelongToBusinessCode = model.BelongToBusinessCode
                ,
                IsAssignedToAgentId = model.IsAssignedToAgentId,
                PhoneNumber = model.PhoneNumber,
                OnBoardedOn = model.OnBoardedOn,
                HasbeenAssignedAgent = model.HasbeenAssignedAgent
            };
        }


        public static AgentModel Map(this Agent entities)
        {
            if (entities == null) { return null; }
            return new AgentModel
            {
                Id = entities.Id,
                Location = entities.Location,
                Name = entities.Name,
                Email = entities.Email,
                PhoneNumber = entities.PhoneNumber,
                UserId = entities.UserId,
                OnBoardedOn = entities.OnBoardedOn,
                State = entities.State,
                Lga = entities.Lga,
                BelongToBusinessCode = entities.BelongToBusinessCode,
                SupervisorUserId = entities.SupervisorUserId
            };
        }

        public static Agent Map(this AgentModel model)
        {
            if (model == null) { return null; }
            return new Agent
            {
                // Id = model.Id,
                Location = model.Location,
                Name = model.Name,
                Email = model.Email,
                UserId = model.UserId,
                State = model.State,
                Lga = model.Lga,
                BelongToBusinessCode = model.BelongToBusinessCode
                ,
                PhoneNumber = model.PhoneNumber,
                OnBoardedOn = model.OnBoardedOn,
                SupervisorUserId = model.SupervisorUserId
            };
        }


        #endregion userMgt

        #region walletProvider


        public static SubAccount Map(this SubAccountModel model)
        {
            if (model == null)
            {
                return null;
            }
            return new SubAccount()
            {
                DOB = model.DOB,
                Status = model.Status,
                SubAccountName = model.SubAccountName,
                BankAccount = model.BankAccount,
                BankName = model.BankName,
                BusinessId = model.BusinessId,
                Bvn = model.Bvn,
                CreatedBy = model.CreatedBy,
                CreatedOn = model.OnBoardedOn,
                IsMainAccount = model.IsMainAccount,
                NIN = model.NIN,
                PhoneNumber = model.PhoneNumber,
                UserId = model.UserId,
                WalletCode = model.WalletCode,
                FirstName = model.FirstName,
                LastName = model.LastName,
                MiddleName= model.MiddleName,
                SubAccountType = Enum.GetName(typeof(SubAccountType), model.SubAccountType)

            };
        }

        public static SubAccountModel Map(this SubAccount entity)
        {
            if (entity == null)
            {
                return null;
            }
            return new SubAccountModel()
            {
                DOB = entity.DOB,
                Status = entity.Status,
                SubAccountId = entity.SubAccountId,
                SubAccountName = entity.SubAccountName,
                BankAccount = entity.BankAccount,
                BankName = entity.BankName,
                BusinessId = entity.BusinessId,
                Bvn = entity.Bvn,
                CreatedBy = entity.CreatedBy,
                OnBoardedOn = entity.CreatedOn,
                IsMainAccount = entity.IsMainAccount,
                NIN = entity.NIN,
                PhoneNumber = entity.PhoneNumber,
                UserId = entity.UserId,
                WalletCode = entity.WalletCode,
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                MiddleName= entity.MiddleName,
                SubAccountType = entity.SubAccountType.ToEnum<SubAccountType>(),
            };
        }

        public static SterlingBankWallet Map(this SterlingBankWalletModel model)
        {
            if (model == null)
            {
                return null;
            }
            return new SterlingBankWallet()
            {
                WalletId = model.WalletId,
                AccountNumber = model.AccountNumber,
                AgrovestoUserId = model.AgrovestoUserId,
                CreatedOn = model.CreatedOn,
                InternalAccountId = model.InternalAccountId,
                IsABusinessAccount = model.IsABusinessAccount,
                LedgerAccountNumber = model.LedgerAccountNumber,
                OrganizationId = model.OrganizationId,
                OwnerId = model.OwnerId,
                UserId = model.UserId,
                CustomerId = model.CustomerId,
                AccountName = model.AccountName,
                BankCode = model.BankCode,
                BankName = model.BankName,
                currencyId = model.currencyId,
                customerTypeId = model.customerTypeId,
                walletClassificationId = model.walletClassificationId,

            };
        }

        public static SterlingBankWalletModel Map(this SterlingBankWallet entity)
        {
            if (entity == null)
            {
                return null;
            }
            return new SterlingBankWalletModel()
            {
                WalletId = entity.WalletId,
                AccountNumber = entity.AccountNumber,
                AgrovestoUserId = entity.AgrovestoUserId,
                CreatedOn = entity.CreatedOn,
                InternalAccountId = entity.InternalAccountId,
                IsABusinessAccount = entity.IsABusinessAccount,
                LedgerAccountNumber = entity.LedgerAccountNumber,
                OrganizationId = entity.OrganizationId,
                OwnerId = entity.OwnerId,
                UserId = entity.UserId,
                CustomerId = entity.CustomerId,
                AccountName = entity.AccountName,
                BankCode = entity.BankCode,
                BankName = entity.BankName,
                currencyId = entity.currencyId,
                customerTypeId = entity.customerTypeId,
                walletClassificationId = entity.walletClassificationId,
                Id = entity.Id,
            };
        }


        public static WalletProvider Map(this WalletProviderModel model)
        {
            if (model == null)
            {
                return null;
            }
            return new WalletProvider()
            {
                Code = model.Code,
                Name = model.Name,
                ProviderId = model.ProviderId,
                WalletProviderParameters = model.WalletProviderParameters.Select(x => x.Map()).ToList(),
                Status = model.Status.ToString()
            };
        }

        public static WalletProviderModel Map(this WalletProvider entity)
        {
            if (entity == null)
            {
                return null;
            }
            return new WalletProviderModel()
            {
                Code = entity.Code,
                Name = entity.Name,
                ProviderId = entity.ProviderId,
                WalletProviderParameters = entity.WalletProviderParameters.Select(x => x.Map()).ToList(),
                Status = entity.Status.ToEnum<ProviderStatus>()
            };
        }
        public static WalletProviderParameter Map(this WalletProviderParameterModel model)
        {
            return new WalletProviderParameter()
            {
                Description = model.Description,
                Key = model.Key,
                ParameterId = model.ParameterId,
                WalletProviderProviderId = model.ProviderId,
                Value = model.Value,
                Env = Enum.GetName(typeof(Env), model.Env)//model.Env.ToString()
            };
        }
        public static WalletProviderParameterModel Map(this WalletProviderParameter model)
        {
            return new WalletProviderParameterModel()
            {
                Description = model.Description,
                Key = model.Key,
                ParameterId = model.ParameterId,
                ProviderId = model.WalletProviderProviderId,
                Value = model.Value,
                Env = model.Env.ToEnum<Env>()
            };
        }


        #endregion walletProvider
        #region Business
        public static Business Map(this BusinessModel model)
        {
            if (model == null)
            {
                return null;
            }

            return new Business
            {

                Address = model.Address,

                BusinessCode = model.BusinessCode,
                BusinessEmail = model.BusinessEmail,
                BusinessName = model.BusinessName,
                BusinessStatus = model.BusinessStatus.ToString(),
                ContactEmail = model.ContactEmail,
                DisputeEmail = model.DisputeEmail,
                PhoneNumber = model.PhoneNumber,
                SupportEmail = model.SupportEmail,
                BusinessId = model.BusinessId,
                Category = model.Category.ToString(),
                Website = model.Website,
                BVN = model.BVN,
                NIN = model.NIN,
                BusinessLogo = model.BusinessLogo,
                KycCategory = Enum.GetName(typeof(Category), model.Category),
                BusinessCodeSignage = model.BusinessCodeSignage,
                BusinessCodeSignageSalt = model.BusinessCodeSignageSalt
            };
        }

        public static BusinessLoadModel ViewMap(this BusinessModel model)
        {
            if (model == null)
            {
                return null;
            }

            return new BusinessLoadModel
            {

                Address = model.Address,

                BusinessCode = model.BusinessCode,
                BusinessEmail = model.BusinessEmail,
                BusinessName = model.BusinessName,

                PhoneNumber = model.PhoneNumber,
                Website = model.Website,

                BusinessLogo = model.BusinessLogo,

            };
        }



        public static BusinessModel Map(this Business entity)
        {
            if (entity == null)
            {
                return null;
            }

            return new BusinessModel
            {
                BusinessId = entity.BusinessId,
                Address = entity.Address,
                BusinessCode = entity.BusinessCode,
                BusinessEmail = entity.BusinessEmail,
                BusinessName = entity.BusinessName,
                BusinessStatus = entity.BusinessStatus.ToEnum<BusinessStatus>(),
                ContactEmail = entity.ContactEmail,
                DisputeEmail = entity.DisputeEmail,
                PhoneNumber = entity.PhoneNumber,
                Category = entity.Category.ToEnum<Category>(),
                SupportEmail = entity.SupportEmail,
                Website = entity.Website,
                BVN = entity.BVN,
                BusinessLogo = entity.BusinessLogo,
                NIN = entity.NIN,
                UserId = entity.UserId,
                BusinessCodeSignage = entity.BusinessCodeSignage,
                BusinessCodeSignageSalt = entity.BusinessCodeSignageSalt
            };
        }
        public static BusinessWorker Map(this BusinessWorkerModel model)
        {
            if (model == null)
            {
                return null;
            }

            return new BusinessWorker
            {
                BusinessId = model.BusinessId,
                UserId = model.UserId,
                BusinessWorkerId = model.BusinessWorkerId,
            };
        }

        public static BusinessWorkerModel Map(this BusinessWorker entity)
        {
            if (entity == null)
            {
                return null;
            }

            return new BusinessWorkerModel
            {

                BusinessWorkerId = entity.BusinessWorkerId,
                BusinessId = entity.BusinessId,
                UserId = entity.UserId,
            };
        }



        #endregion Business
        public static TraderModel Map(this Trader entities)
        {
            if (entities == null) { return null; }
            return new TraderModel
            {
                Id = entities.Id,
                Location = entities.Location,
                Name = entities.Name,
                Email = entities.Email,
                PhoneNumber = entities.PhoneNumber,
                UserId = entities.UserId,
                OnBoardedOn = entities.OnBoardedOn,
                State = entities.State,
                Lga = entities.Lga,
                BelongToBusinessCode = entities.BelongToBusinessCode,

                //  SupervisorUserId = entities.SupervisorUserId
            };
        }

        public static Trader Map(this TraderModel model)
        {
            if (model == null) { return null; }
            return new Trader
            {
                // Id = model.Id,
                Location = model.Location,
                Name = model.Name,
                Email = model.Email,
                UserId = model.UserId,
                State = model.State,
                Lga = model.Lga,
                BelongToBusinessCode = model.BelongToBusinessCode
                ,
                PhoneNumber = model.PhoneNumber,
                OnBoardedOn = model.OnBoardedOn,
                //SupervisorUserId = model.SupervisorUserId
            };
        }



        #region FarmerSalesMgt
        public static FarmerSalesTracker Map(this FarmerSalesTrackerModel model)
        {
            if (model == null)
            {
                return null;
            }
            return new FarmerSalesTracker()
            {
                CropPricePerUnitSold = model.CropPricePerUnitSold,
                CollectionDate = model.CollectionDate,
                CropSold = model.CropSold,
                CollectionOfficerId = model.CollectionOfficerId,
                CostOfPackagingMaterial = model.CostOfPackagingMaterial,
                FarmerUserId = model.FarmerUserId,
                IntendedPriceToSellPerUnit = model.IntendedPriceToSellPerUnit,
                IsCropPackaged = model.IsCropPackaged,
                IsCropTransportedToMarket = model.IsCropTransportedToMarket,
                MarketState = model.MarketState,
                QuantitySoldToday = model.QuantitySoldToday,
                TotalPriceSold = model.TotalPriceSold,
                TotalTimeSpentInMarket = model.TotalTimeSpentInMarket,
                MarketLga = model.MarketLga,
                MarketLocation = model.MarketLocation,
                MarketName = model.MarketName,
                MeansOfTransport = model.MeansOfTransport,
                ModeOfPayment = model.ModeOfPayment,
                QuantityBroughtToMarket = model.QuantityBroughtToMarket,
                OtherCost = model.OtherCost,
                PackagingType = model.PackagingType,
                TransportCost = model.TransportCost,
                UnitType = Enum.GetName(typeof(UnitType), model.UnitType),
                TransactionActualDate = model.TransactionActualDate,
                SalesType = Enum.GetName(typeof(SalesType), model.SalesType),
                BelongToBusinessCode = model.BelongToBusinessCode,
                supervisorId = model.supervisorId,
                MarketDay = model.MarketDay,
                SoldCropUnitType = Enum.GetName(typeof(UnitType), model.SoldCropUnitType),
                PriceOfLowestUnit = model.PriceOfLowestUnit,
                LowestUnitQuntity = model.LowestUnitQuntity
            };
        }

        public static FarmerSalesTrackerModel Map(this FarmerSalesTracker entity)
        {
            if (entity == null)
            {
                return null;
            }
            return new FarmerSalesTrackerModel()
            {
                SalesTypeName = entity.SalesType,
                SoldCropUnitTypeName = entity.SoldCropUnitType,
                UnitTypeName = entity.UnitType,
                Id = entity.Id,
                CropPricePerUnitSold = entity.CropPricePerUnitSold,
                CollectionDate = entity.CollectionDate,
                CropSold = entity.CropSold,
                CollectionOfficerId = entity.CollectionOfficerId,
                CostOfPackagingMaterial = entity.CostOfPackagingMaterial,
                FarmerUserId = entity.FarmerUserId,
                IntendedPriceToSellPerUnit = entity.IntendedPriceToSellPerUnit,
                IsCropPackaged = entity.IsCropPackaged,
                IsCropTransportedToMarket = entity.IsCropTransportedToMarket,
                MarketState = entity.MarketState,
                QuantitySoldToday = entity.QuantitySoldToday,
                TotalPriceSold = entity.TotalPriceSold,
                TotalTimeSpentInMarket = entity.TotalTimeSpentInMarket,
                MarketLga = entity.MarketLga,
                MarketLocation = entity.MarketLocation,
                MarketName = entity.MarketName,
                MeansOfTransport = entity.MeansOfTransport,
                ModeOfPayment = entity.ModeOfPayment,
                QuantityBroughtToMarket = entity.QuantityBroughtToMarket,
                OtherCost = entity.OtherCost,
                PackagingType = entity.PackagingType,
                TransportCost = entity.TransportCost,
                //UnitType= entity.UnitType.ToEnum<UnitType>(),
                supervisorId = entity.supervisorId,
                TransactionActualDate = entity.TransactionActualDate,
                //SalesType = entity.SalesType.ToEnum<SalesType>(),
                BelongToBusinessCode = entity.BelongToBusinessCode,
                //UnitTypeName= Enum.GetName(typeof(UnitType), entity.UnitType),
                MarketDay = entity.MarketDay,
                PriceOfLowestUnit = entity.PriceOfLowestUnit,
                LowestUnitQuntity = entity.LowestUnitQuntity
                //  SoldCropUnitType = entity.SoldCropUnitType,
            };
        }


        #endregion FarmerSalesMgt
        #region RoleMgt
        public static Role Map(this RoleModel model)
        {
            if (model == null)
            {
                return null;
            }

            return new Role
            {
                Descriptions = model.RoleDescription,
                Name = model.RoleName,
                IsAvailableToBusiness = model.IsAvailableToBusiness
            };
        }

        public static RoleModel Map(this Role entity)
        {
            if (entity == null)
            {
                return null;
            }

            return new RoleModel
            {
                RoleId = entity.Id,
                RoleDescription = entity.Descriptions,
                RoleName = entity.Name,
                IsAvailableToBusiness = entity.IsAvailableToBusiness
            };
        }
        #endregion RoleMgt

        #region Admin Mgt
        public static UserViewModel ViewMap(this UserModel model)
        {
            if (model == null)
            {
                return null;
            }

            return new UserViewModel
            {


                CreatedBy = model.CreatedBy,
                CreatedOn = model.CreatedOn,
                Email = model.Email,
                LastLoginDate = model.LastLoginDate,
                PhoneNumber = model.PhoneNumber,
                Name = model.FirstName,
                UserType = model.UserType,
                BusinessCode = model.BusinessCode,
                //Gender=model.Gender,
                //DateOfBirth=model.DateOfBirth,
                Id = model.Id,
                IsActive = model.IsActive,
                //Lga=model.Lga,
                //Location=model.Location,
                //State=model.State
            };
        }
        public static User Map(this UserModel model)
        {
            if (model == null)
            {
                return null;
            }

            return new User
            {
                OTP = model.OTP,
                Salt = model.Salt,
                CreatedBy = model.CreatedBy,
                CreatedOn = model.CreatedOn,
                DateModified = model.DateModified,
                Email = model.Email,
                LastLoginDate = model.LastLoginDate,
                ModifiedBy = model.ModifiedBy,
                Password = model.Password,
                PhoneNumber = model.PhoneNumber,
                Pin = model.Pin,
                UserName = model.Email,
                Name = model.FirstName,
                UserType = model.UserType,
                BusinessCode = model.BusinessCode
                //UserType=model.UserType.ToString()
                // UserType = Enum.GetName(typeof(UserType), model.UserType)
            };
        }

        public static UserModel Map(this User entity)
        {
            if (entity == null)
            {
                return null;
            }

            return new UserModel
            {
                OTP = entity.OTP,
                Salt = entity.Salt,
                CreatedBy = entity.CreatedBy,
                CreatedOn = entity.CreatedOn,
                DateModified = entity.DateModified,
                Email = entity.Email,
                LastLoginDate = entity.LastLoginDate,
                ModifiedBy = entity.ModifiedBy,
                Password = entity.Password,
                PhoneNumber = entity.PhoneNumber,
                Pin = entity.Pin,
                UserType = entity.UserType,
                //UserType = Enum.GetName(typeof(UserType), entity.UserType),//entity.UserType.ToEnum<UserType>(),
                IsActive = entity.IsActive,
                Id = entity.Id,
                FirstName = entity.Name,
                BusinessCode = entity.BusinessCode
            };
        }
        #endregion Mgt

    }
}
