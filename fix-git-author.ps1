# Git Author and Email Fix Script
# 用于修改 Git 历史提交中的作者信息

param(
    [Parameter(Mandatory=$true)]
    [string]$OldEmail,
    
    [Parameter(Mandatory=$true)]
    [string]$NewName,
    
    [Parameter(Mandatory=$true)]
    [string]$NewEmail,
    
    [switch]$DryRun,
    [switch]$Force
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Git Author Information Fix Tool" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Old Email: $OldEmail" -ForegroundColor White
Write-Host "  New Name:  $NewName" -ForegroundColor Green
Write-Host "  New Email: $NewEmail" -ForegroundColor Green
Write-Host ""

# 检查是否有未提交的更改
$status = git status --porcelain
if ($status) {
    Write-Host "⚠️  Warning: You have uncommitted changes!" -ForegroundColor Red
    Write-Host "Please commit or stash your changes before running this script." -ForegroundColor Red
    exit 1
}

# 检查是否在 Git 仓库中
$gitRepo = git rev-parse --git-dir 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Error: Not a git repository!" -ForegroundColor Red
    exit 1
}

Write-Host "📊 Analyzing commits with email: $OldEmail" -ForegroundColor Cyan
$affectedCommits = git log --all --pretty=format:"%H %ae %an" | Where-Object { $_ -match $OldEmail }
$commitCount = ($affectedCommits | Measure-Object).Count

if ($commitCount -eq 0) {
    Write-Host "✅ No commits found with email: $OldEmail" -ForegroundColor Green
    exit 0
}

Write-Host "Found $commitCount commit(s) to modify:" -ForegroundColor Yellow
$affectedCommits | ForEach-Object {
    $parts = $_ -split ' ', 3
    Write-Host "  - $($parts[0].Substring(0,8)) | $($parts[1]) | $($parts[2])" -ForegroundColor Gray
}
Write-Host ""

if ($DryRun) {
    Write-Host "🔍 Dry run mode - no changes will be made" -ForegroundColor Cyan
    exit 0
}

if (-not $Force) {
    Write-Host "⚠️  WARNING: This will rewrite Git history!" -ForegroundColor Red
    Write-Host "   - All commit SHAs will change" -ForegroundColor Yellow
    Write-Host "   - You will need to force push to remote repositories" -ForegroundColor Yellow
    Write-Host "   - Other collaborators will need to rebase their work" -ForegroundColor Yellow
    Write-Host ""
    $confirmation = Read-Host "Are you sure you want to continue? (type 'yes' to confirm)"
    
    if ($confirmation -ne 'yes') {
        Write-Host "❌ Operation cancelled" -ForegroundColor Yellow
        exit 0
    }
}

Write-Host ""
Write-Host "🔄 Starting to rewrite Git history..." -ForegroundColor Cyan

# 创建备份标签
$backupTag = "backup-before-author-fix-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
git tag $backupTag
Write-Host "📌 Created backup tag: $backupTag" -ForegroundColor Green

# 执行 filter-branch
$filterScript = @"
OLD_EMAIL="$OldEmail"
CORRECT_NAME="$NewName"
CORRECT_EMAIL="$NewEmail"
if [ "`$GIT_COMMITTER_EMAIL" = "`$OLD_EMAIL" ]
then
    export GIT_COMMITTER_NAME="`$CORRECT_NAME"
    export GIT_COMMITTER_EMAIL="`$CORRECT_EMAIL"
fi
if [ "`$GIT_AUTHOR_EMAIL" = "`$OLD_EMAIL" ]
then
    export GIT_AUTHOR_NAME="`$CORRECT_NAME"
    export GIT_AUTHOR_EMAIL="`$CORRECT_EMAIL"
fi
"@

# 使用 bash 执行 filter-branch（需要 Git Bash）
$env:FILTER_BRANCH_SQUELCH_WARNING = "1"
git filter-branch -f --env-filter $filterScript --tag-name-filter cat -- --branches --tags

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ Successfully rewrote Git history!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "  1. Verify the changes with: git log --all --pretty=format:'%h %an <%ae>' | head -20" -ForegroundColor White
    Write-Host "  2. Force push to remote: git push origin --force --all" -ForegroundColor White
    Write-Host "  3. Force push tags: git push origin --force --tags" -ForegroundColor White
    Write-Host ""
    Write-Host "💾 Backup tag created: $backupTag" -ForegroundColor Cyan
    Write-Host "   To restore: git reset --hard $backupTag" -ForegroundColor Gray
} else {
    Write-Host ""
    Write-Host "❌ Error occurred during rewrite!" -ForegroundColor Red
    Write-Host "   You can restore from backup: git reset --hard $backupTag" -ForegroundColor Yellow
    exit 1
}

# 清理 refs
Write-Host ""
Write-Host "🧹 Cleaning up..." -ForegroundColor Cyan
git for-each-ref --format="delete %(refname)" refs/original | git update-ref --stdin
git reflog expire --expire=now --all
git gc --prune=now --aggressive

Write-Host "✨ Done!" -ForegroundColor Green
