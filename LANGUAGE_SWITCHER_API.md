# Language Switcher API Documentation

## Overview
The Patient Tracker API now includes a language switcher feature that allows users to set their preferred language (English or Arabic) and receive localized responses.

## Available Languages
- **English (en-US)** - Default language
- **Arabic (ar-SA)** - Right-to-left language support

## API Endpoints

### Update Language Preference
Updates the authenticated user's language preference.

**Endpoint:** `PATCH /api/profile/language`

**Authentication:** Required (Bearer token)

**Request Body:**
```json
{
  "languagePreference": "en-US"  // or "ar-SA"
}
```

**Response (Success):**
```json
{
  "message": "Language preference updated successfully",
  "languagePreference": "en-US"
}
```

**Response (Error - Unsupported Language):**
```json
{
  "error": "Unsupported language preference"
}
```

## Authentication Response
The language preference is included in the authentication response when users log in, register, or refresh tokens:

```json
{
  "accessToken": "...",
  "refreshToken": "...",
  "user": {
    "id": 1,
    "email": "user@example.com",
    "fullName": "John Doe",
    "createdAt": "2024-01-01T00:00:00Z",
    "languagePreference": "en-US"
  }
}
```

## How to Use

### 1. Frontend Language Switcher Implementation
```javascript
// Example: Update user language preference
async function updateLanguagePreference(language) {
  const response = await fetch('/api/profile/language', {
    method: 'PATCH',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({ languagePreference: language })
  });
  
  if (response.ok) {
    // Update UI language
    // Store preference in localStorage/state
    // Update Accept-Language header for future requests
  }
}
```

### 2. Requesting Localized Responses
Include the `Accept-Language` header in all API requests:

```javascript
// Example: Set Accept-Language header based on user preference
const headers = {
  'Accept-Language': userLanguagePreference, // 'en-US' or 'ar-SA'
  'Authorization': `Bearer ${token}`
};

const response = await fetch('/api/medications', { headers });
```

## Database Schema
The `Users` table now includes a `LanguagePreference` column:
- Column: `LanguagePreference`
- Type: `nvarchar(10)`
- Default: `'en-US'`
- Nullable: Yes

## Error Messages
All error messages are localized based on the `Accept-Language` header:

### English Examples:
- `"Profile not found"`
- `"Language preference updated successfully"`

### Arabic Examples:
- `"الملف الشخصي غير موجود"`
- `"تم تحديث تفضيل اللغة بنجاح"`

## Implementation Details

### Backend Changes:
1. **User Entity**: Added `LanguagePreference` property
2. **ProfileService**: Added `UpdateLanguagePreferenceAsync` method
3. **ProfileController**: Added `PATCH /api/profile/language` endpoint
4. **AuthResponse**: Updated `UserDto` to include `LanguagePreference`
5. **AuthService**: Returns language preference in all auth responses

### Localization Resources:
- `ErrorMessages.resx` - English messages
- `ErrorMessages.ar.resx` - Arabic messages

## Best Practices
1. Store the user's language preference in the frontend state/localStorage
2. Always include the `Accept-Language` header with the user's preference
3. Initialize the UI language immediately after login using the preference from auth response
4. Fall back to English if a translation is missing

## Testing
To test the language switcher:
1. Login/register a user
2. Note the `languagePreference` in the response
3. Call `PATCH /api/profile/language` with a different language
4. Make subsequent API requests with the appropriate `Accept-Language` header
5. Verify responses are in the requested language
