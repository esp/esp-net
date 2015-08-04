using System;
using Esp.Net.Examples.ComplexModel.Model.Entities.ReferenceData;

namespace Esp.Net.Examples.ComplexModel.Model.Entities.Schedule
{
    public class Schedule
    {
        public void SetReferenceData(CurrencyPairReferenceData referenceData)
        {

        }

        public Guid AddRow()
        {
            return Guid.NewGuid();
        }
    }
}