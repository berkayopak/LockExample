# Lock example

## Requirements

- Install docker engine

## How to try

### 1 - Single Server Scenario
- You can debug with Visual Studio(or any other IDE which supports dotnet project)
- If you don't want to debug, you can just run with "dotnet run" command on terminal
- Check "launchsettings.json" file on "Properties" folder for debug options

### 2 - Distributed System Scenario
- Open a terminal window
- Go to the folder where the docker-compose.yml file is located
- (Optional) Run "docker-compose build" command
- Run "docker-compose up" command

## Scenarios

### 1 - Single Server Scenario

**Solution Description:**
- I used c# lock statement for single instance scenario.

### 2 - Distributed System Scenario

**Solution Description:**
- I used RedLock(Redis lock) package for distributed system scenario.

**Weaknesses:**
- By using RedLock, project become dependent on redis. If there is a malfunction or delay in our Redis server, it becomes a factor that will affect the system.
- I set an expiry parameter for RedLock, this parameter needs to be carefully considered and adjusted according to the situation. If we set the expiry time too low, there is a risk of expiring the lock before the transaction is completed. On the contrary, if we set the expiry time too high, in case of a problem in the process, an unwanted lock may occur on the item basis.