﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Affiliates;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Services;
using Nop.Services.Affiliates;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Web.Areas.Admin.Extensions;
using Nop.Web.Areas.Admin.Models.Affiliates;
using Nop.Web.Areas.Admin.Models.Common;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Factories
{
    /// <summary>
    /// Represents the affiliate model factory implementation
    /// </summary>
    public partial class AffiliateModelFactory : IAffiliateModelFactory
    {
        #region Fields

        private readonly IAffiliateService _affiliateService;
        private readonly ICountryService _countryService;
        private readonly ICustomerService _customerService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly ILocalizationService _localizationService;
        private readonly IOrderService _orderService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IWebHelper _webHelper;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public AffiliateModelFactory(IAffiliateService affiliateService,
            ICountryService countryService,
            ICustomerService customerService,
            IDateTimeHelper dateTimeHelper,
            ILocalizationService localizationService,
            IOrderService orderService,
            IPriceFormatter priceFormatter,
            IStateProvinceService stateProvinceService,
            IWebHelper webHelper,
            IWorkContext workContext)
        {
            this._affiliateService = affiliateService;
            this._countryService = countryService;
            this._customerService = customerService;
            this._dateTimeHelper = dateTimeHelper;
            this._localizationService = localizationService;
            this._orderService = orderService;
            this._priceFormatter = priceFormatter;
            this._stateProvinceService = stateProvinceService;
            this._webHelper = webHelper;
            this._workContext = workContext;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Prepare address model
        /// </summary>
        /// <param name="model">Address model</param>
        /// <param name="address">Address</param>
        /// <returns>Address model</returns>
        protected virtual AddressModel PrepareAddressModel(AddressModel model, Address address)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            //set all address fields as enabled and required
            model.FirstNameEnabled = true;
            model.FirstNameRequired = true;
            model.LastNameEnabled = true;
            model.LastNameRequired = true;
            model.EmailEnabled = true;
            model.EmailRequired = true;
            model.CompanyEnabled = true;
            model.CountryEnabled = true;
            model.CountryRequired = true;
            model.StateProvinceEnabled = true;
            model.CountyEnabled = true;
            model.CountyRequired = true;
            model.CityEnabled = true;
            model.CityRequired = true;
            model.StreetAddressEnabled = true;
            model.StreetAddressRequired = true;
            model.StreetAddress2Enabled = true;
            model.ZipPostalCodeEnabled = true;
            model.ZipPostalCodeRequired = true;
            model.PhoneEnabled = true;
            model.PhoneRequired = true;
            model.FaxEnabled = true;

            //prepare available countries
            var availableCountries = _countryService.GetAllCountries(showHidden: true);
            model.AvailableCountries = availableCountries
                .Select(country => new SelectListItem { Text = country.Name, Value = country.Id.ToString() }).ToList();

            //insert special country item for the "select" value
            model.AvailableCountries
                .Insert(0, new SelectListItem { Text = _localizationService.GetResource("Admin.Address.SelectCountry"), Value = "0" });

            //prepare available states
            var states = model.CountryId.HasValue
                ? _stateProvinceService.GetStateProvincesByCountryId(model.CountryId.Value, showHidden: true)
                : new List<StateProvince>();
            model.AvailableStates = states.Select(state => new SelectListItem { Text = state.Name, Value = state.Id.ToString() }).ToList();

            //insert special state item for the "non US" value
            if (!states.Any())
            {
                model.AvailableStates
                    .Insert(0, new SelectListItem { Text = _localizationService.GetResource("Admin.Address.OtherNonUS"), Value = "0" });
            }

            return model;
        }

        /// <summary>
        /// Prepare affiliated order list model
        /// </summary>
        /// <param name="model">Affiliated order list model</param>
        /// <param name="affiliate">Affiliate</param>
        /// <returns>Affiliated order list model</returns>
        protected virtual AffiliateModel.AffiliatedOrderListModel PrepareAffiliatedOrderListModel(AffiliateModel.AffiliatedOrderListModel model,
            Affiliate affiliate)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (affiliate == null)
                throw new ArgumentNullException(nameof(affiliate));

            model.AffliateId = affiliate.Id;

            //prepare order, payment and shipping statuses
            model.AvailableOrderStatuses = OrderStatus.Pending.ToSelectList(false).ToList();
            model.AvailablePaymentStatuses = PaymentStatus.Pending.ToSelectList(false).ToList();
            model.AvailableShippingStatuses = ShippingStatus.NotYetShipped.ToSelectList(false).ToList();

            //insert special status item for the "all" value
            var allSelectListItem = new SelectListItem { Text = _localizationService.GetResource("Admin.Common.All"), Value = "0" };
            model.AvailableOrderStatuses.Insert(0, allSelectListItem);
            model.AvailablePaymentStatuses.Insert(0, allSelectListItem);
            model.AvailableShippingStatuses.Insert(0, allSelectListItem);

            return model;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Prepare affiliate list model
        /// </summary>
        /// <param name="model">Affiliate list model</param>
        /// <returns>Affiliate list model</returns>
        public virtual AffiliateListModel PrepareAffiliateListModel(AffiliateListModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            return model;
        }

        /// <summary>
        /// Prepare paged affiliate list model for the grid
        /// </summary>
        /// <param name="listModel">Affiliate list model</param>
        /// <param name="command">Pagination parameters</param>
        /// <returns>Grid model</returns>
        public virtual DataSourceResult PrepareAffiliateListGridModel(AffiliateListModel listModel, DataSourceRequest command)
        {
            if (listModel == null)
                throw new ArgumentNullException(nameof(listModel));

            if (command == null)
                throw new ArgumentNullException(nameof(command));

            //get affiliates
            var affiliates = _affiliateService.GetAllAffiliates(listModel.SearchFriendlyUrlName,
                listModel.SearchFirstName,
                listModel.SearchLastName,
                listModel.LoadOnlyWithOrders,
                listModel.OrdersCreatedFromUtc,
                listModel.OrdersCreatedToUtc,
                command.Page - 1, command.PageSize, true);

            //prepare grid model
            var model = new DataSourceResult
            {
                //fill in model values from the entity
                Data = affiliates.Select(affiliate => new AffiliateModel
                {
                    Id = affiliate.Id,
                    Active = affiliate.Active,
                    Address = affiliate.Address.ToModel()
                }),
                Total = affiliates.TotalCount
            };

            return model;
        }

        /// <summary>
        /// Prepare affiliate model
        /// </summary>
        /// <param name="model">Affiliate model</param>
        /// <param name="affiliate">Affiliate</param>
        /// <param name="excludeProperties">Whether to exclude populating of some properties of model</param>
        /// <returns>Affiliate model</returns>
        public virtual AffiliateModel PrepareAffiliateModel(AffiliateModel model, Affiliate affiliate, bool excludeProperties = false)
        {
            //fill in model values from the entity
            if (affiliate != null)
            {
                model = model ?? new AffiliateModel();
                model.Id = affiliate.Id;
                model.Url = affiliate.GenerateUrl(_webHelper);

                //prepare order list model
                model.AffiliatedOrderList = PrepareAffiliatedOrderListModel(model.AffiliatedOrderList, affiliate);

                //whether to fill in some of properties
                if (!excludeProperties)
                {
                    model.AdminComment = affiliate.AdminComment;
                    model.FriendlyUrlName = affiliate.FriendlyUrlName;
                    model.Active = affiliate.Active;
                    model.Address = affiliate.Address.ToModel();
                }
            }

            //prepare address model
            model.Address = PrepareAddressModel(model.Address, affiliate?.Address);

            return model;
        }

        /// <summary>
        /// Prepare paged affiliated order list model for the grid
        /// </summary>
        /// <param name="listModel">Affiliated order list model</param>
        /// <param name="command">Pagination parameters</param>
        /// <param name="affiliate">Affiliate</param>
        /// <returns>Grid model</returns>
        public virtual DataSourceResult PrepareAffiliatedOrderListGridModel(AffiliateModel.AffiliatedOrderListModel listModel,
            DataSourceRequest command, Affiliate affiliate)
        {
            if (listModel == null)
                throw new ArgumentNullException(nameof(listModel));

            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (affiliate == null)
                throw new ArgumentNullException(nameof(affiliate));

            //get parameters to filter orders
            var startDateValue = !listModel.StartDate.HasValue ? null
                : (DateTime?)_dateTimeHelper.ConvertToUtcTime(listModel.StartDate.Value, _dateTimeHelper.CurrentTimeZone);
            var endDateValue = !listModel.EndDate.HasValue ? null
                : (DateTime?)_dateTimeHelper.ConvertToUtcTime(listModel.EndDate.Value, _dateTimeHelper.CurrentTimeZone).AddDays(1);
            var orderStatusIds = listModel.OrderStatusId > 0 ? new List<int>() { listModel.OrderStatusId } : null;
            var paymentStatusIds = listModel.PaymentStatusId > 0 ? new List<int>() { listModel.PaymentStatusId } : null;
            var shippingStatusIds = listModel.ShippingStatusId > 0 ? new List<int>() { listModel.ShippingStatusId } : null;

            //get orders
            var orders = _orderService.SearchOrders(createdFromUtc: startDateValue,
                createdToUtc: endDateValue,
                osIds: orderStatusIds,
                psIds: paymentStatusIds,
                ssIds: shippingStatusIds,
                affiliateId: affiliate.Id,
                pageIndex: command.Page - 1, pageSize: command.PageSize);

            //prepare grid model
            var model = new DataSourceResult
            {
                //fill in model values from the entity
                Data = orders.Select(order => new AffiliateModel.AffiliatedOrderModel
                {
                    Id = order.Id,
                    OrderStatus = order.OrderStatus.GetLocalizedEnum(_localizationService, _workContext),
                    OrderStatusId = order.OrderStatusId,
                    PaymentStatus = order.PaymentStatus.GetLocalizedEnum(_localizationService, _workContext),
                    ShippingStatus = order.ShippingStatus.GetLocalizedEnum(_localizationService, _workContext),
                    OrderTotal = _priceFormatter.FormatPrice(order.OrderTotal, true, false),
                    CreatedOn = _dateTimeHelper.ConvertToUserTime(order.CreatedOnUtc, DateTimeKind.Utc),
                    CustomOrderNumber = order.CustomOrderNumber
                }),
                Total = orders.TotalCount
            };

            return model;
        }

        /// <summary>
        /// Prepare paged affiliated customer list model for the grid
        /// </summary>
        /// <param name="command">Pagination parameters</param>
        /// <param name="affiliate">Affiliate</param>
        /// <returns>Grid model</returns>
        public virtual DataSourceResult PrepareAffiliatedCustomerListGridModel(DataSourceRequest command, Affiliate affiliate)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (affiliate == null)
                throw new ArgumentNullException(nameof(affiliate));

            //get customers
            var customers = _customerService.GetAllCustomers(affiliateId: affiliate.Id,
                pageIndex: command.Page - 1, pageSize: command.PageSize);

            //prepare grid model
            var model = new DataSourceResult
            {
                //fill in model values from the entity
                Data = customers.Select(customer => new AffiliateModel.AffiliatedCustomerModel
                {
                    Id = customer.Id,
                    Name = customer.Email
                }),
                Total = customers.TotalCount
            };

            return model;
        }

        #endregion
    }
}