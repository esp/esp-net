namespace Esp.Net
{
    public class TestModel
    {
        public int AnInt { get; set; }
        public string AString { get; set; }
        public decimal ADecimal { get; set; }
    }

    public class BaseEvent { }
    public class Event1 : BaseEvent { }
    public class Event2 : BaseEvent { }
    public class Event3 : BaseEvent { }
}