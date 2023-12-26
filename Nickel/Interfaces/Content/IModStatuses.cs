namespace Nickel;

public interface IModStatuses
{
    IStatusEntry RegisterStatus(string name, StatusConfiguration configuration);
}
