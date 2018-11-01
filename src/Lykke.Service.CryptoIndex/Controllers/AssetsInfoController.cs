﻿using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Lykke.Service.CryptoIndex.Client.Api;
using Lykke.Service.CryptoIndex.Client.Models;
using Lykke.Service.CryptoIndex.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.CryptoIndex.Controllers
{
    [Route("/api/[controller]")]
    public class AssetsInfoController : Controller, IAssetsInfoApi
    {
        private readonly ISettingsService _settingsService;
        private readonly IIndexCalculator _indexCalculator;
        private readonly ITickPricesService _tickPricesService;

        public AssetsInfoController(ISettingsService settingsService, IIndexCalculator indexCalculator, ITickPricesService tickPricesService)
        {
            _settingsService = settingsService;
            _indexCalculator = indexCalculator;
            _tickPricesService = tickPricesService;
        }

        [HttpGet("all")]
        [ProducesResponseType(typeof(IReadOnlyList<AssetInfo>), (int)HttpStatusCode.OK)]
        [ResponseCache(Duration = 1, VaryByQueryKeys = new[] { "*" })]
        public async Task<IReadOnlyList<AssetInfo>> GetAllAsync()
        {
            var settings = await _settingsService.GetAsync();
            var marketCaps = await _indexCalculator.GetAllAssetsMarketCapsAsync();
            var prices = await _tickPricesService.GetPricesAsync();

            var result = new List<AssetInfo>();

            foreach (var asset in settings.Assets)
            {
                var marketCap = marketCaps[asset];

                IDictionary<string, decimal> assetPrices = new Dictionary<string, decimal>();
                if (prices.ContainsKey(asset))
                    assetPrices = prices[asset];

                var assetInfo = new AssetInfo
                {
                    Asset = asset,
                    MarketCap = marketCap,
                    Prices = assetPrices as IReadOnlyDictionary<string, decimal>
                };

                result.Add(assetInfo);
            }

            return result;
        }
    }
}
