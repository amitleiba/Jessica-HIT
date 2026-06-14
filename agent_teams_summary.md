---
name: agent-teams-summary
description: "Summary of Claude Code Agent Teams feature — what it does, how to activate, key capabilities and limitations"
metadata: 
  node_type: memory
  type: reference
  originSessionId: ecf8e606-e634-4eed-b976-7b0a23b8db7c
---

# Claude Code — Agent Teams Summary

Source: https://code.claude.com/docs/en/agent-teams

## What It Is

Agent teams let you coordinate multiple independent Claude Code instances working together. One session is the **team lead** that coordinates work and assigns tasks; the rest are **teammates** that work in parallel, each with its own context window.

Unlike subagents (which only report back to the main agent), **teammates can message each other directly** and share a task list.

Requires Claude Code **v2.1.32 or later**.

---

## How to Activate

**1. Check your version:**
```bash
claude --version
```

**2. Enable the feature flag in `~/.claude/settings.json`:**
```json
{
  "env": {
    "CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS": "1"
  }
}
```

**3. Start a team in natural language:**
```
Create an agent team with 3 teammates: one for frontend, one for backend,
one for tests. Refactor the auth module.
```

---

## What You Can Do

| Capability | Description |
|---|---|
| **Parallel research** | Multiple teammates investigate different angles simultaneously |
| **Parallel implementation** | Each teammate owns separate files/modules — no conflicts |
| **Competing hypotheses** | Teammates actively try to disprove each other (great for debugging) |
| **Cross-layer work** | Frontend, backend, and tests each owned by a different teammate |
| **Plan approval gates** | Require teammates to plan before implementing; lead approves/rejects |
| **Direct teammate messaging** | Talk to any individual teammate without going through the lead |
| **Quality hooks** | `TeammateIdle`, `TaskCreated`, `TaskCompleted` hooks to enforce rules |
| **Task dependencies** | Tasks auto-unblock when their dependencies complete |
| **Subagent definitions** | Reuse named agent types (e.g. security-reviewer) as teammates |

---

## Display Modes

- **In-process** (default): all teammates in your main terminal. Use **Shift+Down** to cycle between them.
- **Split panes**: each teammate gets its own pane. Requires `tmux` or iTerm2.
  - **Not supported in VS Code's integrated terminal.**

To force in-process mode:
```bash
claude --teammate-mode in-process
```

Or set in `~/.claude/settings.json`:
```json
{
  "teammateMode": "in-process"
}
```

---

## Best Practices

- Start with **3–5 teammates** — coordination overhead grows with team size
- Aim for **5–6 tasks per teammate**
- Give each teammate distinct file ownership to avoid conflicts
- Include task-specific context in the spawn prompt (teammates don't inherit lead's conversation history)
- Start with research/review tasks before attempting parallel implementation
- Monitor and steer — don't let the team run unattended too long

---

## Key Limitations

- **Experimental** — disabled by default
- **No session resumption** — `/resume` and `/rewind` do not restore in-process teammates
- **One team at a time** — clean up before creating a new one
- **No nested teams** — teammates cannot spawn their own sub-teams
- **Lead is fixed** — the session that creates the team is always the lead
- **Split panes** require `tmux` or iTerm2 — not supported in VS Code terminal
- **Token cost** scales linearly with teammates — each teammate is a full Claude instance
- Task status can lag — teammates sometimes fail to mark tasks complete

---

## Subagents vs Agent Teams (Quick Comparison)

| | Subagents | Agent Teams |
|---|---|---|
| **Communication** | Report to main agent only | Teammates message each other |
| **Coordination** | Main agent manages all work | Shared task list, self-coordinating |
| **Token cost** | Lower | Higher (each teammate = full instance) |
| **Best for** | Focused tasks, result matters | Complex work needing discussion |
