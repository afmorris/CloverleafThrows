-- ============================================================
-- Cloverleaf Throws — Database Schema
-- SQL Server 2019+
-- Run this script to create the database and all tables.
-- ============================================================

-- CREATE DATABASE CloverleafThrows;
-- GO
-- USE CloverleafThrows;
-- GO

-- ============================================================
-- EXERCISES (reusable library)
-- ============================================================
CREATE TABLE ExerciseCategories (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    Name            NVARCHAR(100) NOT NULL,       -- 'Throws Warmup', 'Shot Put', 'Discus', 'Lifting', 'Mobility', 'Core', 'Plyometrics', 'Sprint'
    SortOrder       INT NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE Exercises (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    Name            NVARCHAR(200) NOT NULL,
    CategoryId      INT NOT NULL REFERENCES ExerciseCategories(Id),
    DefaultReps     NVARCHAR(50) NULL,            -- e.g., '3 x 10', 'x 20' — suggested default
    Notes           NVARCHAR(500) NULL,
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE INDEX IX_Exercises_Category ON Exercises(CategoryId);

-- ============================================================
-- MESOCYCLES
-- ============================================================
CREATE TABLE Mesocycles (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    Name            NVARCHAR(200) NOT NULL,       -- 'Familiarity & Strength Building'
    Description     NVARCHAR(1000) NULL,
    StartDate       DATE NOT NULL,
    EndDate         DATE NOT NULL,
    IsCurrent       BIT NOT NULL DEFAULT 0,       -- only one active at a time
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- ============================================================
-- WORKOUT DAYS
-- ============================================================
CREATE TABLE WorkoutDays (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    MesocycleId     INT NOT NULL REFERENCES Mesocycles(Id) ON DELETE CASCADE,
    DayNumber       INT NOT NULL,
    [Date]          DATE NOT NULL,
    DayType         NVARCHAR(50) NOT NULL,        -- 'Oly / Lower', 'Upper Body', 'Throwing Only', 'Jump / Sprint', 'Full Body'
    ThrowsFocus     NVARCHAR(20) NOT NULL,        -- 'Shot Put', 'Discus'
    CoachNotes      NVARCHAR(2000) NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT UQ_WorkoutDays_Date UNIQUE (MesocycleId, [Date]),
    CONSTRAINT UQ_WorkoutDays_DayNumber UNIQUE (MesocycleId, DayNumber)
);

CREATE INDEX IX_WorkoutDays_Mesocycle ON WorkoutDays(MesocycleId);
CREATE INDEX IX_WorkoutDays_Date ON WorkoutDays([Date]);

-- ============================================================
-- WORKOUT SECTIONS & EXERCISES
-- ============================================================

-- Sections within a workout day (e.g., 'Throws Warmup', 'Throwing', 'Lifting', 'Mobility', 'Core')
CREATE TABLE WorkoutSections (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    WorkoutDayId    INT NOT NULL REFERENCES WorkoutDays(Id) ON DELETE CASCADE,
    Name            NVARCHAR(100) NOT NULL,       -- 'Throws Warmup', 'Throwing', 'Lifting', 'Mobility', 'Core'
    SortOrder       INT NOT NULL DEFAULT 0,
    HeaderColor     NVARCHAR(7) NULL,             -- hex color for section header, e.g. '#1F4E79'
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE INDEX IX_WorkoutSections_Day ON WorkoutSections(WorkoutDayId);

-- Exercise groups within a section (e.g., 'Warmup', 'Superset 1', 'Compound')
CREATE TABLE ExerciseGroups (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    WorkoutSectionId INT NOT NULL REFERENCES WorkoutSections(Id) ON DELETE CASCADE,
    Label           NVARCHAR(100) NULL,           -- 'Warmup', 'Superset 1', 'Plyometrics', etc. NULL = no label
    SortOrder       INT NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE INDEX IX_ExerciseGroups_Section ON ExerciseGroups(WorkoutSectionId);

-- Individual exercises within a group
CREATE TABLE WorkoutExercises (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    ExerciseGroupId INT NOT NULL REFERENCES ExerciseGroups(Id) ON DELETE CASCADE,
    ExerciseId      INT NULL REFERENCES Exercises(Id),  -- NULL if ad-hoc / not from library
    ExerciseName    NVARCHAR(200) NOT NULL,       -- denormalized name (allows overrides)
    Number          NVARCHAR(10) NULL,            -- '1a', '2b', 'W1', etc.
    Reps            NVARCHAR(100) NOT NULL,       -- '3 x 10 each', '5 x 5', 'x 20'
    SortOrder       INT NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE INDEX IX_WorkoutExercises_Group ON WorkoutExercises(ExerciseGroupId);

-- ============================================================
-- ATHLETES & ROSTER
-- ============================================================
CREATE TABLE Athletes (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    FirstName       NVARCHAR(100) NOT NULL,
    LastName        NVARCHAR(100) NOT NULL,
    GradYear        INT NULL,
    Gender          NVARCHAR(1) NOT NULL DEFAULT 'M',  -- 'M' or 'F'
    IsActive        BIT NOT NULL DEFAULT 1,
    Notes           NVARCHAR(500) NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Events an athlete is assigned to
CREATE TABLE AthleteEvents (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    AthleteId       INT NOT NULL REFERENCES Athletes(Id) ON DELETE CASCADE,
    EventName       NVARCHAR(100) NOT NULL,       -- 'Shot Put', 'Discus', 'Hammer Throw'
    IsPrimary       BIT NOT NULL DEFAULT 0,       -- primary vs secondary event
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE INDEX IX_AthleteEvents_Athlete ON AthleteEvents(AthleteId);

-- ============================================================
-- MEETS
-- ============================================================
CREATE TABLE Meets (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    Name            NVARCHAR(300) NOT NULL,
    [Date]          DATE NOT NULL,
    Location        NVARCHAR(300) NULL,
    MeetType        NVARCHAR(50) NOT NULL DEFAULT 'Outdoor',  -- 'Indoor', 'Outdoor'
    Notes           NVARCHAR(1000) NULL,
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE INDEX IX_Meets_Date ON Meets([Date]);

-- ============================================================
-- MESOCYCLE TEMPLATES (for mesocycle builder)
-- ============================================================

-- A template defines a repeatable week structure
CREATE TABLE MesocycleTemplates (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    Name            NVARCHAR(200) NOT NULL,       -- 'Standard Throws Week'
    Description     NVARCHAR(500) NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Template days define the pattern for each day of the week
CREATE TABLE TemplateDays (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    TemplateId      INT NOT NULL REFERENCES MesocycleTemplates(Id) ON DELETE CASCADE,
    DayOfWeek       INT NOT NULL,                 -- 1=Monday ... 5=Friday
    DayType         NVARCHAR(50) NOT NULL,
    ThrowsFocus     NVARCHAR(20) NOT NULL,        -- 'Shot Put', 'Discus', 'Alternate'
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE INDEX IX_TemplateDays_Template ON TemplateDays(TemplateId);

-- Template sections define which sections each template day includes
CREATE TABLE TemplateSections (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    TemplateDayId   INT NOT NULL REFERENCES TemplateDays(Id) ON DELETE CASCADE,
    SectionName     NVARCHAR(100) NOT NULL,
    SortOrder       INT NOT NULL DEFAULT 0,
    HeaderColor     NVARCHAR(7) NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- ============================================================
-- ADMIN AUTH (simple cookie-based)
-- ============================================================
CREATE TABLE AdminUsers (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    Username        NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash    NVARCHAR(500) NOT NULL,       -- BCrypt hash
    DisplayName     NVARCHAR(200) NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastLoginAt     DATETIME2 NULL
);

-- ============================================================
-- TRAINING LOAD TRACKING (for season overview)
-- ============================================================
CREATE TABLE DailyLoadSummary (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    WorkoutDayId    INT NOT NULL REFERENCES WorkoutDays(Id) ON DELETE CASCADE,
    TotalExercises  INT NOT NULL DEFAULT 0,
    ThrowingVolume  INT NOT NULL DEFAULT 0,       -- total throw count for the day
    LiftingSets     INT NOT NULL DEFAULT 0,       -- total lifting sets
    HasPlyos        BIT NOT NULL DEFAULT 0,
    HasSprints      BIT NOT NULL DEFAULT 0,
    ComputedAt      DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE INDEX IX_DailyLoadSummary_Day ON DailyLoadSummary(WorkoutDayId);

-- ============================================================
-- SEED DATA: Exercise Categories
-- ============================================================
INSERT INTO ExerciseCategories (Name, SortOrder) VALUES
    ('Throws Warmup', 1),
    ('Shot Put', 2),
    ('Discus', 3),
    ('Lifting - Compound', 4),
    ('Lifting - Accessory', 5),
    ('Plyometrics', 6),
    ('Sprint', 7),
    ('Mobility', 8),
    ('Core', 9),
    ('Med Ball', 10);

-- ============================================================
-- SEED DATA: Exercise Library
-- ============================================================

-- Throws Warmup
INSERT INTO Exercises (Name, CategoryId, DefaultReps) VALUES
    ('Lateral Line Hops', 1, '3 x 10 each'),
    ('Duck Walks', 1, '3 x 10y'),
    ('Broad Jumps', 1, '3 x 6'),
    ('Sprints', 1, '3 x 25y'),
    ('Broad Jump to Sprint', 1, '3 x 25y'),
    ('Single Leg Hops', 1, '3 x 10y each'),
    ('Bounds', 1, '3 x 15y'),
    ('A-Skips', 1, '3 x 20y'),
    ('Striders', 1, '3 x 25y'),
    ('Plyo Push-Ups', 1, '3 x 6'),
    ('Twisting Sit-Ups', 1, '3 x 10 each'),
    ('Clap Push-Ups', 1, '3 x 6'),
    ('Speed Skaters', 1, '3 x 8 each'),
    ('Double Broad Jumps', 1, '3 x 5'),
    ('Power Skips for Height', 1, '3 x 6 each'),
    ('Tuck Jumps', 1, '3 x 5'),
    ('Backward Run', 1, '3 x 20y'),
    ('Bear Crawls', 1, '3 x 15y'),
    ('Squat Jumps', 1, '3 x 6'),
    ('Skipping for Distance', 1, '3 x 25y');

-- Shot Put
INSERT INTO Exercises (Name, CategoryId, DefaultReps) VALUES
    ('Drivers', 2, 'x 20'),
    ('Overhead Throws', 2, 'x 20'),
    ('Stand Throws', 2, 'x 8'),
    ('Side Shuffles', 2, 'x 6'),
    ('Step Back Glides', 2, 'x 4'),
    ('Glide Shuffles', 2, 'x 3'),
    ('Full Throws', 2, 'x 3');

-- Discus
INSERT INTO Exercises (Name, CategoryId, DefaultReps) VALUES
    ('Pressers', 3, 'x 20'),
    ('Front Raises', 3, 'x 20'),
    ('Stand Throws', 3, 'x 6'),
    ('Walking Line Throws', 3, 'x 6'),
    ('Static Wheel', 3, 'x 4'),
    ('Dynamic Wheel', 3, 'x 3'),
    ('South Africans', 3, 'x 6'),
    ('Full Throws', 3, 'x 3');

-- Lifting - Compound
INSERT INTO Exercises (Name, CategoryId, DefaultReps) VALUES
    ('Power Clean + Front Squat', 4, '6 x 2/4'),
    ('Hang Clean', 4, '5 x 3'),
    ('Back Squat', 4, '5 x 5'),
    ('BB Bench Press', 4, '5 x 5'),
    ('BB Bent Over Row', 4, '5 x 5'),
    ('BB RDL', 4, '5 x 6 ea');

-- Lifting - Accessory
INSERT INTO Exercises (Name, CategoryId, DefaultReps) VALUES
    ('DB Bulgarian Split Squat', 5, '5 x 6 ea'),
    ('Cossack Squats', 5, '4 x 6 ea'),
    ('Landmine Rot. Press', 5, '4 x 6 ea'),
    ('DB Incline Press', 5, '4 x 8'),
    ('Pull-Ups (band-assist OK)', 5, '4 x 8'),
    ('DB Reverse Flys', 5, '3 x 12'),
    ('DB Lateral Raise', 5, '3 x 10'),
    ('DB Step-Ups', 5, '4 x 6 ea'),
    ('DB Single Arm Row', 5, '4 x 8 ea'),
    ('Single-Leg RDL', 5, '3 x 6 ea'),
    ('Push-Ups', 5, '3 x 12');

-- Plyometrics
INSERT INTO Exercises (Name, CategoryId, DefaultReps) VALUES
    ('Box Jumps', 6, '4 x 3'),
    ('Hurdle Hops', 6, '4 x 5'),
    ('Single-Leg Box Jump', 6, '3 x 3 ea');

-- Sprint
INSERT INTO Exercises (Name, CategoryId, DefaultReps) VALUES
    ('10m Acceleration', 7, 'x 4'),
    ('30m Fly Sprint', 7, 'x 4');

-- Mobility
INSERT INTO Exercises (Name, CategoryId, DefaultReps) VALUES
    ('Deep Squat Hold', 8, '2 x 1:00'),
    ('90/90 Hip Stretch', 8, '2 x 1:00 ea'),
    ('Cat Cow', 8, '2 x 10'),
    ('Tib Raises', 8, '2 x 25'),
    ('Kneeling Lunge Hold', 8, '2 x 1:00 ea'),
    ('Seated T-Spine Rotations', 8, '2 x 5 ea'),
    ('World''s Greatest Stretch', 8, '2 x 5 ea'),
    ('Pigeon Stretch', 8, '2 x 1:00 ea'),
    ('Scorpion', 8, '2 x 8 ea'),
    ('Wall Slide', 8, '2 x 10'),
    ('Banded Shoulder ER', 8, '2 x 10 ea'),
    ('Ankle Circles', 8, '2 x 10 ea'),
    ('Leg Swings (front/lateral)', 8, '2 x 10 ea'),
    ('Walking Knee Hugs', 8, '2 x 10');

-- Core
INSERT INTO Exercises (Name, CategoryId, DefaultReps) VALUES
    ('MB Russian Twists', 9, '3 x 20'),
    ('Dead Bugs', 9, '3 x 10'),
    ('Side Plank', 9, '3 x 0:30 ea'),
    ('V-Ups', 9, '3 x 10'),
    ('Front Plank', 9, '3 x 1:00'),
    ('MB Slam + Rotate', 9, '3 x 8 ea'),
    ('Pallof Press Hold', 9, '3 x 0:20 ea'),
    ('Bicycle Crunches', 9, '3 x 20'),
    ('Bird Dogs', 9, '3 x 8 ea'),
    ('Suitcase Carry', 9, '3 x 30y ea'),
    ('Hollow Body Hold', 9, '3 x 0:30'),
    ('Plank Shoulder Taps', 9, '3 x 10 ea'),
    ('Flutter Kicks', 9, '3 x 0:30');

-- Med Ball
INSERT INTO Exercises (Name, CategoryId, DefaultReps) VALUES
    ('MB Chest Pass', 10, '3 x 5'),
    ('MB Overhead Throw', 10, '3 x 5'),
    ('MB Rotational Throw', 10, '3 x 4 ea');

-- ============================================================
-- SEED: Default Admin User (password: changeme)
-- BCrypt hash for 'changeme'
-- ============================================================
INSERT INTO AdminUsers (Username, PasswordHash, DisplayName) VALUES
    ('coach', '$2a$11$K7KzYFmqZVbjHlAbCo1GCuGNMft66LMjNV/dODGeYF.qX3pMk5sXa', 'Coach Tony');

GO

PRINT 'Schema created successfully.';
