# RaceDay — API Endpoint Plan

**Roles:** **Organiser** (creates and manages events, categories and results) and **Participant** (browses events, enrols, views own results).
**Role required column:** *None* = public, *Any* = any logged-in user, otherwise the named role only.

**Authentication approach:** cookie-based session. `POST /api/auth/login` verifies the credentials and starts a session; the browser then sends the session cookie automatically on every request. The user id and role are stored in the session and checked on each protected endpoint. Passwords are stored only as salted PBKDF2 hashes.

**Registering as an Organiser** requires a valid organiser invite code, so a user cannot simply self-assign the more privileged role. Participants register freely.

**Common failure responses:** every protected endpoint returns `401 Unauthorized` when no session exists and `403 Forbidden` when the logged-in role (or ownership) is wrong. `400 Bad Request` is returned for any validation failure.

## Authentication

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Creates an account as an Organiser or Participant; Organisers must supply the invite code. | None | { "fullName", "email", "password", "role", "organiserCode" (Organiser only) } | 201 Created — user (no password) <br> 400 Bad Request <br> 403 Forbidden — bad organiser code <br> 409 Conflict — email already registered |
| POST | /api/auth/login | Verifies credentials and starts a session cookie. | None | { "email", "password" } | 200 OK — user (id, name, email, role) + session cookie <br> 401 Unauthorized — invalid credentials |
| POST | /api/auth/logout | Ends the current session. | Any | None | 204 No Content <br> 401 Unauthorized |

## User Profile

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| GET | /api/users/me | Returns the logged-in user's profile. | Any | None | 200 OK — profile <br> 401 Unauthorized |
| PUT | /api/users/me | Updates the logged-in user's name and email. | Any | { "fullName", "email" } | 200 OK — updated profile <br> 400 Bad Request <br> 401 Unauthorized <br> 409 Conflict — email taken |
| PUT | /api/users/me/password | Changes the logged-in user's password. | Any | { "currentPassword", "newPassword" } | 204 No Content <br> 400 Bad Request — wrong current password / weak new password <br> 401 Unauthorized |

## Events

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| GET | /api/events | Lists published events; optional filters `eventType`, `location`, `from`. | None | None | 200 OK — array of events |
| GET | /api/events/mine | Lists all events (including drafts) owned by the logged-in Organiser. | Organiser | None | 200 OK — array of events <br> 401 / 403 |
| GET | /api/events/{id} | Returns one event with its categories. Drafts are visible only to their owner. | None | None | 200 OK — event + categories <br> 404 Not Found |
| POST | /api/events | Creates an event owned by the logged-in Organiser. | Organiser | { "eventName", "description", "location", "eventDate", "distanceKm", "eventType" (Run/Walk/Cycle), "status" } | 201 Created — event <br> 400 Bad Request <br> 401 / 403 |
| PUT | /api/events/{id} | Updates an event the Organiser owns. | Organiser | same as POST | 200 OK — updated event <br> 400 <br> 403 Forbidden — not owner <br> 404 |
| DELETE | /api/events/{id} | Deletes an event the Organiser owns (only if none of its categories have enrolments). | Organiser | None | 204 No Content <br> 403 <br> 404 <br> 409 Conflict — event has enrolments |

## Categories

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| GET | /api/events/{eventId}/categories | Lists the categories of an event. | None | None | 200 OK — array of categories <br> 404 — event not found |
| POST | /api/events/{eventId}/categories | Adds an age or distance category to an event the Organiser owns. | Organiser | { "categoryName", "categoryType" (Age/Distance), "maxParticipants", "price" } | 201 Created — category <br> 400 <br> 403 <br> 404 |
| PUT | /api/categories/{id} | Updates a category on an event the Organiser owns. | Organiser | same as POST | 200 OK — updated category <br> 400 <br> 403 <br> 404 |
| DELETE | /api/categories/{id} | Deletes a category (only if it has no enrolments). | Organiser | None | 204 No Content <br> 403 <br> 404 <br> 409 Conflict — has enrolments |

## Event Enrolments

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| POST | /api/categories/{categoryId}/enrolments | Enrols the logged-in Participant in a category (which links participant, event and category) and issues a bib number. | Participant | None | 201 Created — enrolment <br> 401 / 403 <br> 404 — category not found <br> 409 Conflict — already enrolled, category full, or event not published |
| GET | /api/users/me/enrolments | Lists the logged-in Participant's enrolments. | Participant | None | 200 OK — array of enrolments |
| GET | /api/events/{eventId}/enrolments | Lists every enrolment across all categories of an event the Organiser owns. | Organiser | None | 200 OK — array of enrolments <br> 403 <br> 404 |
| GET | /api/categories/{categoryId}/enrolments | Lists the enrolments for one category of an event the Organiser owns. | Organiser | None | 200 OK — array of enrolments <br> 403 <br> 404 |
| DELETE | /api/enrolments/{id} | Cancels an enrolment (the enrolled Participant or the owning Organiser). | Any | None | 204 No Content <br> 403 <br> 404 |

## Results

| HTTP method | Route | Description | Role required | Request body | Expected response |
|---|---|---|---|---|---|
| POST | /api/enrolments/{enrolmentId}/results | Records the finish time and position for an enrolment in an event the Organiser owns. | Organiser | { "finishTime" (hh:mm:ss), "position", "status" (Finished/DNF/DSQ) } | 201 Created — result <br> 400 <br> 403 <br> 404 <br> 409 Conflict — result already recorded or enrolment cancelled |
| PUT | /api/results/{id} | Corrects a recorded result. | Organiser | same as POST | 200 OK — updated result <br> 400 <br> 403 <br> 404 |
| GET | /api/categories/{categoryId}/results | Returns the results of a category ordered by position. | None | None | 200 OK — array of results <br> 404 |
| GET | /api/users/me/results | Lists the logged-in Participant's own results. | Participant | None | 200 OK — array of results |
