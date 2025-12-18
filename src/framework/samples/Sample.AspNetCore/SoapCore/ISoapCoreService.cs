using System.ServiceModel;

namespace Sample.AspNetCore.SoapCore;

[ServiceContract(Namespace = "https://vnlog.schenker-ap.com/Watsons/ServiceWS.asmx")]
public interface ISoapCoreService
{
    [OperationContract]
    Task<bool> SalesOrder(SalesOrderModel model);
}

public class SoapCoreService : ISoapCoreService
{
    public Task<bool> SalesOrder(SalesOrderModel model)
    {
        Console.WriteLine(model.No);
        return Task.FromResult(true);
    }
}
