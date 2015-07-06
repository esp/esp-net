#if ESP_EXPERIMENTAL

using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace Esp.Net.Examples
{
    public interface IBookingService
    {
        IObservable<string> AcceptQuote(string quoteId);
        IObservable<string> GenerateTermsheet(string quoteId);
    }

    public class BookingService : IBookingService
    {
        private readonly List<IObserver<string>> _bookingObservers = new List<IObserver<string>>();
        private readonly List<IObserver<string>> _termSheetObservers = new List<IObserver<string>>();

        public void SendBookingResponse(string response)
        {
            foreach (IObserver<string> observer in _bookingObservers)
            {
                observer.OnNext(response);
            }
        }

        public void SendTermsheetResponse(string response)
        {
            foreach (IObserver<string> observer in _termSheetObservers)
            {
                observer.OnNext(response);
            }
        }

        public IObservable<string> AcceptQuote(string quoteId)
        {
            return Observable.Create<string>(o =>
            {
                _bookingObservers.Add(o); 
                return () => { };
            });
        }

        public IObservable<string> GenerateTermsheet(string quoteId)
        {
            return Observable.Create<string>(o =>
            {
                _termSheetObservers.Add(o); 
                return () => { };
            });
        }
    }
}
#endif