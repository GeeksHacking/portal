# Caching Strategy & Implementation Guide

## Overview

The HackOMania Event Platform uses a dual-layer caching system to improve performance and reduce database load. This document outlines the caching strategy, implementation details, and potential cache invalidation concerns.

## Architecture

### Caching Layers

1. **Primary Cache: Redis**
   - Implementation: `SqlSugarRedisCache` service
   - Uses StackExchange.Redis via Aspire integration
   - JSON serialization for cache entries
   - Keys follow pattern: `SqlSugarDataCache.*`

2. **Fallback Cache: NoOpCacheService**
   - Activated when Redis is unavailable during startup
   - Gracefully degrades to no caching
   - Prevents application startup failures

### Automatic Cache Invalidation

The application is configured with **automatic cache invalidation** enabled:

```csharp
MoreSettings = new ConnMoreSettings { IsAutoRemoveDataCache = true }
```

This means that SqlSugar automatically invalidates cached queries when:
- INSERT operations are performed on a table
- UPDATE operations are performed on a table
- DELETE operations are performed on a table

**How it works:**
- When you call `.WithCache()` on a query, SqlSugar caches the result
- When you insert/update/delete data in any table, ALL cache entries for that table are automatically removed
- No manual cache invalidation is needed in most cases

## Cached Endpoints

### Currently Cached Endpoints (29 total)

#### Auth & User Profile
- ~~`GET /auth/whoami`~~ - Removed (uses navigation properties)
- `GET /users/me` - User profile

#### Hackathons (Organizers)
- `GET /organizers/hackathons` - List organizer hackathons
- `GET /organizers/hackathons/{id}` - Get hackathon details
- `GET /organizers/hackathons/{id}/organizers` - List organizers

#### Challenges (Organizers & Participants)
- `GET /organizers/hackathons/{id}/challenges` - List challenges
- `GET /organizers/hackathons/{id}/challenges/{id}` - Get challenge details
- `GET /participants/hackathons/{id}/challenges` - List challenges (participant view)
- `GET /participants/hackathons/{id}/challenges/{id}` - Get challenge details (participant view)

#### Judges
- `GET /organizers/hackathons/{id}/judges` - List judges
- `GET /organizers/hackathons/{id}/judges/{id}` - Get judge details

#### Workshops
- `GET /organizers/hackathons/{id}/workshops` - List workshops
- `GET /participants/hackathons/{id}/workshops` - List workshops (participant view)

#### Registration Questions
- `GET /organizers/hackathons/{id}/registration/questions` - List registration questions
- `GET /participants/hackathons/{id}/registration/questions` - List registration questions (participant view)
- ~~`GET /participants/hackathons/{id}/registration/submissions`~~ - Removed (uses navigation properties)

#### Resources
- `GET /organizers/hackathons/{id}/resources` - List resources
- `GET /participants/hackathons/{id}/resources` - List resources (participant view)

#### Timeline
- `GET /participants/hackathons/{id}/timeline` - List timeline events

#### Teams & Submissions
- `GET /organizers/hackathons/{id}/teams` - List teams
- `GET /organizers/hackathons/{id}/submissions` - List submissions
- `GET /organizers/hackathons/{id}/submissions/{id}` - Get submission details
- `GET /participants/hackathons/{id}/teams/mine` - Get user's team
- `GET /participants/hackathons/{id}/submissions` - List submissions (participant view)
- `GET /participants/hackathons/{id}/submissions/{id}` - Get submission details (participant view)

#### Participants
- `GET /organizers/hackathons/{id}/participants` - List all participants with review status
- `GET /participants/hackathons/{id}/status` - Get user's participation status

#### Venue
- `GET /organizers/hackathons/{id}/venue/overview` - Get check-in overview

#### Public Hackathons
- `GET /participants/hackathons` - List published hackathons
- `GET /participants/hackathons/{id}` - Get hackathon details

## Cache Invalidation Concerns

