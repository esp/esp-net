using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using Esp.Net.Examples.ReactiveModel.ClientApp.Services;
using Esp.Net.Examples.ReactiveModel.ClientApp.Services.Entities;
using Esp.Net.Examples.ReactiveModel.Common.Services;
using Esp.Net.Examples.ReactiveModel.TraderApp.Services;
using TraderAppEntities = Esp.Net.Examples.ReactiveModel.TraderApp.Services.Entities;

namespace Esp.Net.Examples.ReactiveModel.Common
{
    // Given both the sales and trader app work in the same process, this exists to fake up the communications between them.
    // It does this by implementing the simplistic lower level messaging interfaces for RFQ on both sides.
    // Given this it's doing some crazy anti-pattern like things, e.g. storing IObservers, never clearing requests from memory etc.
    // i.e. Don't look to this for guidance, it exists to wire the example apps together!! 
    public class FakeMiddleware : IRfqServiceClient, IRfqService, IReferenceDataServiceClient
    {
        public static FakeMiddleware Instance = new FakeMiddleware();

        private readonly Subject<TraderAppEntities.RfqRequest> _requests = new Subject<TraderAppEntities.RfqRequest>();
        private readonly Subject<Guid> _quoteAccepts = new Subject<Guid>();
        private readonly Subject<Guid> _quoteRejects = new Subject<Guid>();
        private readonly Subject<Guid> _quoteClosed = new Subject<Guid>();
        private readonly Dictionary<Guid, IObserver<RfqResponse>> _inFlightRfqs = new Dictionary<Guid, IObserver<RfqResponse>>();
        private readonly ISchedulerService _schedulerService = new SchedulerService();

        IObservable<RfqResponse> IRfqServiceClient.RequestQuote(RfqRequest request)
        {
            return Observable.Create<RfqResponse>(o =>
            {
                _inFlightRfqs.Add(request.QuoteId, o);
                TraderAppEntities.RfqRequest mappedRequest = new TraderAppEntities.RfqRequest(
                    request.QuoteId, 
                    new TraderAppEntities.CurrencyPair(request.CurrencyPair.IsoCode, request.CurrencyPair.Precision),
                    request.Notional
                );
                _requests.OnNext(mappedRequest);
                return () =>
                {
                    _quoteClosed.OnNext(request.QuoteId);
                };
            });
        }

        IObservable<Unit> IRfqServiceClient.AcceptQuote(Guid quoteId)
        {
            return Observable.Create<Unit>(o =>
            {
                _quoteAccepts.OnNext(quoteId);
                o.OnNext(Unit.Default);
                return () =>
                {
                };
            });
        }

        public IObservable<Unit> RejectQuote(Guid quoteId)
        {
            return Observable.Create<Unit>(o =>
            {
                _quoteRejects.OnNext(quoteId);
                o.OnNext(Unit.Default);
                return () =>
                {
                };
            });
        }

        void IRfqService.SendUpdate(TraderAppEntities.RfqResponse response)
        {
            var observer = _inFlightRfqs[response.QuoteId];
            RfqResponse mappedResponse = new RfqResponse(
                response.QuoteId,
                new CurrencyPair(response.CurrencyPair.IsoCode, response.CurrencyPair.Precision),
                response.Notional,
                response.Rate,
                MapQuoteStatus(response.QuoteStatus)
            );
            observer.OnNext(mappedResponse);

            if (response.IsLastMessage) observer.OnCompleted();
        }

        IObservable<TraderAppEntities.RfqRequest> IRfqService.QuoteRequests
        {
            get { return _requests.AsObservable(); }
        }

        IObservable<Guid> IRfqService.QuoteAccepts
        {
            get { return _quoteAccepts.AsObservable(); }
        }

        IObservable<Guid> IRfqService.QuoteRejects
        {
            get { return _quoteRejects.AsObservable(); }
        }

        IObservable<Guid> IRfqService.QuotesClosed
        {
            get { return _quoteClosed.AsObservable(); }
        }

        IObservable<CurrencyPair[]> IReferenceDataServiceClient.GetCurrencyPairs()
        {
            return Observable.Create<CurrencyPair[]> (o =>
            {
                return _schedulerService.Ui.Schedule(TimeSpan.FromSeconds(3), () =>
                {
                    var pairs = new[]
                    {
                        new  CurrencyPair("EURUSD", 4), 
                        new  CurrencyPair("EURGBP", 4),
                        new  CurrencyPair("EURJPY", 6), 
                        new  CurrencyPair("USDAUD", 4), 
                        new  CurrencyPair("USDCAD", 4), 
                    };
                    o.OnNext(pairs);
                });
            });
        }

        private QuoteStatus MapQuoteStatus(TraderAppEntities.QuoteStatus quoteStatus)
        {
            switch (quoteStatus)
            {
                case TraderAppEntities.QuoteStatus.New:
                    return QuoteStatus.New;
                case TraderAppEntities.QuoteStatus.Quoting:
                    return QuoteStatus.Quoting;
                case TraderAppEntities.QuoteStatus.ClientRejected:
                    return QuoteStatus.ClientRejected;
                case TraderAppEntities.QuoteStatus.TraderRejected:
                    return QuoteStatus.TraderRejected;
                case TraderAppEntities.QuoteStatus.Booked:
                    return QuoteStatus.Booked;
                default:
                    throw new ArgumentOutOfRangeException(nameof(quoteStatus), quoteStatus, null);
            }
        }
    }
}