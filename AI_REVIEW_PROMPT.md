# Pull Request Review

Review the Bitbucket pull request described below.

## Security

The credentials in this prompt are sensitive. Use them only to retrieve the review context. Never repeat, quote, summarize, log, or include them in your response.

## Access

- Bitbucket account: `{{BITBUCKET_EMAIL}}`
- Bitbucket API token: `{{BITBUCKET_API_TOKEN}}`
- Bitbucket API base URL: `{{BITBUCKET_API_BASE_URL}}`
- Bitbucket workspace: `{{BITBUCKET_WORKSPACE}}`

Use HTTP Basic authentication with the account as the username and the API token as the password.

## Pull Request

- URL: {{PULL_REQUEST_URL}}
- Repository: {{REPOSITORY_NAME}}
- Pull request: #{{PULL_REQUEST_ID}}
- Title: {{PULL_REQUEST_TITLE}}
- Author: {{PULL_REQUEST_AUTHOR}}
- Opened: {{PULL_REQUEST_OPENED_ON}}
- Detected Jira issue: {{JIRA_ISSUE_KEY}}

### Description available in the dashboard

{{PULL_REQUEST_DESCRIPTION}}

## Review Instructions

1. Retrieve the pull request metadata from Bitbucket. Identify the source branch, destination branch, current commit, merge base, full description, reviewers, open tasks, and current approval state.
2. Retrieve the complete diff and compare the pull request against the destination branch, not against an arbitrary repository revision.
3. Inspect all changed production code, tests, configuration, database changes, API contracts, and deployment-related files.
4. Find the Jira issue referenced by the branch name, title, description, or commit messages. Review the Jira summary, description, acceptance criteria, constraints, and relevant discussion when Jira access is available.
5. Evaluate whether the implementation actually solves the Jira issue and matches the pull request description. Explicitly identify scope gaps, unrelated changes, incomplete acceptance criteria, and behavior that contradicts the stated intent.
6. Review for correctness, regressions, edge cases, security, concurrency, performance, maintainability, backward compatibility, observability, and missing tests.
7. Do not assume that passing tests prove correctness. Trace important control flow and data transformations.
8. If Jira cannot be accessed, state that limitation clearly and continue with the Bitbucket review. Do not invent Jira requirements.

## Required Output

Start with actionable findings ordered by severity:

- `Critical`
- `High`
- `Medium`
- `Low`

For every finding include:

- file and line or the smallest relevant code location;
- the concrete failure mode or risk;
- why it matters;
- a specific correction;
- a test that would detect the problem when applicable.

Then provide:

1. `Jira and PR alignment` - whether the implementation matches the Jira issue and PR description.
2. `Missing information` - unavailable Jira data, unclear requirements, or assumptions.
3. `Test gaps`.
4. `Summary` - concise overall assessment.

Do not include generic praise, style-only comments, or findings without a concrete impact.
