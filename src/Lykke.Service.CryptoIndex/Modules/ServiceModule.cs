﻿using Autofac;
using AzureStorage.Tables;
using Common;
using JetBrains.Annotations;
using Lykke.CoinMarketCap.Client;
using Lykke.Common.Log;
using Lykke.Service.CryptoIndex.Domain.AzureRepositories.LCI10.IndexHistory;
using Lykke.Service.CryptoIndex.Domain.AzureRepositories.LCI10.IndexState;
using Lykke.Service.CryptoIndex.Domain.AzureRepositories.LCI10.Settings;
using Lykke.Service.CryptoIndex.Domain.LCI10;
using Lykke.Service.CryptoIndex.Domain.LCI10.IndexHistory;
using Lykke.Service.CryptoIndex.Domain.LCI10.IndexState;
using Lykke.Service.CryptoIndex.Domain.LCI10.Settings;
using Lykke.Service.CryptoIndex.Domain.MarketCapitalization;
using Lykke.Service.CryptoIndex.Domain.TickPrice;
using Lykke.Service.CryptoIndex.DomainServices.LCI10;
using Lykke.Service.CryptoIndex.DomainServices.MarketCapitalization;
using Lykke.Service.CryptoIndex.RabbitMq.Subscribers;
using Lykke.Service.CryptoIndex.Settings;
using Lykke.SettingsReader;

namespace Lykke.Service.CryptoIndex.Modules
{
    [UsedImplicitly]
    public class ServiceModule : Module
    {
        private readonly IReloadingManager<AppSettings> _appSettings;
        private readonly CryptoIndexSettings _settings;
        private readonly IReloadingManager<string> _connectionString;

        public ServiceModule(IReloadingManager<AppSettings> appSettings)
        {
            _appSettings = appSettings;
            _settings = _appSettings.CurrentValue.CryptoIndexService;
            _connectionString = _appSettings.Nested(x => x.CryptoIndexService.Db.DataConnectionString);
        }

        protected override void Load(ContainerBuilder builder)
        {
            // Subscribers

            foreach (var exchange in _settings.RabbitMq.Exchanges)
            {
                builder.RegisterType<TickPricesSubscriber>()
                    .AsSelf()
                    .As<IStartable>()
                    .As<IStopable>()
                    .WithParameter("connectionString", _settings.RabbitMq.ConnectionString)
                    .WithParameter("exchangeName", exchange)
                    .SingleInstance();
            }

            // Repositories

            builder.Register(container => new SettingsRepository(
                    AzureTableStorage<SettingsEntity>.Create(_connectionString,
                        nameof(Settings), container.Resolve<ILogFactory>())))
                .As<ISettingsRepository>()
                .SingleInstance();

            builder.Register(container => new IndexHistoryRepository(
                    AzureTableStorage<IndexHistoryEntity>.Create(_connectionString,
                        nameof(IndexHistory), container.Resolve<ILogFactory>())))
                .As<IIndexHistoryRepository>()
                .SingleInstance();

            builder.Register(container => new IndexStateRepository(
                    AzureTableStorage<IndexStateEntity>.Create(_connectionString,
                        nameof(IndexState), container.Resolve<ILogFactory>())))
                .As<IIndexStateRepository>()
                .SingleInstance();

            // Services

            builder.RegisterType<CoinMarketCapClient>()
                .As<ICoinMarketCapClient>()
                .WithParameter(TypedParameter.From(new CoinMarketCap.Client.Settings(_settings.CoinMarketCapApiKey)))
                .SingleInstance();

            builder.RegisterType<MarketCapitalizationService>()
                .As<IMarketCapitalizationService>()
                .SingleInstance();

            builder.RegisterType<SettingsService>()
                .As<ISettingsService>()
                .SingleInstance();

            builder.RegisterType<LCI10Calculator>()
                .As<ILCI10Calculator>()
                .As<ITickPriceHandler>()
                .As<IStartable>()
                .As<IStopable>()
                .WithParameter("weightsCalculationInterval", _settings.WeightsCalculationInterval)
                .WithParameter("indexCalculationInterval", _settings.IndexCalculationInterval)
                .SingleInstance();
        }
    }
}
