#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"
#load "helpers.csx"
#load "audio-handler.csx"

using System;
using System.Net;
using Microsoft.CognitiveServices.SpeechRecognition;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;

public static async Task<object> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info($"[stt-cris] request received");

    string jsonContent = await req.Content.ReadAsStringAsync();
    dynamic data = JsonConvert.DeserializeObject(jsonContent);

    string logStart = $"[{data.fileName}] - ";
    
    // Create Azure Storage objects
    var storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(AzureStorageAccount.ConnectionString);
    var blobClient = storageAccount.CreateCloudBlobClient();
    var container = blobClient.GetContainerReference($"{data.containerName}");
    
    // If required parameters aren't included, return BadRequest
    if (data.fileName == null || data.containerName == null) {
        return req.CreateResponse(HttpStatusCode.BadRequest, new {
            error = "Please pass fileName and blobPath properties in the input object"
        });
    }

    log.Info(logStart + "Processing");

    // Create Audio Handler and get transcribed text
    var audioHandler = new AudioHandler(log: log, logId: logStart);
    
    var audioText = await audioHandler.ProcessBlob(container, $"{data.fileName}");
    log.Info(logStart + $"Transcribed: {audioText}");

    // Return results JSON
    return req.CreateResponse(HttpStatusCode.OK, new {
        audio_text = $"{audioText}",
        file_name = $"{data.fileName}"
    });
}
