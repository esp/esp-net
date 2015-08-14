using System;

namespace Esp.Net.Examples.ComplexModel.Model.ReferenceData
{
    public interface IReferenceDataGateway
    {
        void BeginGetReferenceDataForCurrencyPair(Guid modelId, string currencyPair);
    }
}