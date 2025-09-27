---
    description: General configuration for AI behavior and workflow requirements
    applyTo: '**'
---
---
# General Copilot Instructions
Configuration for AI behavior and workflow requirements in this repository.

## CRITICAL: Git Workflow Requirements

### Always Commit and Push Changes
- **MANDATORY**: After making ANY file changes, modifications, or additions, you MUST:
  1. Stage all changes using `git add .` or `git add <specific-files>`
  2. Commit changes with a descriptive commit message using `git commit -m "descriptive message"`
  3. Push changes to the remote repository using `git push`
- This applies to ALL changes including:
  - Code modifications
  - New file creation
  - Configuration updates
  - Documentation changes
  - Workflow file additions/modifications
  - Any other file system changes
- **NO EXCEPTIONS**: Do not ask for permission to commit and push - always do it automatically
- Use clear, descriptive commit messages that explain what was changed and why

### Commit Message Guidelines
- Use present tense ("Add feature" not "Added feature")
- Use imperative mood ("Move cursor to..." not "Moves cursor to...")
- Keep the first line under 50 characters
- Reference issues or PRs when applicable
- Examples:
  - "Add GitHub workflow for Copilot setup steps"
  - "Update instructions to require automatic commits"
  - "Fix configuration issue in appsettings.json"

### Git Workflow Process
1. Make your changes
2. Run any required analysis or validation
3. Stage changes: `git add .`
4. Commit with descriptive message: `git commit -m "your message"`
5. Push to remote: `git push`
6. Confirm push was successful

## General Development Guidelines
- Follow the existing code style and patterns in the repository
- Test changes locally when possible before committing
- Keep commits focused and atomic (one logical change per commit)
- Document significant changes in commit messages
---