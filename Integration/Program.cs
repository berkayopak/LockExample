using Integration.Service;
using RedLockNet.SERedis.Configuration;
using RedLockNet.SERedis;
using System.Net;

namespace Integration;

public abstract class Program
{
    public static void Main(string[] args)
    {
        var service = new ItemIntegrationService();

        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        Console.WriteLine($"Environment: {environment}");

        //Production env for clustered scenario
        if (environment == "Production")
        {
            var endPoints = new List<RedLockEndPoint>
            {
                new DnsEndPoint("cache", 6379)
            };
            var redlockFactory = RedLockFactory.Create(endPoints);

            ThreadPool.QueueUserWorkItem(async _ => await service.SaveItemForDistributedSystem("a", redlockFactory));
            ThreadPool.QueueUserWorkItem(async _ => await service.SaveItemForDistributedSystem("b", redlockFactory));
            ThreadPool.QueueUserWorkItem(async _ => await service.SaveItemForDistributedSystem("c", redlockFactory));

            Thread.Sleep(500);

            ThreadPool.QueueUserWorkItem(async _ => await service.SaveItemForDistributedSystem("a", redlockFactory));
            ThreadPool.QueueUserWorkItem(async _ => await service.SaveItemForDistributedSystem("b", redlockFactory));
            ThreadPool.QueueUserWorkItem(async _ => await service.SaveItemForDistributedSystem("c", redlockFactory));
        }
        else
        {
            ThreadPool.QueueUserWorkItem(_ => service.SaveItem("a"));
            ThreadPool.QueueUserWorkItem(_ => service.SaveItem("b"));
            ThreadPool.QueueUserWorkItem(_ => service.SaveItem("c"));

            Thread.Sleep(500);

            ThreadPool.QueueUserWorkItem(_ => service.SaveItem("a"));
            ThreadPool.QueueUserWorkItem(_ => service.SaveItem("b"));
            ThreadPool.QueueUserWorkItem(_ => service.SaveItem("c"));
        }

        Thread.Sleep(5000);

        Console.WriteLine("Everything recorded:");

        service.GetAllItems().ForEach(Console.WriteLine);

        Console.ReadLine();
    }
}