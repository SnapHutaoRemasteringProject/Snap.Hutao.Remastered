$ErrorActionPreference = 'Stop'

$repository = $env:GITHUB_REPOSITORY
$triggerSha = $env:GITHUB_SHA
if ([string]::IsNullOrWhiteSpace($repository) -or [string]::IsNullOrWhiteSpace($triggerSha)) {
    throw 'GITHUB_REPOSITORY and GITHUB_SHA are required.'
}

git config user.name 'github-actions[bot]'
git config user.email 'github-actions[bot]@users.noreply.github.com'

git fetch origin '+refs/heads/*:refs/remotes/origin/*'
if ($LASTEXITCODE -ne 0) { throw "git fetch failed with exit code $LASTEXITCODE" }

git checkout origin/dev
if ($LASTEXITCODE -ne 0) { throw "git checkout origin/dev failed with exit code $LASTEXITCODE" }

# A push can start before its PR exists. Merge the exact triggering commit even
# when the open-PR list does not contain its branch yet.
git merge-base --is-ancestor $triggerSha HEAD
$triggerAlreadyIncluded = $LASTEXITCODE -eq 0
if ($LASTEXITCODE -notin 0, 1) { throw "Cannot inspect triggering commit $triggerSha" }
if (-not $triggerAlreadyIncluded) {
    Write-Host "Merging triggering commit $triggerSha into dev"
    git merge $triggerSha --strategy=ort --allow-unrelated-histories -m "Merge triggering commit $triggerSha into dev"
    if ($LASTEXITCODE -ne 0) { throw "Merging triggering commit $triggerSha failed with exit code $LASTEXITCODE" }
}

$headers = @{
    Authorization = "Bearer $env:GH_TOKEN"
    Accept = 'application/vnd.github+json'
}
try {
    $pulls = Invoke-RestMethod -Uri "https://api.github.com/repos/$repository/pulls?state=open&per_page=100" -Headers $headers
}
catch {
    throw "Cannot list open pull requests of $repository (rate limited or insufficient permissions): $($_.Exception.Message)"
}

$refs = @()
foreach ($pull in $pulls) {
    if ($pull.draft) { continue }
    if ($pull.head.repo.full_name -ne $repository) {
        Write-Host "Skipping #$($pull.number): head branch lives in $($pull.head.repo.full_name), not in $repository"
        continue
    }
    # The trigger was already merged by SHA. A subsequent push must not silently
    # replace the revision that this run is supposed to validate.
    if ($env:GITHUB_REF -eq "refs/heads/$($pull.head.ref)") { continue }
    $refs += $pull.head.ref
}

if ($triggerAlreadyIncluded -and ($refs.Count -eq 0 -or ($refs.Count -eq 1 -and $refs[0] -eq 'l10n_dev'))) {
    Write-Host 'No new triggering changes or PRs to merge'
    'continue=false' | Out-File -FilePath $env:GITHUB_OUTPUT -Append
    exit 0
}

foreach ($ref in $refs) {
    Write-Host "Merging $ref into dev"
    git merge "origin/$ref" --strategy=ort --allow-unrelated-histories -m "Merge $ref into dev"
    if ($LASTEXITCODE -ne 0) { throw "Merging origin/$ref into dev failed with exit code $LASTEXITCODE" }
}

git merge-base --is-ancestor $triggerSha HEAD
if ($LASTEXITCODE -ne 0) { throw "Canary source does not contain triggering commit $triggerSha" }
$sourceSha = git rev-parse HEAD
if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve the compiled source commit' }

Write-Host "Verified Canary source: $sourceSha (contains triggering commit $triggerSha)"
"source-sha=$sourceSha" | Out-File -FilePath $env:GITHUB_OUTPUT -Append
'continue=true' | Out-File -FilePath $env:GITHUB_OUTPUT -Append
@"
### Canary source

- Triggering commit: $triggerSha
- Compiled source commit: $sourceSha
- Verified that the compiled source contains the triggering commit.
"@ | Out-File -FilePath $env:GITHUB_STEP_SUMMARY -Append
