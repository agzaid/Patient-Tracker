# Localization Guide

## Overview
The Patient Tracker API supports localization for English (en-US) and Arabic (ar-SA) languages.

## How it Works
- Error messages are stored in resource files in the `PatientTracker.Application/Resources` directory
- `ErrorMessages.resx` contains the default (English) messages
- `ErrorMessages.ar.resx` contains Arabic translations
- Controllers and services use `IStringLocalizer<ErrorMessages>` to get localized messages

## Supported Languages
- **English (en-US)** - Default language
- **Arabic (ar-SA)** - Right-to-left language support

## How to Request Localized Responses

Clients can request localized responses by including the `Accept-Language` header in their API requests:

### For English:
```
Accept-Language: en-US
```

### For Arabic:
```
Accept-Language: ar-SA
```

## Example API Response

### English Response:
```json
{
  "error": "Profile not found"
}
```

### Arabic Response:
```json
{
  "error": "الملف الشخصي غير موجود"
}
```

## Adding New Localized Messages

1. Add the message to `ErrorMessages.resx` (English):
```xml
<data name="NewMessageKey" xml:space="preserve">
  <value>English message</value>
</data>
```

2. Add the translation to `ErrorMessages.ar.resx` (Arabic):
```xml
<data name="NewMessageKey" xml:space="preserve">
  <value>الرسالة العربية</value>
</data>
```

3. Use in code:
```csharp
return NotFound(new { error = _localizer["NewMessageKey"] });
```

## Adding New Languages

1. Create a new resource file with the appropriate culture code (e.g., `ErrorMessages.fr.resx` for French)
2. Add the culture code to the supported cultures array in `Program.cs`:
```csharp
var supportedCultures = new[] { "en-US", "ar-SA", "fr-FR" };
```
3. Translate all messages in the new resource file

## Notes
- The API automatically falls back to English if a translation is missing
- Culture-specific formatting (dates, numbers) is handled automatically
- JSON responses remain in standard format; only the message content is localized
