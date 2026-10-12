/* =========================================================
   RaceDay Database Creation Script
   Target: SQL Server (SSMS)
   ========================================================= */

IF DB_ID('RaceDayDB') IS NULL
BEGIN
    CREATE DATABASE RaceDayDB;
END
GO

USE RaceDayDB;
GO

/* Drop tables if re-running this script during development,
   in reverse dependency order (children before parents) */
IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrolments', 'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
GO

/* =========================================================
   TABLE: Users
   Stores both Organisers and Participants, distinguished
   by the Role column.
   ========================================================= */
CREATE TABLE dbo.Users (
    UserID          INT             IDENTITY(1,1)   PRIMARY KEY,
    FullName        NVARCHAR(100)   NOT NULL,
    Email           NVARCHAR(150)   NOT NULL UNIQUE,
    PasswordHash    NVARCHAR(255)   NOT NULL,
    Role            NVARCHAR(20)    NOT NULL
                        CONSTRAINT CK_Users_Role CHECK (Role IN ('Organiser', 'Participant')),
    CreatedAt       DATETIME        NOT NULL DEFAULT GETDATE()
);
GO

/* =========================================================
   TABLE: Events
   A race day owned by one Organiser.
   ========================================================= */
CREATE TABLE dbo.Events (
    EventID         INT             IDENTITY(1,1)   PRIMARY KEY,
    OrganiserID     INT             NOT NULL,
    EventName       NVARCHAR(150)   NOT NULL,
    Description     NVARCHAR(1000)  NULL,
    Location        NVARCHAR(150)   NOT NULL,
    EventDate       DATE            NOT NULL,
    DistanceKm      DECIMAL(6,2)    NOT NULL,
    EventType       NVARCHAR(10)    NOT NULL
                        CONSTRAINT CK_Events_EventType CHECK (EventType IN ('Run', 'Walk', 'Cycle')),
    Status          NVARCHAR(20)    NOT NULL DEFAULT 'Draft'
                        CONSTRAINT CK_Events_Status CHECK (Status IN ('Draft', 'Published', 'Cancelled', 'Completed')),
    CreatedAt       DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganiserID)
        REFERENCES dbo.Users(UserID)
);
GO

/* =========================================================
   TABLE: Categories
   Age or distance divisions within an Event
   (e.g. Under 20, Senior, 10km, 21km).
   ========================================================= */
CREATE TABLE dbo.Categories (
    CategoryID      INT             IDENTITY(1,1)   PRIMARY KEY,
    EventID         INT             NOT NULL,
    CategoryName    NVARCHAR(100)   NOT NULL,
    CategoryType    NVARCHAR(10)    NOT NULL
                        CONSTRAINT CK_Categories_Type CHECK (CategoryType IN ('Age', 'Distance')),
    MaxParticipants INT             NOT NULL DEFAULT 100,
    Price           DECIMAL(8,2)    NOT NULL DEFAULT 0,
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventID)
        REFERENCES dbo.Events(EventID)
);
GO

/* =========================================================
   TABLE: Enrolments
   Links a Participant to a Category they signed up for.
   ========================================================= */
CREATE TABLE dbo.Enrolments (
    EnrolmentID     INT             IDENTITY(1,1)   PRIMARY KEY,
    ParticipantID   INT             NOT NULL,
    CategoryID      INT             NOT NULL,
    EnrolmentDate   DATETIME        NOT NULL DEFAULT GETDATE(),
    Status          NVARCHAR(20)    NOT NULL DEFAULT 'Confirmed'
                        CONSTRAINT CK_Enrolments_Status CHECK (Status IN ('Pending', 'Confirmed', 'Cancelled')),
    BibNumber       NVARCHAR(10)    NULL,
    CONSTRAINT FK_Enrolments_Participant FOREIGN KEY (ParticipantID)
        REFERENCES dbo.Users(UserID),
    CONSTRAINT FK_Enrolments_Category FOREIGN KEY (CategoryID)
        REFERENCES dbo.Categories(CategoryID),
    CONSTRAINT UQ_Enrolments_ParticipantCategory UNIQUE (ParticipantID, CategoryID)
);
GO

