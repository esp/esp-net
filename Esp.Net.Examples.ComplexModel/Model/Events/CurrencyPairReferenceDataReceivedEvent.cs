using Esp.Net.Examples.ComplexModel.Model.Entities.ReferenceData;

namespace Esp.Net.Examples.ComplexModel.Model.Events
{
    public class CurrencyPairReferenceDataReceivedEvent
    {
        public CurrencyPairReferenceDataReceivedEvent(CurrencyPairReferenceData refData)
        {
            RefData = refData;
        }

        public CurrencyPairReferenceData RefData { get; private set; }
    }
}