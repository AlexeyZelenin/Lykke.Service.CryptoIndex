﻿using System.Collections.Generic;

namespace Lykke.Service.CryptoIndex.Client.Models
{
    /// <summary>
    /// Represents information about an asset
    /// </summary>
    public class AssetInfo
    {
        /// <summary>
        /// Asset name
        /// </summary>
        public string Asset { get; set; }

        /// <summary>
        /// Cross asset name
        /// </summary>
        public string CrossAsset { get; set; }

        /// <summary>
        /// Market Cap of asset
        /// </summary>
        public decimal MarketCap { get; set; }

        /// <summary>
        /// Exchanges prices of asset
        /// </summary>
        public IReadOnlyDictionary<string, decimal> Prices { get; set; } = new Dictionary<string, decimal>();

        ///<inheritdoc />
        public override string ToString()
        {
            return $"{Asset}/{CrossAsset}, {MarketCap}, {Prices?.Count}";
        }
    }
}
