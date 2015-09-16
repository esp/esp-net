using System;
using System.Reactive.Disposables;
using Esp.Net.Examples.ReactiveModel.TraderApp.Model.Entities;
using Esp.Net.Examples.ReactiveModel.TraderApp.Model.Events;
using Esp.Net.Examples.ReactiveModel.TraderApp.Services;
using Esp.Net.Examples.ReactiveModel.TraderApp.Services.Entities;

namespace Esp.Net.Examples.ReactiveModel.TraderApp.Model.Gateways
{
    public class RfqServiceGateway : IRfqServiceGateway
    {
        private readonly IRfqService _rfqService;
        private readonly IRouter<RfqScreen> _router;

        public RfqServiceGateway(IRfqService rfqService, IRouter<RfqScreen> router)
        {
            _rfqService = rfqService;
            _router = router;
        }

        public IDisposable BeginReceiveRfqEvents()
        {
            var disposables = new CompositeDisposable();
            disposables.Add(
                _rfqService.QuoteRequests.Subscribe(
                    rfqRequest => _router.PublishEvent(new RfqReceivedEvent(rfqRequest))
                )
            );
            disposables.Add(
                _rfqService.QuoteAccepts.Subscribe(
                    quoteId => _router.PublishEvent(new ClientAcceptedQuoteEvent(quoteId))
                )
            );
            disposables.Add(
                _rfqService.QuoteRejects.Subscribe(
                    quoteId => _router.PublishEvent(new ClientRejectQuoteEvent(quoteId))
                )
            );
            disposables.Add(
                _rfqService.QuotesClosed.Subscribe(
                    quoteId => _router.PublishEvent(new ClientClosedQuoteEvent(quoteId))
                )
            );
            return disposables;
        }

        public void SendUpdate(RfqDetails rfqDetails, bool isLastmessage = false)
        {
            var response = new RfqResponse(
                rfqDetails.QuoteId, 
                rfqDetails.CurrencyPair, 
                rfqDetails.Notional, 
                rfqDetails.Rate.Value,
                rfqDetails.QuoteStatus, 
                isLastmessage
            );
            _rfqService.SendUpdate(response);
        }
    }
}