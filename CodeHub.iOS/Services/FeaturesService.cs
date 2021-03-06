using CodeHub.Core.Services;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace CodeHub.iOS.Services
{
    public class FeaturesService : IFeaturesService
    {
        private readonly IDefaultValueService _defaultValueService;
        private readonly IInAppPurchaseService _inAppPurchaseService;

        /// <summary>
        /// The pro edition identifier
        /// </summary>
        public const string ProEdition = "com.dillonbuchanan.codehub.pro";

        public FeaturesService(IDefaultValueService defaultValueService, IInAppPurchaseService inAppPurchaseService)
        {
            _defaultValueService = defaultValueService;
            _inAppPurchaseService = inAppPurchaseService;
        }

        public bool IsPushNotificationsActivated
        {
            get
            {
                return IsActivated(ProEdition);
            }
        }

        public bool IsEnterpriseSupportActivated
        {
            get
            {
                return IsActivated(ProEdition);
            }
        }

        public bool IsPrivateRepositoriesEnabled
        {
            get
            {
                return IsActivated(ProEdition);
            }
        }

        public bool IsProEnabled
        {
            get
            {
                return IsActivated(ProEdition);
            }
        }

        public async Task ActivatePro()
        {
            var productData = (await _inAppPurchaseService.RequestProductData(ProEdition)).Products.FirstOrDefault();
            if (productData == null)
                throw new InvalidOperationException("Unable to activate CodeHub Pro");
            await _inAppPurchaseService.PurchaseProduct(productData);
        }

        public Task RestorePro()
        {
            return _inAppPurchaseService.Restore();
        }

        private bool IsActivated(string id)
        {
            bool value;
            return _defaultValueService.TryGet<bool>(id, out value) && value;
        }
    }
}

