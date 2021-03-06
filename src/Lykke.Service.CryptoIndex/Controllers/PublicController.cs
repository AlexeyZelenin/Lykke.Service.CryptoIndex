﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Lykke.Common.ApiLibrary.Exceptions;
using Lykke.Service.CryptoIndex.Client.Api;
using Lykke.Service.CryptoIndex.Client.Models;
using Lykke.Service.CryptoIndex.Domain.Repositories;
using Lykke.Service.CryptoIndex.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using IndexHistory = Lykke.Service.CryptoIndex.Domain.Models.IndexHistory;

namespace Lykke.Service.CryptoIndex.Controllers
{
    [Route("/api/[controller]")]
    public class PublicController : Controller, IPublicApi
    {
        private readonly IIndexHistoryRepository _indexHistoryRepository;
        private readonly IIndexStateRepository _indexStateRepository;
        private readonly IIndexCalculator _indexCalculator;
        private readonly IStatisticsService _statisticsService;

        private readonly object _sync = new object();
        private static DateTime? _lastReset;
        private static IndexHistory _midnight;

        public PublicController(IIndexHistoryRepository indexHistoryRepository, IIndexStateRepository indexStateRepository,
            IIndexCalculator indexCalculator, IStatisticsService statisticsService)
        {
            _indexHistoryRepository = indexHistoryRepository;
            _indexStateRepository = indexStateRepository;
            _indexCalculator = indexCalculator;
            _statisticsService = statisticsService;
        }

        [HttpGet("index/last")]
        [ProducesResponseType(typeof(PublicIndexHistory), (int)HttpStatusCode.OK)]
        [ResponseCache(Duration = 10, VaryByQueryKeys = new[] { "*" })]
        public async Task<PublicIndexHistory> GetLastAsync()
        {
            var domain = _indexCalculator.GetLastIndexHistory();

            if (domain == null)
                throw new ValidationApiException(HttpStatusCode.NotFound, "Last index value is not found.");

            var result = Mapper.Map<PublicIndexHistory>(domain);

            return result;
        }

        [HttpGet("change")]
        [ProducesResponseType(typeof(IReadOnlyList<(DateTime, decimal)>), (int)HttpStatusCode.OK)]
        [ResponseCache(Duration = 10, VaryByQueryKeys = new[] { "*" })]
        [Obsolete("Use GetKeyNumbers().Return24H instead.")]
        public async Task<IReadOnlyList<(DateTime, decimal)>> GetChangeAsync()
        {
            var resultPoints = new List<IndexHistory>();

            var from = DateTime.UtcNow.Date;

            lock (_sync)
            {
                var lastReset = _indexCalculator.GetLastReset();
                if (_midnight == null || _midnight.Time.Date != DateTime.UtcNow.Date || lastReset != _lastReset)
                {
                    _lastReset = lastReset;
                    from = _lastReset.HasValue && _lastReset.Value > from ? _lastReset.Value : from;
                    _midnight = _indexHistoryRepository.GetAsync(from, DateTime.UtcNow).GetAwaiter().GetResult().FirstOrDefault();
                }

                if (_midnight != null)
                    resultPoints.Add(_midnight);
            }       

            var last = _indexCalculator.GetLastIndexHistory();

            if (last != null)
                resultPoints.Add(last);

            var result = resultPoints.Select(x => (x.Time, x.Value)).OrderBy(x => x.Time).ToList();

            return result;
        }

        [HttpGet("indexHistory/{timeInterval}")]
        [ProducesResponseType(typeof(IDictionary<DateTime, decimal>), (int)HttpStatusCode.OK)]
        [ResponseCache(Duration = 10, VaryByQueryKeys = new[] { "*" })]
        public async Task<IDictionary<DateTime, decimal>> GetIndexHistory(TimeInterval timeInterval)
        {
            switch (timeInterval)
            {
                case TimeInterval.Hour24: return _statisticsService.GetIndexHistory24H();
                case TimeInterval.Day5: return _statisticsService.GetIndexHistory5D();
                case TimeInterval.Day30: return _statisticsService.GetIndexHistory30D();
                case TimeInterval.Unspecified:
                default: return new Dictionary<DateTime, decimal>();
            }
        }

        [HttpGet("keyNumbers")]
        [ProducesResponseType(typeof(KeyNumbers), (int)HttpStatusCode.OK)]
        [ResponseCache(Duration = 10, VaryByQueryKeys = new[] { "*" })]
        public async Task<KeyNumbers> GetKeyNumbers()
        {
            var domain = _statisticsService.GetKeyNumbers();

            var result = Mapper.Map<KeyNumbers>(domain);

            return result;
        }
    }
}
