# MileStone3-5
# NextGen EduConnect Management System

## Project Overview

The EduConnect Management System is a group project developed by **NextGen, Group 7**.

The system aims to support the management of tutoring sessions by allowing students to book and manage tutoring sessions and administrators to manage tutor information and generate reports.

## Project Objectives

The system will provide the following functionality:

### Milestone 3: Student Booking System
- Allow students to book tutoring sessions.
- Allow students to update existing bookings.
- Allow students to cancel bookings.
- Display tutor availability and prevent double bookings.
- Enforce booking rules and validate user input.
- Store booking information in the database.

### Milestone 4: Administrator Interface and Reporting
- Allow administrators to add new tutors.
- Allow administrators to edit existing tutor information.
- Generate and display the three reports defined in Milestone 2.
- Display reports using a GridView.
- Provide functionality to print reports.
- Integrate the booking and administration features into one application.

### Milestone 5: Presentation
- Present an overview of the problem and implementation approach.
- Demonstrate the running application.
- Explain usability, input validation and exception handling.
- Explain the responsibilities of each team member.

## Technologies

- **Programming language:** C#
- **Development environment:** Visual Studio
- **Version control and collaboration:** Git and GitHub
- **Database:** Access

## Project Structure

The repository will contain the application source code, database setup files, documentation and other files required to build, test and run the system.



## Academic GPA Table (one student per academic year)

The academic GPA table is `STUDENTACADEMICRECORD` with the fields
`StudentNo`, `AcademicYear`, `Term1`, `Term2`, `Term3`, `Term4`, and `RecordSource`.
The primary key is the pair (`StudentNo`, `AcademicYear`), so each student has
one row for 2026 instead of four separate 2026-T1 through 2026-T4 rows.

To migrate a PC that still shows the older one-record-per-term table:

1. Switch to `integration/test-student-tutor` and pull the latest changes.
2. Close Microsoft Access, the running EduConnect application and Visual Studio debugging.
3. From the project directory, run:
   `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\\DatabaseTools\\Consolidate-Academic-Terms.ps1`
4. Reopen `Edu_Connect.accdb` and verify `STUDENTACADEMICRECORD` shows the four term columns.
5. Rebuild the C# application and generate Report 3 for 2026.
6. Commit and push only the updated `Edu_Connect.accdb` after verifying the records.

The migration creates a backup in `DatabaseBackups`, prepares a separate database
copy and verifies all term values before replacing the local original. If values
from the legacy and intermediate GPA tables conflict, it stops without changing
the original. This migration does not edit the tutoring-request or session tables.
The deprecated GPA population script must not be used after migration.

Existing academic records that were synthetic or illustrative remain identified as
such in their underlying `RecordSource` metadata. Do not present those records as
verified real student academic results.

## Team Members

NextGen Group 7 consists of:

1. Khayalethu Mncube
2. Mpho Mokoloko
3. Vukona Novela
4. Rachel Sibeko
5. Prince Moloto
6. Itumeleng Motaung

## Collaboration Guidelines

To keep the project organised:

1. Pull the latest changes before starting new work.
2. Create a separate branch for each feature or task.
3. Commit changes with clear, descriptive messages.
4. Push completed work to GitHub.
5. Open a pull request before merging changes into the main branch.
6. Test changes before marking a task as complete.
7. Do not commit passwords, personal credentials or other sensitive information.

## Project Status

The project is under development. Features will be added, tested and integrated as the team progresses.

## Setup Instructions

Setup instructions for the application and database will be added once the project structure and database configuration have been finalised.

## Academic Context

**Group:** NextGen, Group 7  
**Project:** EduConnect Management System
