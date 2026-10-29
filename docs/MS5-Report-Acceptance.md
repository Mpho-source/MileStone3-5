# EduConnect Milestone 5 — Report acceptance checklist

This is the team's final test checklist for the three reports designed in Milestone 2, within the Milestone 3–5 brief. It does not claim runtime acceptance until the team tests the actual Access database and rebuilt application.

## Required report behaviour

1. **Outstanding Tutoring Requests (7+ Days):** Show only `TUTORING_REQUEST.Status = Submitted` where `RequestDate` is at least seven full calendar days before the report date. Include student number/name, course, submission date, age and status, oldest first. A Scheduled or Completed session is not an outstanding request.
2. **Tutor Workload by Reporting Period:** Count Scheduled and Completed sessions by tutor using the selected `SessionDate` range, report both counts separately, total sessions and completion rate. The default report date range covers the current calendar year; users can change it.
3. **Student GPA and Tutoring Attendance:** GPA is read from existing `STUDENTACADEMICRECORD` for T1–T4; attendance is calculated only from Completed tutoring sessions with Attended/Absent. Missing attendance remains missing, not 0%. Display the all-student T1–T4 table by default, with the chart available via Show chart. Preview/PDF must contain the chart and the student table.

## Fixture setup (on a working Windows PC with Microsoft Access Database Engine)

Close Microsoft Access and EduConnect. Use the `Edu_Connect.accdb` in the *same project root* that Visual Studio runs from.

```bash
git fetch origin
git show origin/integration/test-student-tutor:scripts/Populate-EduConnectFullReports.ps1 > scripts/Populate-EduConnectFullReports.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "scripts/Populate-EduConnectFullReports.ps1"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "scripts/Populate-EduConnectFullReports.ps1" -Apply
```

Inspect the PREVIEW before running the final line. The script creates a fresh `.accdb` backup and inserts linked illustrative sample data transactionally. It does not change existing account credentials or GPA records; it adds reports' required sample tutoring requests and sessions. Its verification checks for old Submitted requests, Scheduled and Completed sessions, and attended/absent records across all four terms. If any verification fails it rolls back.

## Runtime checks before submitting

- Report 1 must list aged `Submitted` requests; the full-table empty view is only valid when there truly are no qualifying requests.
- Report 2: select 01 Jan 2026–31 Dec 2026 (or use the year-wide default after rebuilding). Confirm different tutors have Scheduled and Completed counts.
- Report 3: select 2026. The chart should have attendance data for T1, T2, T3 and T4. Click Show table to inspect one row per student with T1–T4 and attendance percentages.
- Print Preview and the exported PDF must include the chart plus the student table for Report 3. Confirm correct date/time formatting and no truncation.
- Tutor attendance should only be entered through the Tutor UI after the session has ended; completing a session must update the linked request. Historical records are illustrative demonstration data, not verified real student outcomes.
- Verify existing student, tutor and administrator logins, availability checks, 48-hour booking rules, duplicate prevention, confirmation messages and graceful exception handling.
- Deliver the complete C# project including the Access database and a **recorded PowerPoint presentation no longer than 15 minutes**; include runtime demonstration, usability explanation and team roles.

Do not publish authentic passwords, credentials or personal academic records in a repository accessible to unauthorized people. Use authorized sample identities and data.

The working Access database is not automatically modified by a GitHub script commit. After running and verifying the script, the group must commit and push the updated database separately.
