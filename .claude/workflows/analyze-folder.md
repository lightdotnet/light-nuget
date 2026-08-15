# Workflow: Analyze Folder

Triggered by requests like "analyze this folder" naming a specific directory.

## Steps

1. **Confirm the exact folder path.** If ambiguous (e.g. multiple folders with similar names across solutions), ask.
2. **Analyze only that folder.** Do not inspect parent, sibling, or unrelated folders. If the folder references something outside itself (e.g. a project reference), note the reference but don't fully analyze the target unless it's needed to answer the request.
3. **Read files within the folder as needed** — prefer `Glob`/`Grep` to identify structure before reading full file contents.
4. **Generate or update documentation only if requested**:
   - If asked, write/update a scoped doc under `.claude/docs/generated/<path>/` using the appropriate template from `.claude/docs/templates/` (project-overview, or a custom scope-appropriate template).
   - If not asked, just report findings back conversationally — do not write files.
5. **Do not inspect unrelated folders** even if they seem "related" — only expand scope if the user asks or if answering the specific question truly requires it, and flag the expansion explicitly before doing it.

## Output

- A summary of the folder's contents/purpose/structure, scoped strictly to what was asked.
- Optionally, a generated/updated doc file (only if requested).
