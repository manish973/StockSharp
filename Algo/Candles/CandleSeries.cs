#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Candles.Algo
File: CandleSeries.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Candles
{
	using System;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Configuration;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Candles series.
	/// </summary>
	public class CandleSeries : NotifiableObject, IPersistable
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CandleSeries"/>.
		/// </summary>
		public CandleSeries()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CandleSeries"/>.
		/// </summary>
		/// <param name="candleType">The candle type.</param>
		/// <param name="security">The instrument to be used for candles formation.</param>
		/// <param name="arg">The candle formation parameter. For example, for <see cref="TimeFrameCandle"/> this value is <see cref="TimeFrameCandle.TimeFrame"/>.</param>
		public CandleSeries(Type candleType, Security security, object arg)
		{
			if (candleType == null)
				throw new ArgumentNullException(nameof(candleType));

			if (!candleType.IsCandle())
				throw new ArgumentOutOfRangeException(nameof(candleType), candleType, LocalizedStrings.WrongCandleType);

			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (arg == null)
				throw new ArgumentNullException(nameof(arg));

			_security = security;
			_candleType = candleType;
			_arg = arg;
			WorkingTime = security.CheckExchangeBoard().WorkingTime;
		}

		private Security _security;

		/// <summary>
		/// The instrument to be used for candles formation.
		/// </summary>
		public virtual Security Security
		{
			get => _security;
			set
			{
				_security = value;
				NotifyChanged(nameof(Security));
			}
		}

		private Type _candleType;

		/// <summary>
		/// The candle type.
		/// </summary>
		public virtual Type CandleType
		{
			get => _candleType;
			set
			{
				NotifyChanging(nameof(CandleType));
				_candleType = value;
				NotifyChanged(nameof(CandleType));
			}
		}

		private object _arg;

		/// <summary>
		/// The candle formation parameter. For example, for <see cref="TimeFrameCandle"/> this value is <see cref="TimeFrameCandle.TimeFrame"/>.
		/// </summary>
		public virtual object Arg
		{
			get => _arg;
			set
			{
				NotifyChanging(nameof(Arg));
				_arg = value;
				NotifyChanged(nameof(Arg));
			}
		}

		/// <summary>
		/// The time boundary, within which candles for give series shall be translated.
		/// </summary>
		public WorkingTime WorkingTime { get; set; }

		/// <summary>
		/// To perform the calculation <see cref="Candle.PriceLevels"/>. By default, it is disabled.
		/// </summary>
		public bool IsCalcVolumeProfile { get; set; }

		/// <summary>
		/// The initial date from which you need to get data.
		/// </summary>
		[Nullable]
		public DateTimeOffset? From { get; set; }

		/// <summary>
		/// The final date by which you need to get data.
		/// </summary>
		[Nullable]
		public DateTimeOffset? To { get; set; }

		/// <summary>
		/// Build candles mode.
		/// </summary>
		public BuildCandlesModes BuildCandlesMode { get; set; }

		/// <summary>
		/// Which market-data type is used as an candle source value.
		/// </summary>
		public MarketDataTypes? BuildCandlesFrom { get; set; }

		/// <summary>
		/// Extra info for the <see cref="BuildCandlesFrom"/>.
		/// </summary>
		public Level1Fields? BuildCandlesField { get; set; }

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return CandleType?.Name + "_" + Security + "_" + TraderHelper.CandleArgToFolderName(Arg);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			var secProvider = ConfigManager.TryGetService<ISecurityProvider>();
			if (secProvider != null)
			{
				var securityId = storage.GetValue<string>(nameof(SecurityId));

				if (!securityId.IsEmpty())
					Security = secProvider.LookupById(securityId);
			}

			CandleType = storage.GetValue(nameof(CandleType), CandleType);
			Arg = storage.GetValue(nameof(Arg), Arg);
			From = storage.GetValue(nameof(From), From);
			To = storage.GetValue(nameof(To), To);
			WorkingTime = storage.GetValue(nameof(WorkingTime), WorkingTime);

			IsCalcVolumeProfile = storage.GetValue(nameof(IsCalcVolumeProfile), IsCalcVolumeProfile);

			BuildCandlesMode = storage.GetValue(nameof(BuildCandlesMode), BuildCandlesMode);
			BuildCandlesFrom = storage.GetValue(nameof(BuildCandlesFrom), BuildCandlesFrom);
			BuildCandlesField = storage.GetValue(nameof(BuildCandlesField), BuildCandlesField);
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			if (Security != null)
				storage.SetValue(nameof(SecurityId), Security.Id);

			if (CandleType != null)
				storage.SetValue(nameof(CandleType), CandleType.GetTypeName(false));

			if (Arg != null)
				storage.SetValue(nameof(Arg), Arg);

			storage.SetValue(nameof(From), From);
			storage.SetValue(nameof(To), To);

			if (WorkingTime != null)
				storage.SetValue(nameof(WorkingTime), WorkingTime);

			storage.SetValue(nameof(IsCalcVolumeProfile), IsCalcVolumeProfile);

			storage.SetValue(nameof(BuildCandlesMode), BuildCandlesMode);
			storage.SetValue(nameof(BuildCandlesFrom), BuildCandlesFrom);
			storage.SetValue(nameof(BuildCandlesField), BuildCandlesField);
		}
	}
}