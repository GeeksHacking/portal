# Participant Review Email System with Postmark Integration

## Overview
This feature allows organizers to automatically send emails to participants when they are accepted or rejected during the review process. It also provides a batch email endpoint for manual email sending. Emails are sent using Postmark templates for easy customization. **Template IDs are configured per hackathon in the database**, allowing different hackathons to use different email templates.

The email system is **fully extensible** - templates can reference a comprehensive set of context-specific variables including participant information, hackathon details, event dates, and more.

## Configuration

### Global Postmark Configuration

Add the following configuration to your `appsettings.json` or environment variables:

```json
{
  "Postmark": {
    "ServerToken": "your-postmark-server-token",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "HackOMania",
    "Enabled": true
  }
}
```

### Global Configuration Options

- **ServerToken**: Your Postmark API server token (required)
- **FromEmail**: The sender email address (required)
- **FromName**: The sender name that appears in emails (required)
- **Enabled**: Set to `false` to disable email sending globally (useful for testing)

### Per-Hackathon Email Template Configuration

Each hackathon can configure a flexible map of event keys to template IDs:

- **emailTemplates**: dictionary where key is an event key and value is a template ID/alias

This can be set when creating or updating a hackathon. If an event key is not configured, emails for that event are skipped.

**Example:**
```json
{
  "name": "HackOMania 2026",
  "emailTemplates": {
    "participant.review.accepted": "participant-accepted",
    "participant.review.rejected": "participant-rejected"
  }
}
```

## Postmark Template Setup

You need to create two templates in your Postmark account. The email system provides a comprehensive set of context-specific variables that can be referenced in your templates.

### Available Template Variables

All email templates have access to the following variables:

#### Participant Information
- `participant_name` - Full name of the participant
- `participant_first_name` - First name only
- `participant_last_name` - Last name only
- `participant_email` - Participant's email address
- `participant_id` - Unique participant ID (GUID)
- `user_id` - User ID (GUID)

#### Hackathon Information
- `hackathon_name` - Name of the hackathon
- `hackathon_id` - Unique hackathon ID (GUID)
- `hackathon_short_code` - Short code for the hackathon
- `hackathon_venue` - Event venue
- `hackathon_description` - Full description
- `hackathon_homepage_url` - Homepage URL

#### Event Dates
- `event_start_date` - Start date (yyyy-MM-dd format)
- `event_end_date` - End date (yyyy-MM-dd format)
- `event_start_date_formatted` - Start date (MMMM dd, yyyy format)
- `event_end_date_formatted` - End date (MMMM dd, yyyy format)

#### Submission Dates
- `submissions_start_date` - Submissions start (yyyy-MM-dd format)
- `submissions_end_date` - Submissions end (yyyy-MM-dd format)
- `submissions_start_date_formatted` - Submissions start (MMMM dd, yyyy format)
- `submissions_end_date_formatted` - Submissions end (MMMM dd, yyyy format)

#### Review Information
- `reason` - Optional message/reason from organizers (empty string if not provided)
- `has_reason` - Boolean indicating if a reason was provided
- `review_status` - Status of the review (Accepted/Rejected)

#### Participant Metadata
- `joined_at` - Date participant joined (yyyy-MM-dd format)
- `joined_at_formatted` - Date participant joined (MMMM dd, yyyy format)

### Acceptance Email Template Example

```html
<h1>Congratulations, {{participant_first_name}}!</h1>
<p>Dear {{participant_name}},</p>
<p>We are thrilled to inform you that your application for <strong>{{hackathon_name}}</strong> has been <strong>accepted</strong>!</p>

{{#has_reason}}
<div class="organizer-message">
  <p><strong>Message from organizers:</strong></p>
  <p>{{reason}}</p>
</div>
{{/has_reason}}

<div class="event-details">
  <h2>Event Details</h2>
  <ul>
    <li><strong>Event:</strong> {{hackathon_name}}</li>
    <li><strong>Venue:</strong> {{hackathon_venue}}</li>
    <li><strong>Dates:</strong> {{event_start_date_formatted}} - {{event_end_date_formatted}}</li>
    <li><strong>Submissions:</strong> {{submissions_start_date_formatted}} - {{submissions_end_date_formatted}}</li>
  </ul>
  <p><a href="{{hackathon_homepage_url}}">View Event Homepage</a></p>
</div>

<p>We look forward to seeing you at the event!</p>
<p>Best regards,<br/>The {{hackathon_name}} Team</p>
```

