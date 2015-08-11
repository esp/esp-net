using System;
using System.Collections.Generic;
using System.Linq;
using Esp.Net.Examples.ComplexModel.Model.ReferenceData;
using Esp.Net.Examples.ComplexModel.Model.Schedule;
using Esp.Net.Examples.ComplexModel.Model.Snapshot;
using Esp.Net.Examples.ComplexModel.Model.Snapshot.Schedule;
using Esp.Net.Examples.ComplexModel.Model.Strategies;

namespace Esp.Net.Examples.ComplexModel.Model
{
    public class Option
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(Option));

        private readonly IReferenceDataGateway _referenceDataGateway;
        private readonly IScheduleGenerationGateway _scheduleGenerationGateway;
        private decimal? _notional;
        private string _currencyPair;
        private DateTime[] _holidayDates = new DateTime[0];
        private readonly Schedule.Schedule _schedule = new Schedule.Schedule();
        private int _version = 0;
        private bool _isValid = false;
        private FixingFrequency? _frequency;
        private List<Strategy> _strategies;
 
        public Option(Guid modelId, IReferenceDataGateway referenceDataGateway, IScheduleGenerationGateway scheduleGenerationGateway)
        {
            Id = modelId;
            _referenceDataGateway = referenceDataGateway;
            _scheduleGenerationGateway = scheduleGenerationGateway;
            _strategies = new List<Strategy>();
        }

        public Guid Id { get; private set; }

        public void IncrementVersion()
        {
            _version++;
            Log.DebugFormat("Model version is at {0}", _version);
        }

        public void SetNotional(decimal? notional)
        {
            Log.DebugFormat("Setting notional to {0}", notional);
            _notional = notional;
        }

        public void SetNotionalPerFixing(decimal? notionalPerFixing)
        {
            Log.DebugFormat("Setting notional per fixing to {0}", notionalPerFixing);
            _schedule.SetNotionalPerFixing(notionalPerFixing);
        }

        public void SetCurrencyPair(string currencyPair)
        {
            Log.DebugFormat("Setting currency pair to {0}", currencyPair);
            _currencyPair = currencyPair;

            // TODO properly model this request dispatch in order to correlate the result event and ensure it's still valid
            _referenceDataGateway.BeginGetReferenceDataForCurrencyPair(Id, currencyPair);
            _schedule.Reset();
            
            // Here you'd poke many other parts of the model, and they in turn poke their parts. 
            // Downside for large models is the'll become extremely rigid as you only interact with it via the top. 
            // On the up side it's more OO so it arguabely eaiser to pickup for new devs. 
        }

        public void ReceiveCurrencyPairReferenceData(CurrencyPairReferenceData referenceData)
        {
            Log.DebugFormat("Setting ref data");
            _holidayDates = referenceData.HolidayDates;
            _schedule.SetHolidayDates(referenceData.HolidayDates);
            TryGenerateSchedule();
        }

        public void AddScheduleCoupons(CouponSnapshot[] couponSnapshots)
        {
            Log.DebugFormat("Adding schedule Coupons");
            _schedule.AddScheduleCoupons(couponSnapshots);
        }

        public bool Validate()
        {
            Log.Debug("Running validation");
            _isValid = !string.IsNullOrWhiteSpace(_currencyPair) && _holidayDates.Length > 0 && _schedule.Validate();
            return _isValid;
        }

        public OptionSnapshot CreateSnapshot()
        {
            return new OptionSnapshot(
                _schedule.CreateSnapshot(), 
                _isValid, 
                _frequency, 
                _version, 
                _currencyPair, 
                _notional,
                _strategies.Select(s => s.CreateSnapShot()).ToList()
            );
        }

        public void SetFixingFrequency(FixingFrequency? frequency)
        {
            Log.DebugFormat("Setting fixing frequency to {0}", frequency);
            _frequency = frequency;
            TryGenerateSchedule();
        }

        private void TryGenerateSchedule()
        {
            bool canGenerate = 
                !_schedule.HasSchedule &&
                !string.IsNullOrWhiteSpace(_currencyPair) &&
                _frequency.HasValue;

            if (canGenerate)
            {
                Log.Debug("Generating schedule");
                // TODO properly model this request dispatch in order to correlate the result event and ensure it's still valid
                _scheduleGenerationGateway.BeginGenerateSchedule(Id, _currencyPair, _frequency.Value);
            }
        }
    }
}