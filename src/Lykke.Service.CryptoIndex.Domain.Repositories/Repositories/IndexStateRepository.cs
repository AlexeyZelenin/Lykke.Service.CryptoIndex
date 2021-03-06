﻿using System.Threading.Tasks;
using AutoMapper;
using AzureStorage;
using Lykke.Service.CryptoIndex.Domain.Models;
using Lykke.Service.CryptoIndex.Domain.Repositories.Models;

namespace Lykke.Service.CryptoIndex.Domain.Repositories.Repositories
{
    public class IndexStateRepository : IIndexStateRepository
    {
        private const string ConstKey = nameof(IndexState);
        private readonly INoSQLTableStorage<IndexStateEntity> _storage;

        public IndexStateRepository(INoSQLTableStorage<IndexStateEntity> storage)
        {
            _storage = storage;
        }

        public async Task SetAsync(IndexState indexState)
        {
            var model = Mapper.Map<IndexStateEntity>(indexState);
            model.PartitionKey = ConstKey;
            model.RowKey = ConstKey;

            await _storage.InsertOrReplaceAsync(model);
        }

        public async Task<IndexState> GetAsync()
        {
            var model = await _storage.GetDataAsync(ConstKey, ConstKey);

            var domain = Mapper.Map<IndexState>(model);

            return domain;
        }

        public async Task Clear()
        {
            await _storage.DeleteAsync(ConstKey, ConstKey);
        }
    }
}