### ⚠️ High-Frequency Update Endpoints

The following endpoints involve data that changes frequently and may cause cache thrashing:

#### 1. **Venue Check-In/Check-Out** ⚠️ POTENTIAL INVALIDATION ISSUE
- `POST /organizers/hackathons/{id}/venue/checkin`
- `POST /participants/hackathons/{id}/venue/checkout`
- **Issue**: Each check-in/check-out invalidates `VenueCheckIn` cache
- **Impact**: `GET /organizers/hackathons/{id}/venue/overview` cache becomes stale immediately
- **Frequency**: High during event hours
- **Recommendation**: 
  - Consider short TTL (5-30 seconds) for venue overview queries
  - OR remove caching from venue endpoints during active event periods
  - OR use more granular cache keys based on time windows

#### 2. **Participant Reviews** ⚠️ POTENTIAL INVALIDATION ISSUE
- `POST /organizers/hackathons/{id}/participants/{id}/review`
- **Issue**: Each review invalidates `ParticipantReview` cache
- **Impact**: Affects participant list and status endpoints
- **Frequency**: Moderate during registration review periods
- **Recommendation**: Acceptable - reviews happen in bursts but not continuously

#### 3. **Team Operations** ⚠️ POTENTIAL INVALIDATION ISSUE
- `POST /participants/hackathons/{id}/teams` - Create team
- `POST /participants/hackathons/{id}/teams/join` - Join team
- `POST /participants/hackathons/{id}/teams/leave` - Leave team
- `DELETE /participants/hackathons/{id}/teams/members/{id}` - Remove member
- `PATCH /participants/hackathons/{id}/teams/{id}` - Update team
- `PUT /participants/hackathons/{id}/teams/{id}/challenge` - Select challenge
- `POST /participants/teams/join` - Join team by code
- **Issue**: Team changes invalidate both `Team` and `Participant` caches
- **Impact**: Affects team list, team details, and participant queries
- **Frequency**: High during team formation periods, but infrequent after teams are established
- **Mitigation**: Read queries within these endpoints are now cached since teams are not frequently mutated
- **Recommendation**: 
  - Automatic cache invalidation handles write operations correctly
  - Caching validation queries improves performance during team operations
  - Monitor cache hit/miss ratio during team formation periods

#### 4. **Registration Submissions** ⚠️ POTENTIAL INVALIDATION ISSUE
- `POST /participants/hackathons/{id}/registration/submissions/submit`
- **Issue**: Each submission invalidates `ParticipantRegistrationSubmission` cache
- **Impact**: User's registration submission list becomes stale
- **Frequency**: High during registration periods
- **Recommendation**: User-specific cache keys already provide isolation

### ✅ Low-Risk Cached Endpoints

These endpoints cache data that changes infrequently:

- Hackathon metadata (name, description, dates)
- Challenges (created once, rarely modified)
- Workshops (created once, rarely modified)
- Registration questions (set up once)
- Resources (created periodically)
- Timeline events (created periodically)
- Judges (added occasionally)

## Best Practices

### When to Add `.WithCache()`

✅ **DO cache when:**
- Data is read frequently
- Data changes infrequently
- Query is expensive (multiple joins, aggregations)
- Stale data for a few seconds is acceptable

❌ **DON'T cache when:**
- Data must be real-time accurate
- Data changes on every request
- Query is simple and fast
- User-specific data that varies per request (unless cache key includes user ID)
- **Query uses `.Includes()` for navigation properties** - SqlSugar caching doesn't support navigation property serialization

### Cache Key Considerations

SqlSugar automatically generates cache keys based on:
- SQL query text
- Query parameters
- Table names

**User-Specific Data**: Queries with `WHERE userId = {userId}` get unique cache keys per user, providing natural isolation.

**Navigation Properties**: Queries using `.Includes()` for navigation properties should **NOT** use `.WithCache()` as SqlSugar's caching doesn't properly serialize/deserialize navigation properties. Load related entities separately or use explicit joins instead.

