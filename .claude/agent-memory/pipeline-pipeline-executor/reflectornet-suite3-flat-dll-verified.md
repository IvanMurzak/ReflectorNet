---
name: reflectornet-suite3-flat-dll-verified
description: implement-task reflectornet profile Suite 3 (Unity EditMode) verification path works end-to-end with the flat-DLL fallback; concrete confirmation of the heavy cross-repo verification flow
metadata:
  type: project
---

The `implement-task` `reflectornet` profile's full three-suite verification (incl. the heavy Suite 3 Unity EditMode) runs cleanly end-to-end in a worktree.

**Why:** Suite 3 is conditional and delicate (DLL propagation + Unity Editor bring-up); confirming the documented path actually works removes hesitation about running it for ReflectorNet diffs that touch `src/Converter/` or JSON schema integration.

**How to apply:** For a ReflectorNet diff touching Converter/schema/binder paths, the conservative "run Suite 3" choice is feasible and worth doing. The working sequence (issue #82 run):
1. Suite 1: `dotnet test -c Release` in `<wt>/ReflectorNet` — both net8.0 + net9.0.
2. Suite 2: pack ReflectorNet with `-p:Version=<base>-dev-<sha>-<epoch>` (NOT `--version-suffix` — explicit `<Version>` ignores it), write `<wt>/MCP-Plugin-dotnet/nuget.config` local feed, perl-rewrite the 3 consumer csproj `PackageReference Version=`, then `dotnet restore && build && test --no-build`.
3. Suite 3: `cli.py build-plugins --dry-run` reports **0 destinations** (current Unity submodule uses the FLAT layout `Assets/Plugins/NuGet/ReflectorNet.dll`, no versioned dir) — this is expected, NOT a broken setup. Fall back to `propagate-flat-dll.py --src <wt>/ReflectorNet/ReflectorNet/bin/Release/netstandard2.1/ReflectorNet.dll --unity-root <wt>/Unity-MCP --dll-name ReflectorNet.dll --expected-count 6` (copies + sha256-verifies 6 dests: 1 plugin + 5 Unity-Tests versions). Use the **netstandard2.1** build (that's what Unity loads), not net8/net9.
4. Pin `<unity_project>/UserSettings/AI-Game-Developer-Config.json` (connectionMode=Custom, host=rstripped UNITY_MCP_CLOUD_URL, token, timeoutMs=1800000) BEFORE `unity-mcp-cli open`, then `wait-for-ready`, then `run-tool tests-run ... {"testMode":"EditMode"}`.

Verifier dirtiness in `<wt>/MCP-Plugin-dotnet/` (csproj Version=, nuget.config) and `<wt>/Unity-MCP/` (the 6 propagated DLLs) is expected and MUST NOT be committed — only `<wt>/ReflectorNet/` paths get staged. See [[fork-pr-worktree-push-target]] (not always applicable — for a plain upstream branch `origin` IS the target repo and `git push -u origin <branch>` is correct).

**Re-confirmed (issue #84, PR #85, 2026-06-02):** identical full-three-suite flow worked again. Base `<Version>` had drifted to `5.2.0` (read it from ReflectorNet.csproj, never assume). Counts this run: Suite 1 1450/1450 (both TFMs), Suite 2 268/268 + 409/409, Suite 3 EditMode 953/953. propagate-flat-dll.py src sha256 95525bae… across 6 dests, exit 0. Unity bring-up clean (Editor 2022.3.62f3, wait-for-ready fast, negotiate probe returned connectionId). Two independent ReflectorNet implement-task chains (#82 and #84) coexisted in separate worktrees (slots 43 and 47) without interference.
