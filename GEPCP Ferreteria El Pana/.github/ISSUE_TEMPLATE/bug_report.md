name: Bug Report
description: Report a bug or issue you've encountered
title: "[BUG] "
labels: ["bug", "needs-triage"]
assignees: []

body:
  - type: markdown
	attributes:
	  value: |
		Thanks for taking the time to report a bug! Please provide as much detail as possible to help us investigate.

  - type: checkboxes
	id: prerequisites
	attributes:
	  label: Prerequisites
	  description: Please verify these before reporting
	  options:
		- label: I have checked existing issues
		  required: true
		- label: I have the latest version of GEPCP installed
		  required: true
		- label: I have reviewed the documentation
		  required: true

  - type: textarea
	id: description
	attributes:
	  label: Description
	  description: Clear description of the bug
	  placeholder: "What happened and what did you expect to happen?"
	validations:
	  required: true

  - type: textarea
	id: steps
	attributes:
	  label: Steps to Reproduce
	  description: Step-by-step instructions to reproduce the issue
	  placeholder: |
		1. Navigate to...
		2. Click on...
		3. Enter...
		4. Observe the bug...
	validations:
	  required: true

  - type: textarea
	id: expected
	attributes:
	  label: Expected Behavior
	  description: What should happen instead
	  placeholder: "The system should..."
	validations:
	  required: true

  - type: textarea
	id: actual
	attributes:
	  label: Actual Behavior
	  description: What actually happened
	  placeholder: "Instead, the system..."
	validations:
	  required: true

  - type: textarea
	id: screenshots
	attributes:
	  label: Screenshots
	  description: Add screenshots if applicable
	  placeholder: "Paste screenshots here"

  - type: textarea
	id: logs
	attributes:
	  label: Error Logs
	  description: Include error messages or logs
	  placeholder: "Paste error logs here"

  - type: textarea
	id: environment
	attributes:
	  label: Environment
	  description: System and application information
	  placeholder: |
		- OS: Windows 10/11
		- GEPCP Version: 1.0.0
		- .NET Runtime: 8.0.x
		- Browser: Chrome/Edge
	validations:
	  required: true

  - type: textarea
	id: workaround
	attributes:
	  label: Workaround (if available)
	  description: Any temporary workaround you've found
	  placeholder: "Describe any workarounds..."

  - type: textarea
	id: additional
	attributes:
	  label: Additional Context
	  description: Any other relevant information
	  placeholder: "Add any other context here..."
