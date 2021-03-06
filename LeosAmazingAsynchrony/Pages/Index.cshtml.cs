﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace LeosAmazingAsynchrony.Pages
{
    public class IndexModel : PageModel
    {
        private readonly string _functionsUrl;
        private readonly CloudBlobContainer _cloudBlobContainer;

        public IndexModel(IOptions<AppSettings> options)
        {
            _functionsUrl = options.Value.FunctionsAppUrl;
            var account = CloudStorageAccount.Parse(options.Value.ConnectionStrings.StorageAccountConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            _cloudBlobContainer = blobClient.GetContainerReference("uploaded-files");
        }

        public void OnGet()
        {
            var orchestrationId = (string)TempData.Peek("cur-orchestration-id");
            if (!string.IsNullOrEmpty(orchestrationId))
            {
                var orchestrationResult = new OrchestrationStartResult
                {
                    Id = Guid.Parse(orchestrationId),
                    StatusQueryGetUri = $"{_functionsUrl}/runtime/webhooks/durabletask/instances/{orchestrationId}?taskHub=DurableFunctionsHub&connection=Storage"
                };
                ViewData.Add("OrchestrationResult", orchestrationResult);
            }
        }

        [BindProperty]
        public IFormFile Upload { get; set; }
        public async Task OnPostAsync()
        {
            if (Upload != null)
            {
                var uniqueId = Guid.NewGuid();
                if (await _cloudBlobContainer.CreateIfNotExistsAsync())
                {
                    var permissions = new BlobContainerPermissions
                    {
                        PublicAccess = BlobContainerPublicAccessType.Blob
                    };
                    await _cloudBlobContainer.SetPermissionsAsync(permissions);
                };
                var blockBlob = _cloudBlobContainer.GetBlockBlobReference(uniqueId.ToString());
                using (var stream = Upload.OpenReadStream())
                {
                    await blockBlob.UploadFromStreamAsync(stream);
                }
                using (var client = new HttpClient())
                {
                    var result = await client.GetAsync($"{_functionsUrl}/api/FileProcessor_HttpStart?fileId={uniqueId}");
                    var orchestrationStartResult = await result.Content.ReadAsAsync<OrchestrationStartResult>();
                    ViewData.Add("OrchestrationResult", orchestrationStartResult);
                    TempData.Add("cur-orchestration-id", orchestrationStartResult.Id.ToString("N"));
                }
                ViewData.Add("FileName", Upload.FileName);
            }
        }
    }
}
