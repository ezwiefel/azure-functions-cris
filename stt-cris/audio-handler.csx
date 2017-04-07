#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"
#load "helpers.csx"

using System;
using System.Net;
using Microsoft.CognitiveServices.SpeechRecognition;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;

///<summary>
/// Wrapper class for Cognitive Services
/// Thanks to JeffBrand for the inspiration
/// Based on https://gist.github.com/JeffBrand/93843d5583ce44cfc3319ed5f17e324b
/// </summary>
public class AudioHandler
{
    // Allow logging outside of main function
    public TraceWriter log;
    
    TaskCompletionSource<string> _tcs = new TaskCompletionSource<string>();
    
    private string _logStart;
    private DataRecognitionClient _dataClient;
    private string _interimResults;

    public AudioHandler(TraceWriter log, string logId)
    {
        this.log = log;
        this._logStart = logId;
        // Create dataClient object to get the 
        _dataClient = SpeechRecognitionServiceFactory.CreateDataClient(
                   SpeechRecognitionMode.LongDictation,
                   "en-US",
                   GetEnvironmentVariable("cris_primary_key"),
                   GetEnvironmentVariable("cris_secondary_key"),
                   GetEnvironmentVariable("cris_recog_url"));
        
        // Since we're using CRIS, we need to overwrite the AuthenticationUri
        _dataClient.AuthenticationUri = "https://westus.api.cognitive.microsoft.com/sts/v1.0/issueToken";

        // Add handlers to the OnResponseReceived and OnConversationError events
        _dataClient.OnResponseReceived += responseHandler;
        _dataClient.OnConversationError += errorHandler;
    }

    // ErrorHandler returns error codes and text via JSON
    private void errorHandler(object sender, SpeechErrorEventArgs args)
    {
        log.Info($"ERROR CODE: {args.SpeechErrorCode} TEXT: {args.SpeechErrorText}");
        _tcs.SetResult($"ERROR CODE: {args.SpeechErrorCode} TEXT: {args.SpeechErrorText}");
    }

    // ResponseHandler will get results until the EndOfDictation or DictationEndSilenceTimeout
    private void responseHandler(object sender, SpeechResponseEventArgs args)
    {

        log.Info(_logStart + "Response received");
        // Check for RecognitionStatus - if recognition reached end of audio, return results
        if (args.PhraseResponse.RecognitionStatus == RecognitionStatus.EndOfDictation ||
            args.PhraseResponse.RecognitionStatus == RecognitionStatus.DictationEndSilenceTimeout)
        {
            if (_interimResults.Length == 0)
            {
                // If no text was returned, return an Error Message
                _tcs.SetResult("ERROR: Bad audio");
            }
            else
            {
                // If the _interimResults have length, then return those results
                log.Info(_logStart + $"{args.PhraseResponse.RecognitionStatus}");
                _tcs.SetResult(_interimResults);
                var client = sender as DataRecognitionClient;
                client.OnResponseReceived -= responseHandler;
            }
        }
        // If RecognationStatus did not reach the end of the audio, then add the results to _interimResults
        else
        {
            // If the result received was not 0 length, then append the text
            if (args.PhraseResponse.Results.Length != 0)
            {
                _interimResults += args.PhraseResponse.Results[0].DisplayText + " ";
            }
            
        }
    }

    public Task<string> ProcessBlob(Microsoft.WindowsAzure.Storage.Blob.CloudBlobContainer container, string blobName)
    {
        
        // Create MemorySteam object
        var mem = new System.IO.MemoryStream();
        log.Info(_logStart + "Starting to read blob");
        
        // Download the blob to the memory stream
        var blockBlob = container.GetBlockBlobReference(blobName);
        blockBlob.DownloadToStream(mem);
        
        log.Info(_logStart + "Blob read - size=" + mem.Length);
        
        mem.Position = 0;

        
        int bytesRead = 0;
        byte[] buffer = new byte[1024];

        try
        {
            do
            {
                // Send chuncks of audio to CRIS
                bytesRead = mem.Read(buffer, 0, buffer.Length);
                _dataClient.SendAudio(buffer, bytesRead);
            }
            while (bytesRead > 0);
            log.Info(_logStart + "Blob reading finished");

        }
        finally
        {
            // After data sent, signal Audio End
            _dataClient.EndAudio();
            log.Info(_logStart + "Audio sent to CRIS");

        }

        return _tcs.Task;
    }
}
