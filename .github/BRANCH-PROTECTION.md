# Branch protection setup (private repo)

**GitHub limitation:** Branch protection and rulesets are **not enforced** on private repos unless you have a **GitHub Team** or **Enterprise** organization account.

## Options

### 1. Upgrade to GitHub Team
Move the repo to an organization with GitHub Team ($4/user/month) — then branch protection works.

### 2. Make the repo public
Branch protection is enforced on public repos (GitHub Free). If the code can be public, this works.

### 3. Process only (no enforcement)
Use PRs and the validation workflow by convention:
- **Workflow still runs** on PRs — you’ll see build/test status
- Merge only after checks pass
- No technical block on direct pushes; rely on team discipline

### 4. Pre-push hook (local)
Add a local Git hook so developers can’t push to `main`/`development` by mistake:

```bash
# .git/hooks/pre-push (make executable: chmod +x .git/hooks/pre-push)
protected="refs/heads/main refs/heads/development"
while read local_ref local_sha remote_ref remote_sha; do
  for ref in $protected; do
    if [ "$remote_ref" = "$ref" ]; then
      echo "Direct push to $ref is not allowed. Use a PR."
      exit 1
    fi
  done
done
exit 0
```

Or use `scripts/validate-pr.ps1` before pushing.
