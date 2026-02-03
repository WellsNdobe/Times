# Timesheet Flow Assessment

## Intended Flow

| Role | Responsibility |
|------|----------------|
| **Employee** | Create and submit timesheets (entries → submit for approval) |
| **Manager** | Approve or reject submitted timesheets |
| **Admin** | Set up organization-level stuff (creates org, members, clients, projects) |

---

## How Close the Current System Is

### ✅ Employee flow — **very close**

- **Create timesheet:** Any org member can create a weekly timesheet (idempotent per user/week/org). ✓
- **Add/edit entries:** Only the timesheet owner can add/update/delete entries; entries are locked when status is Submitted or Approved. ✓
- **Submit:** Only the owner can submit; allowed from Draft or Rejected; requires at least one entry. ✓
- **View:** Owner can list "mine" and get one by ID. ✓

**Gap:** There is no explicit restriction that "only Employees" create timesheets; Admins and Managers can create their own too. If you want only the Employee role to create timesheets, you’d add a role check in `CreateAsync` (and optionally a separate flow for managers/admins). Often orgs allow everyone to log time, so current behavior may be fine.

---

### ⚠️ Manager flow — **partially there**

- **Approve/Reject:** Only Admin or Manager can approve/reject. ✓
- **View a timesheet:** Manager/Admin can get any timesheet by ID (if they know the ID). ✓

**Missing:**

1. **List pending approval** — There is no endpoint for "timesheets awaiting my approval." Managers have no way to discover submitted timesheets except by being given IDs. A **pending-approval** list (e.g. `GET .../timesheets/pending-approval`) is the main structural gap.
2. **Approval comment** — `ApproveTimesheetRequest` has a `Comment` but it is not stored. Adding an `ApprovedComment` (or similar) field would improve audit and procedures.
3. **Reporting structure (optional)** — There is no "reports to" / manager–employee relationship. Today, any Manager/Admin can approve any timesheet in the org. If you want "managers only approve their direct reports," you’d add something like `ReportsToUserId` (or `ManagerId`) on `OrganizationMember` and restrict:
   - Who can approve which timesheets, and
   - What appears in the pending-approval list (e.g. only timesheets for users who report to the current user).

---

### ✅ Admin / org-level flow — **mostly there**

- **Create org:** Any authenticated user can create an org and becomes its Admin. ✓
- **Update org:** Only Admin. ✓
- **Members:** Add member, Create user in org, Update member — Admin only. ✓
- **View members:** Admin or Manager. ✓
- **Clients:** Create/Update/Delete — Admin **or** Manager. ✓
- **Projects:** Create/Update/Assign/Unassign — Admin **or** Manager. ✓

**Refinement (optional):** If "org-level setup" should be **Admin-only**, you could restrict **Clients** and **Projects** (create/update/delete) to Admin only, and leave Manager with:
- Assigning users to projects, and
- Approving/rejecting timesheets.

That would make the split: Admin = org + clients + projects + members; Manager = assignments + approvals.

---

## Structural and Procedural Changes to Consider

### 1. **Manager: list timesheets pending approval** (high impact)

- **Add:** `ListPendingApprovalAsync(actorUserId, organizationId, fromWeekStart?, toWeekStart?)` in `ITimesheetService` / `TimesheetService`, returning timesheets with `Status == Submitted`.
- **Restrict:** Caller must be Admin or Manager.
- **Controller:** `GET api/v1/organizations/{organizationId}/timesheets/pending-approval?...`
- **Optional later:** Filter by "my reports" when you have a reporting structure.

### 2. **Store approval comment** (audit / procedure)

- **Add:** `ApprovedComment` (nullable string) on `Timesheet` and in `TimesheetResponse`.
- **Use:** In `ApproveAsync`, set `ts.ApprovedComment = request.Comment` (trimmed).
- **Migration:** New column on `Timesheets` table.

### 3. **Reporting structure (optional)**

- **Add:** e.g. `ReportsToUserId` (nullable) on `OrganizationMember` (or a dedicated reporting table).
- **Use:** When listing pending approval and when approving, restrict to timesheets whose owner’s `ReportsToUserId == actorUserId` (and optionally allow Admin to see all).
- **APIs:** Endpoints to set/clear "reporting to" (Admin or Manager only).

### 4. **Submission deadlines (procedure)**

- **Concept:** e.g. "Submit by Friday 5pm for that week."
- **Options:** Org-level or week-end config (e.g. `Organization.SubmissionDeadlineDayOfWeek`, `SubmissionDeadlineTime`), or a fixed rule. Reject or warn when submitting after the deadline; optional "late submission" flag for reporting.

### 5. **Notifications (procedure)**

- On **submit:** Notify managers (or only the employee’s reporting manager when you have reporting structure).
- On **approve/reject:** Notify the employee.
- Implement via in-app feed and/or email, depending on your stack.

### 6. **Restrict timesheet creation to Employees (optional)**

- In `TimesheetService.CreateAsync`, if you want only Employees to create timesheets, add:  
  `if (membership.Role != OrganizationRole.Employee) throw new ForbiddenException("Only Employees can create timesheets.");`  
  (and optionally allow Manager to create their own if desired.)

### 7. **Admin vs Manager: org-level setup**

- **Tighten:** Clients and Projects create/update/delete → Admin only.
- **Keep:** Manager can assign users to projects and approve/reject timesheets.

### 8. **Reports / payroll export**

- **Add:** Read-only endpoint for Admin/Manager: e.g. "all timesheets in status Approved for date range" (and optionally by user/project) for export or payroll integration.

### 9. **DbSeeder and default org**

- Current seeder creates a global Admin user with no org. Consider creating a default organization and adding the seed admin as an org Admin so they can use the app without creating an org first.

---

## Summary

| Area | Status | Main change |
|------|--------|-------------|
| Employee create/submit | ✅ In place | Optional: restrict create to Employee role only |
| Manager approve/reject | ✅ In place | Add **pending-approval list** and optionally **ApprovedComment** |
| Manager discover pending | ❌ Missing | Add `GET .../timesheets/pending-approval` (Manager/Admin) |
| Admin org-level setup | ✅ In place | Optional: restrict Clients/Projects to Admin only |
| Reporting (manager of X) | ❌ Missing | Optional: add ReportsTo and scope approval/pending list |
| Procedures | Partial | Deadlines, notifications, reports, approval comment |

The biggest structural addition that matches your described flow is a **pending-approval** list for managers; storing the **approval comment** is a small, high-value addition for procedures and audit.
