# 0002: Documentation structure and verbosity

## Status

Accepted

## Context

The root README combined application startup, API behavior and configuration,
testing, and devcontainer details. This made the information needed to run the
application harder to find and placed several audiences' instructions in one
document.

The project also needs a consistent way to keep documentation concise without
losing essential prerequisites, operational warnings, or the rationale behind
architecture decisions.

## Decision

Use the root README as a short project overview and application quickstart,
with a brief devcontainer summary and links to focused documentation.

Keep API usage and configuration in `App/README.md` and test instructions and
prerequisites in `Tests/README.md`. Documentation should be concise and
task-oriented, avoid unnecessary duplication, and retain essential safety and
operational information.

Record architecture decisions in `ADR/`. ADRs may be more detailed than
READMEs when explaining context, decisions, alternatives, and consequences;
they are not a replacement for current operational instructions.

Add these verbosity and placement expectations to the repository's Copilot
instructions so future documentation follows the same structure.

## Alternatives

- **Keep all information in the root README:** This avoids navigation between
  files but makes the quickstart harder to find as API and testing details grow.
- **Create a separate documentation directory for everything:** This provides a
  central location but separates the small API and test guides from the
  projects they describe and adds unnecessary structure at the current scale.

## Consequences

- Users can find the startup steps quickly and follow links for API or test
  details.
- Contributors must maintain links and update the appropriate guide when
  behavior or workflows change.
- Information shared across guides should be linked rather than copied where
  practical, while important prerequisites and deployment warnings remain
  visible where needed.
