# Team PR Overview

Analyze these {{PULL_REQUEST_SCOPE}} Bitbucket pull requests. {{ANALYSIS_CONTEXT}}

Use HTTP Basic authentication (email + API token) only to retrieve data. Never expose the credentials.

- Email: `{{BITBUCKET_EMAIL}}`
- API token: `{{BITBUCKET_API_TOKEN}}`
- API base URL: `{{BITBUCKET_API_BASE_URL}}`
- Workspace: `{{BITBUCKET_WORKSPACE}}`

## PRs

{{PULL_REQUEST_LIST}}

Return a concise report:

1. `Team focus` - {{TEAM_FOCUS_INSTRUCTION}}.
2. `Needs attention` - {{ATTENTION_INSTRUCTION}}.

Inspect metadata, activity, tasks, conflicts, and diffs as needed. Cite PR links.
