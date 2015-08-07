using System;

namespace Esp.Net.Examples.ComplexModel.Model.Schedule
{
    public interface IScheduleGenerationGateway
    { 
        void BeginGenerateSchedule(Guid modelId, string currencyPair, FixingFrequency value);
    }
}