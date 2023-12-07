using Integration.Common;
using Integration.Backend;
using RedLockNet.SERedis;
using System.Collections.Concurrent;

namespace Integration.Service;

public sealed class ItemIntegrationService
{
    //This is a dependency that is normally fulfilled externally.
    private ItemOperationBackend ItemIntegrationBackend { get; set; } = new();
    private readonly ConcurrentDictionary<string, object> lockItemDictionary = new();

    // This is called externally and can be called multithreaded, in parallel.
    // More than one item with the same content should not be saved. However,
    // calling this with different contents at the same time is OK, and should
    // be allowed for performance reasons.
    public Result SaveItem(string itemContent)
    {
        //The item key variable is expressed hypothetically for this example.
        var itemKey = itemContent;
        lockItemDictionary[itemKey] = itemContent;

        lock (lockItemDictionary[itemKey])
        {
            // Check the backend to see if the content is already saved.
            if (ItemIntegrationBackend.FindItemsWithContent(itemContent).Count != 0)
            {
                return new Result(false, $"Duplicate item received with content {itemContent}.");
            }

            var item = ItemIntegrationBackend.SaveItem(itemContent);

            return new Result(true, $"Item with content {itemContent} saved with id {item.Id}");
        }
    }

    public async Task<Result> SaveItemForDistributedSystem(string itemContent, RedLockFactory redLockFactory)
    {
        //I set the expiry time as 40 seconds because it is said to be a process that
        //realistically takes 40 seconds, but it can be changed depending on the situation.
        var expiry = TimeSpan.FromSeconds(40);

        //There are also non async Create() methods
        await using var redLock = await redLockFactory.CreateLockAsync(itemContent, expiry); 

        //Make sure we got the lock
        if (redLock.IsAcquired)
        {
            //P.S: We need to integrate real source(like sql, nosql etc.) for clustered systems.
            //Right now ItemIntegrationBackend checking local context so we cant really check data on clustered arch.
            // Check the backend to see if the content is already saved.
            if (ItemIntegrationBackend.FindItemsWithContent(itemContent).Count != 0)
            {
                return new Result(false, $"Duplicate item received with content {itemContent}.");
            }

            var item = ItemIntegrationBackend.SaveItem(itemContent);

            //It was added to create parallel request scenarios while testing.
            //If the operations here finish too quickly, a pod/image that starts
            //late(due to docker-compose) may not be affected by the lock.
            Thread.Sleep(5000);

            return new Result(true, $"Item with content {itemContent} saved with id {item.Id}");
        }
        //The lock is automatically released at the end of the using block

        return new Result(false, $"Some error occured due to redLock mechanism. ItemContent = {itemContent}.");
    }

    public List<Item> GetAllItems()
    {
        return ItemIntegrationBackend.GetAllItems();
    }
}