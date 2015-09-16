using System;
using Esp.Net.Examples.ReactiveModel.TraderApp.Model.Entities;

namespace Esp.Net.Examples.ReactiveModel.TraderApp.Model.Gateways
{
    public interface IRfqServiceGateway
    {
        IDisposable BeginReceiveRfqEvents();
        void SendUpdate(RfqDetails rfqDetails, bool isLastmessage = false);
    }
}