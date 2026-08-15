---
name: testing
description: Playbook for reviewing or improving test coverage and quality using the testing-reviewer agent.
---

# Skill: Testing

## Purpose

Assess and improve test coverage/quality for specific code, focused on behaviorally meaningful gaps rather than raw coverage percentage.

## Inputs

- The target code/project to assess.
- Whether the ask is a review only, or review plus adding tests.

## Workflow

1. **Scope**: identify the specific code/project whose tests are in question.
2. **Delegate review**: invoke [testing-reviewer](../agents/testing-reviewer.md) for a coverage/quality assessment.
3. **Prioritize public surface**: for this framework repo, prioritize coverage of public API contracts consumers rely on.
4. **If adding tests**: implement the highest-priority missing cases first, following existing test conventions in the project (framework, naming, mocking style) rather than introducing new ones.
5. **Report**: coverage summary, prioritized gaps, and (if implemented) what was added.

## Expected Outputs

- A coverage/quality assessment with concrete, scenario-level gaps.
- Optionally, new/updated tests closing the highest-priority gaps.

## Best Practices

- Don't chase 100% coverage as a goal — prioritize behavior that matters, especially public contracts.
- Match existing test framework/conventions in the project; don't introduce a second testing stack.
- Flag brittle/flaky existing tests rather than silently working around them.
