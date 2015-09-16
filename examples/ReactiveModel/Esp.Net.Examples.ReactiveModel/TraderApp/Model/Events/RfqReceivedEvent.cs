using Esp.Net.Examples.ReactiveModel.TraderApp.Services.Entities;

namespace Esp.Net.Examples.ReactiveModel.TraderApp.Model.Events
{
    public class RfqReceivedEvent
    {
        public RfqReceivedEvent(RfqRequest request)
        {
            Request = request;
        }

        public RfqRequest Request { get; private set; } 
    }
}