# .NET Version Upgrade: .NET 9 → .NET 10

## Preferences
- **Flow Mode**: Automatic
- **Target Framework**: net10.0

## Source Control
- **Source Branch**: master
- **Working Branch**: master (git unavailable)
- **Commit Strategy**: Single Commit at End
- **Branch Sync**: Auto (Merge)

## Upgrade Options
**Source**: .github/upgrades/scenarios/dotnet-version-upgrade/upgrade-options.md

### Strategy
- Upgrade Strategy: All-at-Once

## Strategy
**Selected**: All-at-Once  
**Rationale**: Single project, already on modern .NET (net9.0 → net10.0), straightforward TFM bump with clear structure.

### Execution Constraints
- Single atomic upgrade — all work happens in one task (02-upgrade-tfm)
- Validate full solution build after upgrade
- No incremental buildability required — temporarily broken solution is acceptable during upgrade
- Commit all changes together at the end
