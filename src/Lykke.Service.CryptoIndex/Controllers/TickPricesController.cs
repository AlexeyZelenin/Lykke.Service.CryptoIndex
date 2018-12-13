﻿using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Lykke.Service.CryptoIndex.Client.Api;
using Lykke.Service.CryptoIndex.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.CryptoIndex.Controllers
{
    [Route("/api/[controller]")]
    public class TickPricesController : Controller, ITickPricesApi
    {
        private readonly ITickPricesService _tickPricesService;
        private readonly IIndexCalculator _indexCalculator;
        private readonly ISettingsService _settingsService;

        public TickPricesController(ITickPricesService tickPricesService, IIndexCalculator indexCalculator, ISettingsService settingsService)
        {
            _tickPricesService = tickPricesService;
            _indexCalculator = indexCalculator;
            _settingsService = settingsService;
        }

        [HttpGet("sources")]
        [ProducesResponseType(typeof(IReadOnlyList<string>), (int)HttpStatusCode.OK)]
        public async Task<IReadOnlyList<string>> GetSourcesAsync()
        {
            var prices = await _tickPricesService.GetPricesAsync();

            var result = prices.SelectMany(x => x.Value.Keys).Distinct().OrderBy(x => x).ToList();

            return result;
        }

        [HttpGet("assets")]
        [ProducesResponseType(typeof(IReadOnlyList<string>), (int)HttpStatusCode.OK)]
        public async Task<IReadOnlyList<string>> GetAssetsAsync()
        {
            var settings = await _settingsService.GetAsync();
            var marketCapsAssets = (await _indexCalculator.GetAllAssetsMarketCapsAsync()).Keys.ToList();
            var prices = await _tickPricesService.GetPricesAsync(settings.Sources.ToList());

            if (marketCapsAssets.Any())
            {
                foreach (var priceAsset in prices.Keys.ToList())
                {
                    if (!marketCapsAssets.Contains(priceAsset))
                    {
                        prices.Remove(priceAsset);
                    }
                }
            }

            var result = prices.Select(x => x.Key).Distinct().OrderBy(x => x).ToList();

            return result;
        }
    }
}
