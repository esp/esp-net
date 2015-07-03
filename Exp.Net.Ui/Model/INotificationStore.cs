namespace Exp.Net.Ui.Model
{
    public interface INotificationStore
    {
        void AddNotification(INotification notification);
    }

    public interface INotification
    {
        string NotificationTextText { get; }
    }
}