# Xlent API

## What it does
- Make meeting summaries from transcripts.
- Transcribe audio fast.

## Run the project
- Open the `XlentApi` project in Visual Studio.
- Start with the Run/Debug button (F5).
- The IDE shows the local URL (see `Properties/launchSettings.json`).

## Configuration
- Check `appsettings.json` for Azure settings (`AzureOpenAI`, `AzureSpeechToText`, `AzureTextPIIRedaction`).
- Add API keys there or in your IDE if needed.

## API Endpoints

- POST `api/summary`
  - Takes a `MeetingSummaryRequest` with `transcript` (list of segments) and optional `language`.
  - Example request:

```json
{
  "transcript": [
    {
      "text": "Hello everyone",
      "speakerId": "1",
      "startOffsetSeconds": 0.0,
      "durationSeconds": 1.2,
      "isFinal": true
    }
  ],
  "language": "en-US"
}
```

  - Example response:

```json
{
  "summary": "Short summary",
  "actionItems": ["Do X"]
}
```

- POST `api/transcription/upload-fast`
  - Upload one audio file using form-data field `file` (Postman or IDE tools).
  - Optional query params: `languages`, `customPhrases`, `expectedSpeakers`.
  - Example response:

```json
{
  "fullText": "Transcribed full text here",
  "segments": [
    {
      "speakerId": 1,
      "text": "Hello",
      "offsetMilliseconds": 0,
      "durationMilliseconds": 1200
    }
  ]
}
```

## Testing
- Use Postman or your IDE to call the endpoints and upload files.

## Troubleshooting
- Check `Properties/launchSettings.json` for the local URL.
- Check `appsettings.json` for missing keys.
- Restart the project in Visual Studio if needed.
