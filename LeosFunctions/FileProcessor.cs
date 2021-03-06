using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace LeosFunctions
{
    public static class FileProcessor
    {
        [FunctionName("FileProcessor")]
        public static async Task<string> RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var fileId = context.GetInput<string>();
            
            context.SetCustomStatus("Parsing people...");
            var engineers = await context.CallActivityAsync<List<string>>("FileProcessor_ParseEngineers", fileId);
            context.SetCustomStatus("Gathering grunts...");
            var codeMonkeys = await context.CallSubOrchestratorAsync<ICollection<string>>("FileProcessor_WhoIsACodeMonkey", engineers);
            context.SetCustomStatus("Searching superstars...");
            var microserviceSuperstars = await context.CallSubOrchestratorAsync<ICollection<string>>("FileProcessor_WhoIsAMicroserviceSuperstar", engineers.Where(engineer => !codeMonkeys.Contains(engineer)).ToList());
            context.SetCustomStatus("Leveling lackeys...");
            var levels = await context.CallSubOrchestratorAsync<Dictionary<string, int>>("FileProcessor_GetLevels", engineers);

            context.SetCustomStatus("Transmitting textfile...");
            var resultFileId = await context.CallActivityAsync<string>("FileProcessor_CreateResultsFile", new FileProcessorResult
            {
                AllEngineers = engineers,
                CodeMonkeys = codeMonkeys,
                MicroserviceSuperstars = microserviceSuperstars,
                Levels = levels
            });
            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            context.SetCustomStatus("Completed");
            return resultFileId;
        }

        [FunctionName("FileProcessor_ParseEngineers")]
        public static async Task<List<string>> ParseEngineers([ActivityTrigger] string fileId, ILogger log)
        {
            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            var container = storageAccount.CreateCloudBlobClient().GetContainerReference("uploaded-files");
            var blockBlob =  container.GetBlockBlobReference(fileId);

            log.LogInformation("Starting to parse the engineers.");
            await Task.Delay(3000);
            var engineers = (await blockBlob.DownloadTextAsync())
                .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                .ToList();

            log.LogInformation("Done parsing the engineers.");
            return engineers;
        }

        [FunctionName("FileProcessor_WhoIsACodeMonkey")]
        public static async Task<ICollection<string>> WhoIsACodeMonkey([OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var engineers = context.GetInput<List<string>>();
            // log.LogInformation("Starting to figure out which engineers are code monkeys.");
            
            var validationTasks = engineers
                .Select(engineer => context.CallActivityAsync<bool>("FileProcessor_WhoIsACodeMonkey_Each", engineer))
                .ToArray();

            await Task.WhenAll(validationTasks);

            var codeMonkeys = new HashSet<string>();
            for (var i = 0; i < engineers.Count; i++)
            {
                if (validationTasks[i].Result)
                {
                    codeMonkeys.Add(engineers[i]);
                }
            }

            // log.LogInformation("Done figuring out code monkeys.");
            return codeMonkeys;
        }

        [FunctionName("FileProcessor_WhoIsACodeMonkey_Each")]
        public static async Task<bool> WhoIsACodeMonkey_Each([ActivityTrigger] string engineer, ILogger log)
        {
            log.LogInformation($"Starting to validate {engineer}.");
            await Task.Delay(2000);
            log.LogInformation($"Done validating {engineer}.");
            var random = new Random();
            return random.Next(20) < 3;
        }

        [FunctionName("FileProcessor_WhoIsAMicroserviceSuperstar")]
        public static async Task<ICollection<string>> WhoIsAMicroserviceSuperstar([OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var engineers = context.GetInput<List<string>>();
            // log.LogInformation("Starting to figure out which engineers are code monkeys.");
            
            var validationTasks = engineers
                .Select(engineer => context.CallActivityAsync<bool>("FileProcessor_WhoIsAMicroserviceSuperstar_Each", engineer))
                .ToArray();

            await Task.WhenAll(validationTasks);

            var superstars = new HashSet<string>();
            for (var i = 0; i < engineers.Count; i++)
            {
                if (validationTasks[i].Result)
                {
                    superstars.Add(engineers[i]);
                }
            }

            // log.LogInformation("Done figuring out code monkeys.");
            return superstars;
        }

        [FunctionName("FileProcessor_WhoIsAMicroserviceSuperstar_Each")]
        public static async Task<bool> WhoIsAMicroserviceSuperstar_Each([ActivityTrigger] string engineer, ILogger log)
        {
            log.LogInformation($"Starting to validate {engineer}.");
            await Task.Delay(2000);
            log.LogInformation($"Done validating {engineer}.");
            var random = new Random();
            return random.Next(30) < 3;
        }

        [FunctionName("FileProcessor_CreateResultsFile")]
        public static async Task<string> CreateResultsFile([ActivityTrigger] FileProcessorResult result, ILogger log)
        {
            log.LogInformation("Starting to create results file.");
            var engineerList = result.AllEngineers.ToList();
            engineerList.Sort((e1, e2) =>
            {
                if (result.CodeMonkeys.Contains(e1) && !result.CodeMonkeys.Contains(e2) || result.MicroserviceSuperstars.Contains(e2) && !result.MicroserviceSuperstars.Contains(e1))
                {
                    return 1;
                }
                if (result.MicroserviceSuperstars.Contains(e1) && !result.MicroserviceSuperstars.Contains(e2) || result.CodeMonkeys.Contains(e2) && !result.CodeMonkeys.Contains(e1))
                {
                    return -1;
                }
                if (!result.CodeMonkeys.Contains(e1) && !result.MicroserviceSuperstars.Contains(e1) && result.CodeMonkeys.Contains(e2))
                {
                    return -1;
                }
                if (!result.CodeMonkeys.Contains(e2) && !result.MicroserviceSuperstars.Contains(e2) && result.CodeMonkeys.Contains(e1))
                {
                    return 1;
                }
                return result.Levels[e2].CompareTo(result.Levels[e1]);
            });
            result.AllEngineers = engineerList;
            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            var container = storageAccount.CreateCloudBlobClient().GetContainerReference("result-files");
            if (await container.CreateIfNotExistsAsync())
            {
                var permissions = new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                };
                await container.SetPermissionsAsync(permissions);
            }
            var resultFileId = Guid.NewGuid().ToString();
            var blockBlob =  container.GetBlockBlobReference($"{resultFileId}.txt");
            
            using (var memoryStream = new MemoryStream())
            {
                var writer = new StreamWriter(memoryStream);
                foreach (var engineer in result.AllEngineers)
                {
                    string engineerResult;
                    if (result.CodeMonkeys.Contains(engineer))
                    {
                        engineerResult = "code monkey";
                    }
                    else if (result.MicroserviceSuperstars.Contains(engineer))
                    {
                        engineerResult = "microservices superstar";
                    }
                    else
                    {
                        engineerResult = "normal engineer";
                    }
                    writer.WriteLine($"{engineer} is a level {result.Levels[engineer]} {engineerResult}.");
                    writer.Flush();
                }
                memoryStream.Seek(0, SeekOrigin.Begin);
                await blockBlob.UploadFromStreamAsync(memoryStream);
            }

            log.LogInformation("Done creating results file.");
            return $"{resultFileId}.txt";
        }

        [FunctionName("FileProcessor_GetLevels")]
        public static async Task<Dictionary<string, int>> GetLevels([OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var engineers = context.GetInput<List<string>>();
            var levels = new Dictionary<string, int>();
            var tasks = engineers
                .Select(engineer => context.CallActivityWithRetryAsync<int>("FileProcessor_GetLevels_Each", new RetryOptions(TimeSpan.FromMilliseconds(1000), 10), engineer))
                .ToArray();
            await Task.WhenAll(tasks);
            for (var i = 0; i < engineers.Count; i++)
            {
                if (!tasks[i].IsFaulted)
                {
                    levels.Add(engineers[i], tasks[i].Result);
                }
                else
                {
                    levels.Add(engineers[i], 0);
                }
            }
            return levels;
        }

        [FunctionName("FileProcessor_GetLevels_Each")]
        public static async Task<int> GetLevels_Each([ActivityTrigger] string engineer, ILogger log)
        {
            await Task.Delay(1000);
            var random = new Random();
            var val = random.Next(10);
            if (val < 6)
            {
                throw new Exception("Random exception!");
            }

            return random.Next(10) + 1;
        }



        [FunctionName("FileProcessor_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            var query = req.RequestUri.Query;
            var fileId = query.Split("=")[1];
            string instanceId = await starter.StartNewAsync("FileProcessor", fileId);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}