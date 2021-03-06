﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AzureStorage;
using Common;
using Lykke.AzureStorage.Tables;
using Lykke.Service.CryptoIndex.Domain.Models;
using Lykke.Service.CryptoIndex.Domain.Repositories.Models;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.CryptoIndex.Domain.Repositories.Repositories
{
    public class IndexHistoryRepository : IIndexHistoryRepository
    {
        private readonly INoSQLTableStorage<IndexHistoryEntity> _storage;
        private readonly IndexHistoryBlobRepository _blobRepository;

        public IndexHistoryRepository(INoSQLTableStorage<IndexHistoryEntity> storage, IndexHistoryBlobRepository blobRepository)
        {
            _storage = storage;
            _blobRepository = blobRepository;
        }

        public async Task<IReadOnlyList<IndexHistory>> GetAsync(DateTime from, DateTime to)
        {
            var filter = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition(nameof(AzureTableEntity.PartitionKey), QueryComparisons.GreaterThan,
                    GetPartitionKey(to)),
                TableOperators.And,
                TableQuery.GenerateFilterCondition(nameof(AzureTableEntity.PartitionKey), QueryComparisons.LessThanOrEqual,
                    GetPartitionKey(from)));

            var query = new TableQuery<IndexHistoryEntity>().Where(filter);

            var models = await _storage.WhereAsync(query);

            var domain = models.OrderBy(x => x.Time)
                               .Select(x => 
                                    new IndexHistory(
                                        x.Value,
                                        Mapper.Map<AssetMarketCap[]>(x.MarketCaps),
                                        x.Weights,
                                        new List<TickPrice>(),
                                        new List<AssetPrice>(),
                                        x.MiddlePrices,
                                        x.Time,
                                        Mapper.Map<AssetSettings[]>(x.AssetsSettings)))
                               .ToList();
            
            return domain;
        }

        public async Task<IReadOnlyList<(DateTime, decimal)>> GetUpToDateAsync(DateTime to, int limit)
        {
            var filter = TableQuery.GenerateFilterCondition(nameof(AzureTableEntity.PartitionKey), QueryComparisons.GreaterThan,
                    GetPartitionKey(to));

            var query = new TableQuery<IndexHistoryEntity>().Where(filter).Take(limit);

            var models = await _storage.WhereAsync(query);

            var domain = models.OrderBy(x => x.Time).Select(x => (x.Time, x.Value)).ToList();

            return domain;
        }

        public async Task<IReadOnlyList<IndexHistory>> TakeLastAsync(int count, DateTime? from = null)
        {
            var fromValue = from ?? DateTime.MinValue;

            var filter = TableQuery.GenerateFilterCondition(nameof(AzureTableEntity.PartitionKey), QueryComparisons.LessThanOrEqual,
                GetPartitionKey(fromValue));

            var query = new TableQuery<IndexHistoryEntity>().Where(filter).Take(count);

            var models = await _storage.WhereAsync(query);

            var domain = models.Select(x => new IndexHistory(
                x.Value,
                Mapper.Map<AssetMarketCap[]>(x.MarketCaps),
                x.Weights,
                new List<TickPrice>(),
                new List<AssetPrice>(),
                x.MiddlePrices,
                x.Time,
                Mapper.Map<AssetSettings[]>(x.AssetsSettings))
            ).ToList();

            return domain;
        }

        public async Task<IReadOnlyList<DateTime>> GetTimestampsAsync(DateTime from, DateTime to)
        {
            var indexHistories = await GetAsync(from, to);

            var timestamps = indexHistories.OrderBy(x => x.Time).Select(x => x.Time).ToList();

            return timestamps;
        }

        public async Task InsertAsync(IndexHistory domain)
        {
            // Table
            var entity = Mapper.Map<IndexHistoryEntity>(domain);
            entity.PartitionKey = GetPartitionKey(domain.Time);
            entity.RowKey = GetRowKey(domain.Time);
            await _storage.InsertOrReplaceAsync(entity);

            // Blob
            var blob = Mapper.Map<IndexHistoryBlob>(domain);
            await _blobRepository.SaveAsync(blob);
        }

        public async Task<IndexHistory> GetAsync(DateTime dateTime)
        {
            var model = await _storage.GetDataAsync(GetPartitionKey(dateTime), GetRowKey(dateTime));

            if (model == null)
                return null;

            var blob = await _blobRepository.GetAsync(dateTime);

            if (blob == null)
                return null;

            var tickPrices = Mapper.Map<IReadOnlyCollection<TickPrice>>(blob.TickPrices);
            var assetPrices = Mapper.Map<IReadOnlyCollection<AssetPrice>>(blob.GetAssetPrices());

            var domain = new IndexHistory(
                model.Value,
                Mapper.Map<AssetMarketCap[]>(model.MarketCaps),
                model.Weights,
                tickPrices,
                assetPrices,
                model.MiddlePrices,
                model.Time,
                Mapper.Map<AssetSettings[]>(model.AssetsSettings));

            return domain;
        }

        private static string GetPartitionKey(DateTime time)
            => (DateTime.MaxValue.Ticks - time.Ticks).ToString();

        private static string GetRowKey(DateTime time)
            => time.ToIsoDateTime();
    }
}
