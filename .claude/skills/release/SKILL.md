---
name: release
description: Cut a Rebus.Fake release. Use when asked to release, tag a version, bump the version, or publish to NuGet. Suggests a SemVer number from the commits since the last stable tag, writes the CHANGELOG entry, commits it, and tags it. Stops before pushing.
---

# Cut a release

Takes `main` from "commits have landed" to "tagged and ready to push". Pushing is the user's call, always.

An argument like `2.1.1` or `v2.1.1` pins the version. Without one, suggest a number and get agreement
before writing anything.

## Why the approval gate exists

Pushing a tag publishes to nuget.org, and **NuGet packages are immutable**. A wrong version number cannot
be recalled, only superseded. Never run `git push` in this skill. Stop at the tag and ask.

## 1. Preconditions

```bash
git rev-parse --abbrev-ref HEAD          # must be main
git status --porcelain                   # must be empty
git fetch origin && git status -sb       # must not be behind origin/main
```

Confirm CI is green **on the commit being released**, not merely on `main`. The latest run is usually
for an older commit, so compare the SHAs rather than trusting the green tick:

```bash
git rev-parse HEAD
gh run list --branch main --limit 1 --json headSha,conclusion,displayTitle
```

If `headSha` matches `HEAD` and `conclusion` is `success`, CI covers what you are releasing.

Otherwise it does not, and the usual cause is commits sitting unpushed. CI cannot be made to cover
those without pushing, which is the very thing awaiting approval, so verify locally instead:

```bash
dotnet test -c Debug
```

Add `dotnet pack -c Debug` when the diff touches `Rebus.Fake.csproj`, since package metadata is not
exercised by a test run.

A red CI run on a matching SHA, or any local failure, is a blocker. Raise it and stop. Report which of
the two paths was used and say so precisely; "CI is green" and "I built it locally" are different claims.

## 2. Find the last stable version

Prerelease tags are never cut by hand here, but filter anyway so the command stays correct:

```bash
git tag -l 'v*' --sort=-v:refname | grep -E '^v[0-9]+\.[0-9]+\.[0-9]+$' | head -1
```

## 3. Decide what actually ships

**Read the diff, not the commit messages.** Commit subjects describe intent; the package contents decide
the version number.

```bash
git diff --stat <lastTag>..HEAD
git diff <lastTag>..HEAD -- src/
```

Only these reach consumers:

| Path | Ships? | Notes |
|---|---|---|
| `src/Rebus.Fake/**/*.cs` | Yes | The assembly and its XML docs |
| `src/Rebus.Fake/Rebus.Fake.csproj` | Metadata only | `Description`, `PackageTags` and the Rebus version range are consumer-facing |
| `README.md` | Yes | Packed via `PackageReadmeFile`; it is the nuget.org landing page |
| `tests/**`, `.github/**`, `CLAUDE.md`, `global.json`, `*.slnx` | No | Repo-only, never a reason to release |

A diff touching nothing in the "Yes" rows means there is nothing to release. Say so rather than
manufacturing a version.

## 4. Suggest the number

| Bump | When |
|---|---|
| Major | A public type or member was removed, renamed, or had its signature changed. Also a raised minimum Rebus version, since the dependency range is part of the contract. |
| Minor | A new public type, member, or `Use*` configurer was added, with everything existing still working. |
| Patch | No public API change. Docs, `Description`, packaging, or an internal fix. |

Subclassers count. `FakeTransport` is public and its members are overridable, so a change to a
`protected` or `virtual` signature is breaking even when callers never see it. The 2.0.0
`OutgoingMessage` to `OutgoingTransportMessage` change is the precedent.

State the recommendation with the evidence for it, then confirm before writing. Use `AskUserQuestion`
when there is a genuine fork (for example, whether a docs-only change is worth a release at all).

## 5. Write the CHANGELOG entry

Prepend to `CHANGELOG.md`, above the previous release and below the intro paragraph. Get the date from
the machine, do not assume it:

```bash
date +%Y-%m-%d
```

```markdown
## <version> - <YYYY-MM-DD>

- <consumer-relevant change>
```

Rules, in force because the file already follows them:

- **Terse.** One line per change. No "Added", "Changed", "Fixed" section headings; the list is short enough.
- **Consumer-relevant only.** Nothing about tests, CI, coverage, or the build. If every commit in the
  range is repo-only, there is no entry to write, which means there is no release.
- **Prefix breaking changes** with `**Breaking:**`, or `**Breaking for subclasses:**` when only
  overriders are affected.
- **Anything obsoleted or removed must point somewhere.** Name the replacement API, or link the upstream
  doc that explains the move, so the reader is not left to search. For Rebus itself that is usually
  https://github.com/rebus-org/Rebus/wiki. Do not log a removal without an exit route.
- **No `-ci.*` entries.** Per-commit prereleases publish automatically and are out of scope, as the
  intro paragraph states.

## 6. Commit and tag

The tag must land on the commit that contains the CHANGELOG entry, so commit first:

```bash
git add CHANGELOG.md
git commit -m "Release <version>"
git tag v<version>
```

The `v` prefix is required. GitVersion resolves the tag on `HEAD` to exactly that stable version;
without a tag it produces `<next-patch>-ci.N` instead.

## 7. Stop and ask

Report the tag, the version, and the CHANGELOG entry. Then ask for approval to push, and hand over the
command rather than running it:

```bash
git push --atomic origin main v<version>
```

`--atomic` matters. Both refs update in one transaction, so the tag is present when either workflow run
reaches its GitVersion step and both compute the stable number. Pushing them separately is a race: if
the branch run gets there first it builds `<next-patch>-ci.N` and publishes a prerelease nobody asked
for. Two runs are expected either way, and `dotnet nuget push --skip-duplicate` absorbs the second.

If the user declines, leave the commit and tag in place. Both are local and reversible
(`git tag -d`, `git reset`). Say that plainly so the state is clear.