### Rejection Email Template Example

```html
<h1>Application Update</h1>
<p>Dear {{participant_name}},</p>
<p>Thank you for your interest in <strong>{{hackathon_name}}</strong>. After careful consideration, we regret to inform you that we are unable to accept your application at this time.</p>

{{#has_reason}}
<div class="reason-box">
  <p><strong>Feedback:</strong></p>
  <p>{{reason}}</p>
</div>
{{/has_reason}}

<p>We appreciate your interest and encourage you to apply for future events. Stay connected with us at <a href="{{hackathon_homepage_url}}">{{hackathon_homepage_url}}</a>.</p>

<p>Best regards,<br/>The {{hackathon_name}} Team</p>
```

## Features

### 1. Automatic Review Emails

When an organizer reviews a participant (accept or reject), an email is sent directly from the API using the configured Postmark template for that hackathon:

**API Endpoint**: `POST /organizers/hackathons/{hackathonId}/participants/{participantUserId}/review`

**Request Body**:
```json
{
  "decision": "accept",  // or "reject"
  "reason": "Optional message to the participant"
}
```

The API flow will:
- Send an acceptance email using the hackathon's configured acceptance template if decision is "accept"
- Send a rejection email using the hackathon's configured rejection template if decision is "reject"
- Pass all available context variables to the email template (participant info, hackathon details, event dates, etc.)
- Include the optional reason message in the template variables
- Log warnings and skip sending if no template is configured for that hackathon
- Log errors but not fail the review process if email sending fails

No Pub/Sub infrastructure is required for this flow.

**All available template variables are automatically included in the email**, allowing you to create rich, personalized email templates.

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

**Note:** The batch email endpoint also passes all available context variables to each email template, ensuring rich personalization for every recipient.

## Security

- Template variables are automatically escaped by Postmark to prevent XSS
- Email sending is fault-tolerant - failures are logged but don't break the review process
- Batch operations continue even if individual emails fail
- Hackathons without configured templates will skip email sending with a warning log

## Testing

For local development or testing, you can disable email sending globally:

```json
{
  "Postmark": {
    "ServerToken": "test-token",
    "FromEmail": "test@example.com",
    "FromName": "Test",
    "Enabled": false
  }
}
```

When `Enabled` is `false`, the system will log email attempts but not send actual emails.

Alternatively, you can leave specific hackathons without template IDs configured, and they will skip email sending.

## Error Handling

- Email sending errors are logged but don't prevent participant reviews
- Batch operations are fault-tolerant - one failure won't stop others
- All errors are logged with participant email addresses for debugging
- Missing or invalid template IDs will result in warnings or Postmark API errors that are logged
- Hackathons without configured templates will log a warning and skip email sending

## Requirements

- Postmark account with valid server token
- Verified sender email address in Postmark
- Email templates created in Postmark for each hackathon that needs email notifications
- .NET 10.0 or higher
- Postmark NuGet package v5.3.0 or higher

## Database Schema

The `HackathonNotificationTemplate` table stores email template mappings:

```sql
Id UNIQUEIDENTIFIER PRIMARY KEY
HackathonId UNIQUEIDENTIFIER NOT NULL
EventKey NVARCHAR(128) NOT NULL
TemplateId NVARCHAR(256) NOT NULL
```

These can be managed via the hackathon creation/update API endpoints (`emailTemplates`).
