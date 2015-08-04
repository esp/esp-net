using System;

namespace Esp.Net.Examples.ComplexModel.Model.Entities.ReferenceData
{
    internal interface IReferenceDataTask
    {
        void BeginGetReferenceDataForCurrencyPair(Guid modelId, string currencyPair);
    }
}