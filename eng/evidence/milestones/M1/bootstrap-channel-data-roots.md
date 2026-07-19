# FND-006 Channel Data-Root Report

**Result:** Passed

Bootstrap derives three non-colliding per-user roots:

`	ext
<LocalApplicationData>\Opure\Stable
<LocalApplicationData>\Opure\Preview
<LocalApplicationData>\Opure\Development
`

## Enforcement

- the root is absolute;
- the channel name is an exact path segment;
- Runtime validates that the received root matches the received channel;
- Bootstrap does not create or mutate the root;
- session material is passed only through bounded child-process environment variables;
- session material is not written to evidence or command lines.