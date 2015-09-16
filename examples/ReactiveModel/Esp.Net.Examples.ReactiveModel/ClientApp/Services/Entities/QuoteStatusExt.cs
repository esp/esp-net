namespace Esp.Net.Examples.ReactiveModel.ClientApp.Services.Entities
{
    public static class QuoteStatusExt
    {
         public static bool IsEndState(this QuoteStatus status)
         {
            var isEndState = status == QuoteStatus.ClientRejected || status == QuoteStatus.TraderRejected || status == QuoteStatus.Booked;
            return isEndState;
        }

        public static bool RfqInFlight(this QuoteStatus status)
        {
            var rfqInFlight = status == QuoteStatus.Quoting || status == QuoteStatus.Requesting || status == QuoteStatus.Booking || status == QuoteStatus.Rejecting;
            return rfqInFlight;
        }
    }
}