# MCP Smoke Runbook

This runbook defines a stable verification sequence for Unity MCP checks.

## Play toggle paths

Use these menu paths in order:

1. `Edit/Play`
2. Fallback: `Window/General/Game`, then retry `Edit/Play`
3. If both fail, switch to manual Play verification in the Unity Editor

Do not use `Edit/Playmode`.

## Smoke sequence

1. Recompile scripts
2. Read console `error` and `warning` logs
3. Attempt to enter Play mode with the path order above
4. Read console logs again
5. Attempt to exit Play mode with the same path order

## Expected outcomes

- No compile errors
- No runtime exceptions during smoke
- If Play cannot be toggled by MCP, report `manual play required` instead of treating it as a code failure
