# GATE-A-005 parser adversarial report

Result: Passed.

The strict parser rejects comments, trailing commas, duplicate top-level and nested
keys, escaped-equivalent keys, malformed UTF-8, strings beyond the individual
limit and excessive nesting. Rejections use the stable `StrictJsonException`
contract; duplicate inputs are never accepted using last-key-wins behaviour.

Proof is executable in `StrictJsonParserTests` and is rerun by the
`founder-gate-a-configuration` build target.
