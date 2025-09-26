param(
    [Parameter(Mandatory=$true)]
    [string]$VersionFrom,

    [Parameter(Mandatory=$true)]
    [string]$VersionTo
)

# Get repository URL from git remote
$repoUrl = (git remote get-url origin) -replace '\.git$', ''
if ($repoUrl -match '^git@github\.com:(.+)') {
    $repoUrl = "https://github.com/$($matches[1])"
}

# Clear existing release.md if it exists
if (Test-Path "release.md") {
    Remove-Item "release.md"
}

# Add comparison section
Add-Content -Path "release.md" -Value "## Comparison"
Add-Content -Path "release.md" -Value "See every change: [Compare $VersionFrom...$VersionTo]($repoUrl/compare/$VersionFrom...$VersionTo)"
Add-Content -Path "release.md" -Value ""
Add-Content -Path "release.md" -Value "---"
Add-Content -Path "release.md" -Value ""

# Add commit summary section
Add-Content -Path "release.md" -Value "## Commit Summary (Newest → Oldest)"

# Get commit SHAs from previous version to HEAD
$commits = git log --pretty=format:'%H' "$VersionFrom..HEAD"

foreach ($sha in $commits) {
    # Get username via GitHub API
    $repoPath = ($repoUrl -replace 'https://github.com/', '')
    try {
        $commitData = gh api "repos/$repoPath/commits/$sha" --jq '.author.login // .commit.author.name' 2>$null
        $username = $commitData
    }
    catch {
        # Fallback to git commit author name if GitHub API fails
        $username = git log -1 --pretty=format:'%an' $sha
    }

    # Get commit message and short SHA
    $message = git log -1 --pretty=format:'%s' $sha
    $shortSha = git log -1 --pretty=format:'%h' $sha

    # Add commit line to release.md
    Add-Content -Path "release.md" -Value "- [$shortSha]($repoUrl/commit/$sha) — $message by @$username"
}

Write-Host "Release notes generated successfully in release.md"