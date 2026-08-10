# Upgrade Options — Emergency Passport Tracker

Assessment: 1 project (net9.0-windows → net10.0-windows), Windows Forms app, 1 deprecated package (itext7)

## Strategy

### Upgrade Strategy
Single project with straightforward TFM update (net9.0 → net10.0) - all-at-once is the natural approach.

| Value | Description |
|-------|-------------|
| **All-at-Once** (selected) | Upgrade the project in a single atomic pass - fastest for single-project solutions |
