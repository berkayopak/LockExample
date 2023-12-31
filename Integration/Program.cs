﻿using Integration.Service;
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
            /*
            //We can use Parallel for waiting all actions. Blocking IO
            Parallel.Invoke(
                () => service.SaveItem("a"),
                () => service.SaveItem("b"),
                () => service.SaveItem("c"));

            //Or we can use tasks with WaitAll too for waiting all actions. Blocking IO
            Task task1 = Task.Factory.StartNew(() => service.SaveItem("a"));
            Task task2 = Task.Factory.StartNew(() => service.SaveItem("b"));
            Task task3 = Task.Factory.StartNew(() => service.SaveItem("c"));
            Task.WaitAll(task1, task2, task3);
            */

            //This actions will run in parallel too, but we're not waiting in here. Non-Blocking IO
            ThreadPool.QueueUserWorkItem(_ => service.SaveItem("a"));
            ThreadPool.QueueUserWorkItem(_ => service.SaveItem("b"));
            ThreadPool.QueueUserWorkItem(_ => service.SaveItem("c"));

            Thread.Sleep(500);

            ThreadPool.QueueUserWorkItem(_ => service.SaveItem("a"));
            ThreadPool.QueueUserWorkItem(_ => service.SaveItem("b"));
            ThreadPool.QueueUserWorkItem(_ => service.SaveItem("c"));

            //https://learn.microsoft.com/en-us/dotnet/api/system.threading.threadpool.queueuserworkitem?view=net-8.0
            //P.S: You can add ManualResetEvent to your actions and you can call below function for wait all events
            //WaitHandle.WaitAll(ManualResetEventArray);
        }

        //P.S: Since we use Thread.Sleep, we create a blocking IO situation,
        //but since we do not have a system that can run continuously,
        //we keep it waiting in this way. We could have used the other
        //blocking IO methods(Task, Parallel) shown above instead of this approach.
        Thread.Sleep(5000);

        Console.WriteLine("Everything recorded:");

        service.GetAllItems().ForEach(Console.WriteLine);

        Console.ReadLine();
    }
}