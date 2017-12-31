﻿namespace StockSharp.Algo.Import
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Configuration;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// Messages importer from text file in CSV format into storage.
	/// </summary>
	public class CsvImporter : CsvParser
	{
		private readonly IEntityRegistry _entityRegistry;
		private readonly IExchangeInfoProvider _exchangeInfoProvider;
		private readonly IMarketDataDrive _drive;
		private readonly StorageFormats _storageFormat;

		/// <summary>
		/// Initializes a new instance of the <see cref="CsvImporter"/>.
		/// </summary>
		/// <param name="dataType">Data type info.</param>
		/// <param name="fields">Importing fields.</param>
		/// <param name="entityRegistry">The storage of trade objects.</param>
		/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
		/// <param name="drive">The storage. If a value is <see langword="null" />, <see cref="IStorageRegistry.DefaultDrive"/> will be used.</param>
		/// <param name="storageFormat">The format type. By default <see cref="StorageFormats.Binary"/> is passed.</param>
		public CsvImporter(DataType dataType, IEnumerable<FieldMapping> fields, IEntityRegistry entityRegistry, IExchangeInfoProvider exchangeInfoProvider, IMarketDataDrive drive, StorageFormats storageFormat)
			: base(dataType, fields)
		{
			if (entityRegistry == null)
				throw new ArgumentNullException(nameof(entityRegistry));

			if (exchangeInfoProvider == null)
				throw new ArgumentNullException(nameof(exchangeInfoProvider));

			_entityRegistry = entityRegistry;
			_exchangeInfoProvider = exchangeInfoProvider;
			_drive = drive;
			_storageFormat = storageFormat;
		}

		/// <summary>
		/// Update duplicate securities if they already exists.
		/// </summary>
		public bool UpdateDuplicateSecurities { get; set; }

		/// <summary>
		/// Import from CSV file.
		/// </summary>
		/// <param name="fileName">File name.</param>
		/// <param name="updateProgress">Progress notification.</param>
		/// <param name="isCancelled">The processor, returning process interruption sign.</param>
		public void Import(string fileName, Action<int> updateProgress, Func<bool> isCancelled)
		{
			var buffer = new List<dynamic>();

			this.AddInfoLog(LocalizedStrings.Str2870Params.Put(fileName, DataType.MessageType.Name));

			try
			{
				var len = new FileInfo(fileName).Length;
				var prevPercent = 0;
				var lineIndex = 0;

				foreach (var instance in Parse(fileName, isCancelled))
				{
					if (!(instance is SecurityMessage secMsg))
					{
						buffer.Add(instance);

						if (buffer.Count > 1000)
							FlushBuffer(buffer);
					}
					else
					{
						var security = _entityRegistry.Securities.ReadBySecurityId(secMsg.SecurityId);

						if (security != null)
						{
							if (!UpdateDuplicateSecurities)
							{
								this.AddErrorLog(LocalizedStrings.Str1453.Put(secMsg.SecurityId));
								continue;
							}

							security.Type = secMsg.SecurityType ?? secMsg.SecurityId.SecurityType;
							security.CfiCode = secMsg.CfiCode;
							security.Strike = secMsg.Strike;
							security.OptionType = secMsg.OptionType;
							security.Name = secMsg.Name;
							security.ShortName = secMsg.ShortName;
							security.Class = secMsg.Class;
							security.BinaryOptionType = secMsg.BinaryOptionType;
							security.ExternalId = secMsg.SecurityId.ToExternalId();
							security.ExpiryDate = secMsg.ExpiryDate;
							security.SettlementDate = secMsg.SettlementDate;
							security.UnderlyingSecurityId = secMsg.UnderlyingSecurityCode + "@" + secMsg.SecurityId.BoardCode;
							security.Currency = secMsg.Currency;
							security.PriceStep = secMsg.PriceStep;
							security.Decimals = secMsg.Decimals;
							security.VolumeStep = secMsg.VolumeStep;
							security.Multiplier = secMsg.Multiplier;
							security.IssueSize = secMsg.IssueSize;
							security.IssueDate = secMsg.IssueDate;
							security.UnderlyingSecurityType = secMsg.UnderlyingSecurityType;
						}
						else
							security = secMsg.ToSecurity(_exchangeInfoProvider);

						_entityRegistry.Securities.Save(security);

						if (ExtendedInfoStorageItem != null)
						{
							ExtendedInfoStorageItem.Add(secMsg.SecurityId, secMsg.ExtensionInfo);
						}
					}

					var percent = (int)(((double)lineIndex / len) * 100 - 1).Round();

					lineIndex++;

					if (percent <= prevPercent)
						continue;

					prevPercent = percent;
					updateProgress?.Invoke(prevPercent);
				}
			}
			catch (Exception ex)
			{
				ex.LogError();
			}

			if (buffer.Count > 0)
				FlushBuffer(buffer);
		}

		private Security InitSecurity(SecurityId securityId, IExchangeInfoProvider exchangeInfoProvider)
		{
			var id = securityId.ToStringId();
			var security = _entityRegistry.Securities.ReadById(id);

			if (security != null)
				return security;

			security = new Security
			{
				Id = id,
				Code = securityId.SecurityCode,
				Board = exchangeInfoProvider.GetOrCreateBoard(securityId.BoardCode),
				Type = securityId.SecurityType,
			};

			_entityRegistry.Securities.Save(security);
			this.AddInfoLog(LocalizedStrings.Str2871Params.Put(id));

			return security;
		}

		private void FlushBuffer(List<dynamic> buffer)
		{
			var registry = ConfigManager.GetService<IStorageRegistry>();

			if (DataType.MessageType == typeof(NewsMessage))
			{
				registry.GetNewsMessageStorage(_drive, _storageFormat).Save(buffer);
			}
			else
			{
				foreach (var typeGroup in buffer.GroupBy(i => i.GetType()))
				{
					var dataType = (Type)typeGroup.Key;

					foreach (var secGroup in typeGroup.GroupBy(i => (SecurityId)i.SecurityId))
					{
						var secId = secGroup.Key;
						var security = InitSecurity(secGroup.Key, _exchangeInfoProvider);

						if (dataType.IsCandleMessage())
						{
							var timeFrame = (TimeSpan)DataType.Arg;
							var candles = secGroup.Cast<CandleMessage>().ToArray();

							foreach (var candle in candles)
							{
								if (candle.CloseTime < candle.OpenTime)
								{
									// если в файле время закрытия отсутствует
									if (candle.CloseTime.Date == candle.CloseTime)
										candle.CloseTime = default(DateTimeOffset);
								}
								else if (candle.CloseTime > candle.OpenTime)
								{
									// если в файле время открытия отсутствует
									if (candle.OpenTime.Date == candle.OpenTime)
									{
										candle.OpenTime = candle.CloseTime;

										//var tfCandle = candle as TimeFrameCandle;

										//if (tfCandle != null)
										candle.CloseTime += timeFrame;
									}
								}
							}

							registry
								.GetCandleMessageStorage(dataType, security, DataType.Arg, _drive, _storageFormat)
								.Save(candles.OrderBy(c => c.OpenTime));
						}
						else if (dataType == typeof(TimeQuoteChange))
						{
							registry
								.GetQuoteMessageStorage(security, _drive, _storageFormat)
								.Save(secGroup
									.GroupBy(i => i.Time)
									.Select(g => new QuoteChangeMessage
									{
										SecurityId = secId,
										ServerTime = g.Key,
										Bids = g.Cast<QuoteChange>().Where(q => q.Side == Sides.Buy).ToArray(),
										Asks = g.Cast<QuoteChange>().Where(q => q.Side == Sides.Sell).ToArray(),
									})
									.OrderBy(md => md.ServerTime));
						}
						else
						{
							var storage = registry.GetStorage(security, dataType, DataType.Arg, _drive, _storageFormat);

							if (dataType == typeof(ExecutionMessage))
								((IMarketDataStorage<ExecutionMessage>)storage).Save(secGroup.Cast<ExecutionMessage>().OrderBy(m => m.ServerTime));
							else if (dataType == typeof(Level1ChangeMessage))
								((IMarketDataStorage<Level1ChangeMessage>)storage).Save(secGroup.Cast<Level1ChangeMessage>().OrderBy(m => m.ServerTime));
							else
								throw new NotSupportedException(LocalizedStrings.Str2872Params.Put(dataType.Name));
						}
					}
				}
			}

			buffer.Clear();
		}
	}
}