/* =========================================================
   TABLE: Results
   The outcome of a single Enrolment.
   ========================================================= */
CREATE TABLE dbo.Results (
    ResultID        INT             IDENTITY(1,1)   PRIMARY KEY,
    EnrolmentID     INT             NOT NULL UNIQUE,
    FinishTime      TIME            NULL,
    Position        INT             NULL,
    Status          NVARCHAR(20)    NOT NULL DEFAULT 'Finished'
                        CONSTRAINT CK_Results_Status CHECK (Status IN ('Finished', 'DNF', 'DSQ')),
    RecordedAt      DATETIME        NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Results_Enrolment FOREIGN KEY (EnrolmentID)
        REFERENCES dbo.Enrolments(EnrolmentID)
);
GO

/* =========================================================
   SEED DATA
   ========================================================= */

-- Organisers (2) and Participants (2)
INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role) VALUES
('Thabo Mokoena',   'thabo.mokoena@raceday.co.za',   'HASHED_PASSWORD_1', 'Organiser'),
('Lindiwe Nkosi',    'lindiwe.nkosi@raceday.co.za',   'HASHED_PASSWORD_2', 'Organiser'),
('Sarah van Wyk',     'sarah.vanwyk@example.com',      'HASHED_PASSWORD_3', 'Participant'),
('James Botha',      'james.botha@example.com',       'HASHED_PASSWORD_4', 'Participant');
GO

-- Events (3), each owned by an Organiser
INSERT INTO dbo.Events (OrganiserID, EventName, Description, Location, EventDate, DistanceKm, EventType, Status) VALUES
(1, 'Johannesburg City Marathon', 'Annual road race through the Joburg CBD and northern suburbs.', 'Johannesburg, Gauteng', '2026-10-18', 42.20, 'Run', 'Published'),
(1, 'Soweto Fun Walk',            'Family-friendly community walk through Soweto.',                 'Soweto, Gauteng',        '2026-11-08', 10.00, 'Walk', 'Published'),
(2, 'Cape Town Coastal Cycle',    'Scenic coastal cycling challenge along the Atlantic seaboard.',  'Cape Town, Western Cape', '2026-12-05', 50.00, 'Cycle', 'Draft');
GO

-- Categories, at least one per Event (mix of distance and age categories)
INSERT INTO dbo.Categories (EventID, CategoryName, CategoryType, MaxParticipants, Price) VALUES
(1, '10km',          'Distance', 500, 150.00),
(1, '21km',          'Distance', 300, 250.00),
(1, 'Under 20',      'Age',      150, 100.00),
(2, '5km Walk',      'Distance', 400, 80.00),
(2, 'Senior (60+)',  'Age',      200, 60.00),
(3, '50km Route',    'Distance', 250, 300.00);
GO

-- Sample enrolments (Participants enrolling in Categories)
INSERT INTO dbo.Enrolments (ParticipantID, CategoryID, Status, BibNumber) VALUES
(3, 1, 'Confirmed', 'E1-0001'),   -- Sarah -> JHB City Marathon 10km
(3, 4, 'Confirmed', 'E2-0002'),   -- Sarah -> Soweto Fun Walk 5km
(4, 2, 'Confirmed', 'E1-0003'),   -- James -> JHB City Marathon 21km
(4, 6, 'Pending',   NULL);        -- James -> Cape Town Coastal Cycle 50km
GO

-- Sample results for completed enrolments
INSERT INTO dbo.Results (EnrolmentID, FinishTime, Position, Status) VALUES
(1, '00:52:14', 12, 'Finished'),
(3, '01:48:37', 5,  'Finished');
GO
