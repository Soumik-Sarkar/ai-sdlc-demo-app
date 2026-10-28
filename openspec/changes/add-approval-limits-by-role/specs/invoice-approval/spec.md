## ADDED Requirements

### Requirement: Manager approval limit
A Manager SHALL be able to approve a pending invoice whose amount is 50,000 or less.

#### Scenario: Manager approves within limit
- **WHEN** a Manager approves a pending invoice of 50,000
- **THEN** the invoice becomes Approved and records that Manager as the decider

#### Scenario: Manager over limit
- **WHEN** a Manager approves a pending invoice of 50,000.01
- **THEN** the approval is refused with a reason stating the Manager limit of 50,000
- **AND** the invoice is unchanged

### Requirement: Director approval limit
A Director SHALL be able to approve a pending invoice whose amount is 500,000 or less.

#### Scenario: Director approves within limit
- **WHEN** a Director approves a pending invoice of 500,000
- **THEN** the invoice becomes Approved and records that Director as the decider

### Requirement: Two approvers above 500,000
An invoice above 500,000 SHALL become Approved only after two distinct approvers have approved it, and the second approver SHALL be a Director.

#### Scenario: First approval leaves invoice pending
- **WHEN** a Manager or Director gives the first approval on an invoice of 500,000.01
- **THEN** the invoice stays Pending Approval and records the first approver

#### Scenario: Second approver is a Director
- **WHEN** a Director approves an invoice that already has a first approval from a different user
- **THEN** the invoice becomes Approved and records the Director as the decider

#### Scenario: Second approver is a Manager
- **WHEN** a Manager tries to give the second approval on an invoice above 500,000
- **THEN** the approval is refused with a reason that the second approver must be a Director
- **AND** the invoice is unchanged

### Requirement: One user cannot be both approvers
The same user SHALL NOT count as both approvers on one invoice.

#### Scenario: Same Director twice
- **WHEN** the Director who gave the first approval tries to approve the same invoice again
- **THEN** the approval is refused with a reason that a different approver is required
- **AND** the invoice is unchanged

#### Scenario: Same username in different letter case
- **WHEN** a user whose name differs only by letter case from the first approver tries to approve
- **THEN** the approval is refused as the same user

### Requirement: Refused approvals change nothing
A refused approval SHALL return a clear reason and SHALL NOT change the invoice status, approvers, timestamps or notes.

#### Scenario: Refusal is visible to the user
- **WHEN** an approval is refused through the web UI
- **THEN** the reason is shown on the Approvals screen and the invoice remains in the pending list

### Requirement: Roles that cannot approve
Users whose role is neither Manager nor Director SHALL NOT be able to approve an invoice.

#### Scenario: Admin attempts approval
- **WHEN** an Admin approves a pending invoice
- **THEN** the approval is refused and the invoice is unchanged
