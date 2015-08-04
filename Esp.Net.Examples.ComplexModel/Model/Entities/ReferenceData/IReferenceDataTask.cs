using System;

namespace Esp.Net.Examples.ComplexModel.Model.Entities.ReferenceData
{
    public interface IReferenceDataTask
    {
        void BeginGetReferenceDataForCurrencyPair(Guid modelId, string currencyPair);
    }
}