using System;
using Esp.Net.Examples.ReactiveModel.ClientApp.Model.Entities;
using Esp.Net.Examples.ReactiveModel.ClientApp.Model.Events;
using Esp.Net.Examples.ReactiveModel.ClientApp.Services;
using Esp.Net.Examples.ReactiveModel.ClientApp.Services.Entities;

namespace Esp.Net.Examples.ReactiveModel.ClientApp.Model.Gateways
{
    public class RequestForQuoteGateway : IRequestForQuoteGateway
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(RequestForQuoteGateway));

        private readonly IRfqServiceClient _rfqServiceClient;
        private readonly IRouter<OrderScreen> _router;

        public RequestForQuoteGateway(IRfqServiceClient rfqServiceClient, IRouter<OrderScreen> router)
        {
            _rfqServiceClient = rfqServiceClient;
            _router = router;
        }

        public IDisposable BegingGetQuote(Guid quoteId, CurrencyPair currencyPair, decimal notional)
        {
            Log.DebugFormat("Getting quote. Id {0}, {1}, {2}", quoteId, currencyPair.IsoCode, notional);
            return _rfqServiceClient.RequestQuote(new RfqRequest(quoteId, currencyPair, notional)).Subscribe(
                response =>
                {
                    Log.DebugFormat("Quote response received. Id {0}, {1}, {2}", quoteId, currencyPair.IsoCode, notional);
                    _router.PublishEvent(
                        new OrderResponseReceivedEvent(
                            response.QuoteId,
                            new CurrencyPair(response.CurrencyPair.IsoCode, response.CurrencyPair.Precision), 
                            response.Notional,
                            response.Rate,
                            response.IsLastMessage,
                            response.QuoteStatus
                        )
                    );
                }, 
                ex =>
                {
                    Log.ErrorFormat("Quote error. Id {0}, {1}, {2}", quoteId, currencyPair.IsoCode, notional);
                    _router.PublishEvent(new OrderResponseReceivedEvent(ex));
                },
                () =>
                {
                    // publish other event for unexpected completed cases if required
                }
            );
        }

        public IDisposable BegingAcceptQuote(Guid quoteId)
        {
            Log.DebugFormat("Accepting quote with id {0}", quoteId);
            return _rfqServiceClient.AcceptQuote(quoteId).Subscribe(
                ack =>
                {
                    Log.DebugFormat("Quote {0} accept ack received", quoteId);
                },
                ex =>
                {
                    // TODO        
                }
            );
        }

        public IDisposable BegingRejectQuote(Guid quoteId)
        {
            Log.DebugFormat("Rejecting quote with id {0}", quoteId);
            return _rfqServiceClient.RejectQuote(quoteId).Subscribe(
                ack =>
                {
                    Log.DebugFormat("Quote {0} reject ack received", quoteId);
                },
                ex =>
                {
                    // TODO        
                }
            );
        }
    }
}