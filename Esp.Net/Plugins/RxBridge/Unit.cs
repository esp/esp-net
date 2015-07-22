#if ESP_LOCAL
// ReSharper disable once CheckNamespace
namespace System.Reactive.Linq
{
    internal class Unit
    {
        static Unit()
        {
            Default = new Unit();
        }

        public static Unit Default { get; private set; }
    }
}
#endif