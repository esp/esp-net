namespace Esp.Net
{
    public interface ICloneable<out T>
    {
        T Clone();
    }
}