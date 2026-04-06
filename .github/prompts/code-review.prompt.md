---
mode: ask
description: Review the current changes for correctness, security, and coding standards.
---

Please review the changes in the current working tree (or the diff provided) and report:

1. **Correctness** – Are there any logic errors, off-by-one errors, or missing null checks?
2. **Security** – Are there any injection risks, hardcoded secrets, missing input validation, or insecure defaults?
3. **Performance** – Are there any obvious performance issues (e.g., N+1 queries, synchronous I/O in async paths)?
4. **Coding standards** – Do the changes follow the conventions in `.github/copilot-instructions.md` and `.github/instructions/csharp.instructions.md`?
5. **Test coverage** – Are the new or changed code paths covered by tests per `.github/instructions/tests.instructions.md`?

For each finding, state:
- **File and line range** (if applicable)
- **Severity**: `critical` | `major` | `minor` | `suggestion`
- **Description** and **recommended fix**

Be concise. Only flag real issues – do not invent problems to appear thorough.
