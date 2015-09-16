using System;

namespace Esp.Net.Examples.ReactiveModel.TraderApp.Model.Events
{
    public class RfqRateChangedEvent
    {
        public RfqRateChangedEvent(decimal? rate, Guid rfqCorrelationId)
        {
            Rate = rate;
            RfqCorrelationId = rfqCorrelationId;
        }

        public Guid RfqCorrelationId { get; private set; }

        public decimal? Rate { get; private set; } 
    }
}