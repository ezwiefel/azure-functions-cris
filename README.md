# Azure Function CRIS Service Call

Azure functions repo to enable webhook to CRIS long dictation mode

Using this method will transcribe files up to 12 minutes in duration.

The function is a REST api that accepts a JSON message pointing to a file stored in Azure Blob Storage.

_Sample Input JSON:_
```json
{
    "fileName": "voicemail-34.wav",
    "containerName": "voicemails"
}
```

_Sample Output JSON:_
```json
{
    "fileName": "voicemail-34.wav",
    "audioText":"Hey Chris over here at Napa shop just giving you a call to let you know, I got your wheel bearing all done for the Honda Odyssey, you can pick it up anytime thanks bye. "
}
```

## Setup Instructions

### Step 0: Fork this Repo

### Step 1: Create CRIS Model
Go to [cris.ai](http://www.cris.ai) to create an account and a new trained model.

You'll need the recognition URL and the primary and secondary keys.

### Step 1: Create Azure Function

### Step 2: Deploy from Git Repo
In your Azure Function you can deploy the code straight from Github (and the app will update when new code is checked in.)

To do this, go to Platform Features > Deployment Options > Setup > Choose Source > Github and then click on Refresh

### Step 3: Set environment variables
In platform features, select Application Settings and enter the following environment variables:

1. __storage_account_name__ - The storage account name of the Azure Blob Storage account where the audio files will be stored.
1. __storage_account_key__ - The storage key for the Azure Blob Storage account
1. __cris_primary_key__ - Primary key for CRIS subscription
1. __cris_secondary_key__ - Secondary key for CRIS subscription
    ![Alt text](/images/keys.png?raw=true "CRIS Storage Keys")    

1. __cris_recog_url__ - Copy the URL from the deployed CRIS model (see yellow highlighted section below)

    ![Alt text](/images/url.png?raw=true "URL for Recognition")

## Considerations / Disclaimers
Currently, every function call logs the transcribed text. If this is an issue for your use-case look for the following code and remove it.

```c#
log.Info(logStart + $"Transcribed: {audioText}");
```