### Performance Monitoring

Monitor these metrics to identify cache effectiveness:
- Redis hit/miss ratio
- Cache entry eviction rate
- Query response times
- Database load during peak periods

## Migration Notes

### What Changed

#### Initial Implementation (Commits 1-3)
Added `.WithCache()` to 9 previously uncached GET endpoints (2 removed due to navigation property issues):
1. ~~Auth/WhoAmI~~ - Removed (uses `.Includes()` for navigation properties)
2. Users/Profile/Get
3. Organizers/Hackathon/Challenges/Get
4. Organizers/Hackathon/Judges/Get
5. Organizers/Hackathon/Submissions/Get
6. Participants/Hackathon/Submissions/Get
7. Participants/Hackathon/Status
8. Organizers/Hackathon/Organizers/List
9. Organizers/Hackathon/Participants/List
10. ~~Participants/Hackathon/Registration/Submissions/List~~ - Removed (uses `.Includes()` for navigation properties)
11. Organizers/Hackathon/Venue/Overview

**Note**: Queries using `.Includes()` for navigation properties were excluded from caching as SqlSugar doesn't properly serialize/deserialize navigation properties in cached results.

#### Team Endpoints Enhancement (Commit 4)
Added `.WithCache()` to read queries within team-related write endpoints:
- Teams/Create - Hackathon validation query
- Teams/Update - Hackathon and team validation queries
- Teams/SelectChallenge - Hackathon, team, and challenge validation queries
- Teams/JoinTeam - Hackathon and team validation queries
- Teams/Leave - Hackathon and participant validation queries
- Teams/RemoveMember - Hackathon, team, participant, and member count queries
- Teams/JoinByCode - Team, hackathon, and participant validation queries

**Rationale**: Teams are not frequently mutated after initial formation. Caching validation queries improves performance without significant cache invalidation concerns due to automatic cache invalidation on writes.

### Endpoints NOT Cached (By Design)

The following endpoints are intentionally NOT cached:

#### Write Operations
- All CREATE operations
- All UPDATE operations
- All DELETE operations
- **Reason**: These are write operations; only validation read queries within them are cached

#### Special Cases
- `POST /auth/login` - Authentication endpoint
- `POST /callback/login/github` - OAuth callback
- `POST /auth/impersonate` - Admin impersonation
- **Reason**: Security-sensitive, must be real-time

## Future Improvements

### 1. Granular Cache Keys
Consider implementing custom cache key generation for:
- Venue check-ins (include time window in key)
- Team operations (per team ID)

### 2. Cache TTL Configuration
Add configurable TTL per endpoint type:
- Static content: 1 hour
- Semi-static content: 5 minutes
- Dynamic content: 30 seconds

### 3. Cache Warming
Pre-populate cache on application startup for:
- Published hackathons
- Active challenges
- Registration questions

### 4. Selective Cache Invalidation
Instead of invalidating all cache entries for a table, invalidate only affected keys:
- When updating Hackathon X, only invalidate Hackathon X cache, not all hackathons
- Requires custom cache key management

## Troubleshooting

### Cache Not Working
1. Check Redis connection: Ensure Redis is running and accessible
2. Verify `IsAutoRemoveDataCache = true` in `Program.cs`
3. Check logs for `NoOpCacheService` usage (indicates Redis unavailable)

### Stale Data Issues
1. Verify automatic invalidation is working
2. Check if manual cache invalidation is needed for complex scenarios
3. Consider reducing cache TTL for affected endpoints

### Performance Issues
1. Monitor Redis memory usage
2. Check for cache key explosion (too many unique keys)
3. Review query complexity - caching doesn't fix inefficient queries

## Support

For questions or issues related to caching:
1. Check this documentation first
2. Review `SqlSugarRedisCache.cs` implementation
3. Monitor Redis using Redis CLI: `redis-cli MONITOR`
4. Check application logs for cache-related warnings
