﻿using System;
using System.Collections.Generic;

namespace Lykke.Service.CryptoIndex.Domain.Models
{
    public class Settings
    {
        /// <summary>
        /// Where the prices shall be taken from
        /// </summary>
        public IReadOnlyList<string> Sources { get; }

        /// <summary>
        /// White list of assets
        /// </summary>
        public IReadOnlyList<string> Assets { get; }

        /// <summary>
        /// List of frozen assets
        /// </summary>
        public IReadOnlyList<AssetSettings> AssetsSettings { get; }

        /// <summary>
        /// Count of the top assets
        /// </summary>
        public int TopCount { get; }

        /// <summary>
        /// Is crypto index calculation enabled
        /// </summary>
        public bool Enabled { get; }

        /// <summary>
        /// The time when CoinMarketCap data should be refreshed and weights recalculated.
        /// </summary>
        public TimeSpan RebuildTime { get; }

        /// <inheritdoc />
        public Settings(IReadOnlyList<string> sources, IReadOnlyList<string> assets, int topCount, bool enabled, TimeSpan rebuildTime,
            IReadOnlyList<AssetSettings> assetsSettings)
        {
            Sources = sources;
            Assets = assets;
            TopCount = topCount;
            Enabled = enabled;
            RebuildTime = rebuildTime;
            AssetsSettings = assetsSettings;
        }
    }
}
