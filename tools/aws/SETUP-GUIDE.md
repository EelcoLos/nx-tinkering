# AWS Nx Cache Migration - Quick Setup Guide

All code changes are complete. Follow these steps to finalize the migration.

## Summary of Changes

✅ **Completed:**
- Created CloudFormation template (`tools/aws/cloudformation/nx-cache-stack.yaml`)
- Updated `nx.json` with AWS S3 cache configuration
- Installed `@nx/s3-cache` plugin (v5.0.0)
- Uninstalled `@nx/azure-cache` plugin
- Updated GitHub Actions workflow (`.github/workflows/validate.yml`) to use AWS OIDC authentication
- Added comprehensive AWS setup documentation (`tools/aws/README.md`)

**Next Steps:** Implement AWS infrastructure and configure GitHub secrets.

---

## Step 1: Get Your AWS Account ID

You'll need this for the CloudFormation deployment and GitHub secrets.

```powershell
aws sts get-caller-identity --query 'Account' --output text
```

Save this value - you'll use it in steps 3 and 4.

---

## Step 2: Deploy CloudFormation Stack

Navigate to the workspace root and deploy:

```powershell
cd c:\Users\eelco\repos\EelcoLos\nx-tinkering

aws cloudformation create-stack `
  --stack-name nx-prod-euw1-stack `
  --template-body file://tools/aws/cloudformation/nx-cache-stack.yaml `
  --region eu-west-1 `
  --capabilities CAPABILITY_NAMED_IAM `
  --parameters `
    ParameterKey=AppName,ParameterValue=nx `
    ParameterKey=Environment,ParameterValue=prod `
    ParameterKey=RegionShorthand,ParameterValue=euw1 `
    ParameterKey=GitHubOrganization,ParameterValue=EelcoLos `
    ParameterKey=GitHubRepository,ParameterValue=nx-tinkering
```

**Wait for stack creation to complete:**

```powershell
aws cloudformation wait stack-create-complete `
  --stack-name nx-prod-euw1-stack `
  --region eu-west-1
```

**Verify success:**

```powershell
aws cloudformation describe-stacks `
  --stack-name nx-prod-euw1-stack `
  --region eu-west-1 `
  --query 'Stacks[0].StackStatus'
```

Expected output: `CREATE_COMPLETE`

---

## Step 3: Create Access Keys for the Service Account

The CloudFormation template created a dedicated **managed identity** (IAM service account) with minimal S3 permissions.

Now generate Access Keys for this account:

1. Go to **AWS Console** → **IAM** → **Users**
2. Find user: `nx-prod-euw1-nx-cache`
3. Click the user → **Security credentials** tab
4. Under **Access keys**, click **Create access key**
5. Choose **Application running outside AWS** → **Next**
6. Copy the values:
   - **Access Key ID**
   - **Secret Access Key** (this is shown only once!)

## Step 4: Configure GitHub Secrets

Add the Access Keys to GitHub repository secrets:

1. Go to GitHub repository: `https://github.com/EelcoLos/nx-tinkering`
2. **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret** twice to add:
   - **Name:** `AWS_ACCESS_KEY_ID` → **Value:** [Your Access Key ID]
   - **Name:** `AWS_SECRET_ACCESS_KEY` → **Value:** [Your Secret Access Key]
4. Click **Add secret** for each

---

## Step 5: Verify Local Setup

Test your local environment:

```powershell
# Verify AWS credentials are configured (should use the service account keys)
aws sts get-caller-identity

# Navigate to workspace
cd c:\Users\eelco\repos\EelcoLos\nx-tinkering

# Reinstall dependencies to ensure @nx/s3-cache is available
npm install

# Clear local cache
npx nx reset

# Test a build (should cache to S3)
npx nx build api-demo
```

Check for output indicating S3 cache operations.

---

## Step 6: Update or Delete Old Azure Secrets

In GitHub secrets, optionally delete Azure-related secrets (if no longer needed):
- Delete: `AZURE_GITHUB_NX_CACHE_CLIENT_ID`
- Delete: `AZURE_TENANT_ID`
- Delete: `AZURE_SUBSCRIPTION_ID`

Keep: `NX_KEY` (Nx cache encryption key)

---

## Step 7: Test in GitHub Actions

1. Commit and push your changes:
   ```powershell
   git add -A
   git commit -m "chore: migrate Nx cache from Azure to AWS S3"
   git push
   ```

2. Check GitHub Actions run:
   - Go to repository Actions tab
   - Watch the "CI" workflow run
   - Verify no authentication errors
   - Check Nx logs for cache operations (should show S3 bucket access)

3. Verify S3 bucket has cache files:
   ```powershell
   aws s3 ls s3://nx-prod-euw1-cache/ --recursive --region eu-west-1
   ```

---

## Step 8: Verify Cache Hits

Run the workflow again on a subsequent commit:

```powershell
# Make a change and push
git commit --allow-empty -m "test: verify cache hits"
git push
```

Check the Actions run output for:
- Cache hit messages in the Nx logs
- Faster build times than the first run
- S3 bucket operations in the logs

---

## Troubleshooting

### CloudFormation Stack Creation Fails

**Error: "S3 bucket already exists"**
- S3 bucket names are global. If `nx-prod-euw1-cache` exists, try a different name in the parameters.

**Error: "User is not authorized to perform: iam:CreateRole"**
- Your AWS user needs IAM permissions. Contact your AWS admin.

### GitHub Actions Fails

**Error: "InvalidUserID.Malformed" or "SignatureDoesNotMatch"**
1. Verify `AWS_ACCESS_KEY_ID` secret is set correctly
2. Verify `AWS_SECRET_ACCESS_KEY` secret is set correctly
3. Ensure Access Keys were created for the service account: `nx-prod-euw1-nx-cache`
4. Verify the Access Key is still active (not deleted)

**Error: "AccessDenied" when accessing S3**
1. Verify the service account has the S3 policy attached
2. Check CloudFormation stack outputs: `ServiceAccountArn` should exist
3. Verify bucket exists: `aws s3 ls nx-prod-euw1-cache --region eu-west-1`
4. Verify policy includes `ListBucket` action for the bucket

### Local Builds Don't Use Cache

1. Verify `@nx/s3-cache` is installed: `npm list @nx/s3-cache`
2. Check `nx.json` has correct bucket name: `nx-prod-euw1-cache`
3. Verify AWS credentials: `aws sts get-caller-identity`
4. Check environment variable `NX_KEY` is set (for cache encryption)
5. Review Nx logs for errors: `npx nx build --verbose`

---

## Cleanup (if reverting)

**To remove this setup:**

1. Delete CloudFormation stack (removes S3 bucket and IAM service account):
   ```powershell
   aws cloudformation delete-stack `
     --stack-name nx-prod-euw1-stack `
     --region eu-west-1
   ```

2. Remove GitHub secrets:
   - Go to github.com repo Settings → Secrets
   - Delete `AWS_ACCESS_KEY_ID`
   - Delete `AWS_SECRET_ACCESS_KEY`

3. Revert code changes:
   ```powershell
   git revert <commit-hash>
   ```

---

## Need Help?

- Detailed setup: See [tools/aws/README.md](README.md)
- Nx S3 Cache docs: https://nx.dev/docs/reference/remote-cache-plugins/s3-cache/overview
- AWS CLI: https://aws.amazon.com/cli/
- CloudFormation: https://docs.aws.amazon.com/cloudformation/

---

**Status:** Ready for AWS deployment and GitHub configuration → Test in CI → Monitor cache hits
