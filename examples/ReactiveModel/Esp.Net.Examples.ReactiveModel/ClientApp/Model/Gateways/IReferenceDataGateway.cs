using System;

namespace Esp.Net.Examples.ReactiveModel.ClientApp.Model.Gateways
{
    public interface IReferenceDataGateway
    {
        IDisposable BeginGetReferenceData();
    }
}