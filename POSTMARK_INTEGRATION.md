# Hook/Event System with Postmark Email Integration

## Overview
This feature allows organizers to automatically send emails to participants when they are accepted or rejected during the review process. It also provides a batch email endpoint for manual email sending. Emails are sent using Postmark templates for easy customization.

## Configuration

Add the following configuration to your `appsettings.json` or environment variables:

```json
{
  "Postmark": {
    "ServerToken": "your-postmark-server-token",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "HackOMania",
    "Enabled": true,
    "AcceptedTemplateId": "participant-accepted",
    "RejectedTemplateId": "participant-rejected"
  }
}
```

### Configuration Options

- **ServerToken**: Your Postmark API server token (required)
- **FromEmail**: The sender email address (required)
- **FromName**: The sender name that appears in emails (required)
- **Enabled**: Set to `false` to disable email sending (useful for testing)
- **AcceptedTemplateId**: Template ID (numeric) or template alias (string) for acceptance emails
- **RejectedTemplateId**: Template ID (numeric) or template alias (string) for rejection emails

## Postmark Template Setup

You need to create two templates in your Postmark account:

### Acceptance Email Template

**Template Variables:**
- `participant_name` - The name of the participant
- `hackathon_name` - The name of the hackathon
- `reason` - Optional message from organizers (can be empty)
- `has_reason` - Boolean indicating if a reason was provided

**Example Template:**
```html
<h1>Application Accepted!</h1>
<p>Dear {{participant_name}},</p>
<p>We are thrilled to inform you that your application for <strong>{{hackathon_name}}</strong> has been accepted!</p>
{{#has_reason}}
<p><strong>Message from organizers:</strong><br/>{{reason}}</p>
{{/has_reason}}
<p>We look forward to seeing you at the event!</p>
```

### Rejection Email Template

**Template Variables:**
- `participant_name` - The name of the participant
- `hackathon_name` - The name of the hackathon
- `reason` - Optional reason for rejection (can be empty)
- `has_reason` - Boolean indicating if a reason was provided

**Example Template:**
```html
<h1>Application Update</h1>
<p>Dear {{participant_name}},</p>
<p>Thank you for your interest in <strong>{{hackathon_name}}</strong>. After careful consideration, we regret to inform you that we are unable to accept your application at this time.</p>
{{#has_reason}}
<p><strong>Reason:</strong><br/>{{reason}}</p>
{{/has_reason}}
<p>We appreciate your interest and encourage you to apply for future events.</p>
```

## Features

### 1. Automatic Email Hooks

When an organizer reviews a participant (accept or reject), an email is automatically sent to the participant using the configured Postmark template:

**API Endpoint**: `POST /organizers/hackathons/{hackathonId}/participants/{participantUserId}/review`

**Request Body**:
```json
{
  "decision": "accept",  // or "reject"
  "reason": "Optional message to the participant"
}
```

The hook will:
- Send an acceptance email using the configured template if decision is "accept"
- Send a rejection email using the configured template if decision is "reject"
- Include the optional reason message in the template variables
- Log errors but not fail the review process if email sending fails

### 2. Batch Email Sending

Organizers can manually send emails to multiple participants at once:

**API Endpoint**: `POST /organizers/hackathons/{hackathonId}/participants/batch-email`

**Request Body**:
```json
{
  "status": "All",  // Options: "All", "Accepted", "Rejected"
  "participantUserIds": []  // Optional: specific participant IDs to target
}
```

**Response**:
```json
{
  "totalEmailsSent": 10,
  "acceptedEmailsSent": 7,
  "rejectedEmailsSent": 3,
  "errors": []
}
```

#### Use Cases:
- Send emails to all accepted participants: `{ "status": "Accepted" }`
- Send emails to all rejected participants: `{ "status": "Rejected" }`
- Resend emails to specific participants: `{ "participantUserIds": ["guid1", "guid2"] }`
- Send emails to all reviewed participants: `{ "status": "All" }`

## Template Variables

Both templates receive the following variables that can be used with Postmark's Mustachio template syntax:

| Variable | Type | Description |
|----------|------|-------------|
| `participant_name` | string | Name of the participant |
| `hackathon_name` | string | Name of the hackathon |
| `reason` | string | Optional message/reason (empty string if not provided) |
| `has_reason` | boolean | True if a reason was provided, false otherwise |

Use `{{#has_reason}}...{{/has_reason}}` to conditionally show content when a reason is provided.

## Security

- Template variables are automatically escaped by Postmark to prevent XSS
- Email sending is fault-tolerant - failures are logged but don't break the review process
- Batch operations continue even if individual emails fail

## Testing

For local development or testing, you can disable email sending:

```json
{
  "Postmark": {
    "ServerToken": "test-token",
    "FromEmail": "test@example.com",
    "FromName": "Test",
    "Enabled": false,
    "AcceptedTemplateId": "test-accepted",
    "RejectedTemplateId": "test-rejected"
  }
}
```

When `Enabled` is `false`, the system will log email attempts but not send actual emails.

## Error Handling

- Email sending errors are logged but don't prevent participant reviews
- Batch operations are fault-tolerant - one failure won't stop others
- All errors are logged with participant email addresses for debugging
- Missing or invalid template IDs will result in Postmark API errors that are logged

## Requirements

- Postmark account with valid server token
- Verified sender email address in Postmark
- Two email templates created in Postmark (acceptance and rejection)
- .NET 10.0 or higher
- Postmark NuGet package v5.3.0 or higher
