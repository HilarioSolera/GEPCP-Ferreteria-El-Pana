name: Feature Request
description: Suggest a new feature or improvement
title: "[FEATURE] "
labels: ["enhancement", "feature-request"]
assignees: []

body:
  - type: markdown
	attributes:
	  value: |
		Thank you for suggesting an enhancement! Please provide as much detail as possible.

  - type: checkboxes
	id: prerequisites
	attributes:
	  label: Prerequisites
	  description: Please verify these before requesting
	  options:
		- label: I have checked existing feature requests
		  required: true
		- label: This feature doesn't already exist
		  required: true
		- label: I have reviewed the roadmap
		  required: true

  - type: textarea
	id: description
	attributes:
	  label: Description
	  description: Clear description of the requested feature
	  placeholder: "What feature would you like to see?"
	validations:
	  required: true

  - type: textarea
	id: motivation
	attributes:
	  label: Motivation & Use Case
	  description: Why is this feature needed? What problem does it solve?
	  placeholder: |
		- Current limitation: ...
		- Desired outcome: ...
		- Business value: ...
	validations:
	  required: true

  - type: textarea
	id: solution
	attributes:
	  label: Proposed Solution
	  description: How do you think this feature should work?
	  placeholder: |
		- User should be able to...
		- The system should automatically...
		- The interface should include...
	validations:
	  required: true

  - type: textarea
	id: alternatives
	attributes:
	  label: Alternative Approaches
	  description: Any alternative implementations you've considered
	  placeholder: |
		- Alternative 1: ...
		- Alternative 2: ...

  - type: checkboxes
	id: impact
	attributes:
	  label: Impact Assessment
	  description: What areas would this affect?
	  options:
		- label: Employee Management
		- label: Payroll Processing
		- label: Vacation Management
		- label: Loans
		- label: User Interface
		- label: Security
		- label: Performance
		- label: Database Schema
		- label: Integration
		- label: Other

  - type: textarea
	id: resources
	attributes:
	  label: Related Resources
	  description: Links to relevant documentation, issues, or references
	  placeholder: |
		- Related issue: #123
		- Documentation: [Link]
		- Reference: [Link]

  - type: textarea
	id: additional
	attributes:
	  label: Additional Context
	  description: Any other relevant information
	  placeholder: "Add any other context here..."
