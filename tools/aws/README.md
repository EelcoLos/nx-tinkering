# AWS Infrastructure for Nx Distributed Cache

This directory contains Infrastructure as Code (CloudFormation) for AWS resources supporting the Nx distributed cache using S3 and GitHub Actions OIDC authentication.

## Quick Start

### Prerequisites

1. **AWS CLI** - [Install AWS CLI v2](https://aws.amazon.com/cli/)
   ```powershell
   # Windows: Download and install from https://aws.amazon.com/cli/
   # Or via package manager (choco, scoop, winget)
   winget install Amazon.AWSCLI
   
   # Verify installation
   aws --version
   ```

2. **AWS Account Access** - Ensure you have:
   - AWS Account with appropriate IAM permissions (create S3 buckets, IAM roles, CloudFormation stacks)
   - AWS Access Key and Secret Key (or use SSO/temporary credentials)

3. **GitHub Repository** - Access to:
   - Owner/Admin role in the GitHub repository
   - Ability to create/manage GitHub secrets

### 1. Configure AWS Credentials

```powershell
aws configure
```

When prompted:
- **AWS Access Key ID**: Your AWS access key
- **AWS Secret Access Key**: Your AWS secret key
- **Default region**: `eu-west-1` (Ireland)
- **Default output format**: `json`

Verify configuration:
```powershell
aws sts get-caller-identity
```

Expected output shows your AWS Account ID, User ARN, and Arn.

### 2. Deploy CloudFormation Stack

```powershell
# Navigate to workspace root
cd c:\Users\eelco\repos\EelcoLos\nx-tinkering

# Deploy stack with default parameters
aws cloudformation create-stack `
  --stack-name nx-prod-euw1-stack `
  --template-body file://tools/aws/cloudformation/nx-cache-stack.yaml `
  --region eu-west-1 `
  --parameters `
    ParameterKey=AppName,ParameterValue=nx `
    ParameterKey=Environment,ParameterValue=prod `
    ParameterKey=RegionShorthand,ParameterValue=euw1 `
    ParameterKey=GitHubOrganization,ParameterValue=EelcoLos `
    ParameterKey=GitHubRepository,ParameterValue=nx-tinkering
```

Monitor stack creation:
```powershell
aws cloudformation wait stack-create-complete `
  --stack-name nx-prod-euw1-stack `
  --region eu-west-1

# Check status
aws cloudformation describe-stacks `
  --stack-name nx-prod-euw1-stack `
  --region eu-west-1 `
  --query 'Stacks[0].StackStatus'
```

### 3. Retrieve Stack Outputs

```powershell
aws cloudformation describe-stacks `
  --stack-name nx-prod-euw1-stack `
  --region eu-west-1 `
  --query 'Stacks[0].Outputs' `
  --output table
```

Key outputs to note:
- **BucketName**: S3 bucket name (e.g., `nx-prod-euw1-cache`)
- **RoleArn**: IAM role ARN for GitHub Actions (format: `arn:aws:iam::ACCOUNT_ID:role/nx-prod-euw1-github-actions`)
- **AwsAccountId**: Your AWS Account ID

### 4. Update GitHub Actions Workflow

In [.github/workflows/validate.yml](.github/workflows/validate.yml):

1. Add the Role ARN to the `configure-aws-credentials` step:
   ```yaml
   - name: Configure AWS credentials
     uses: aws-actions/configure-aws-credentials@v4
     with:
       role-to-assume: arn:aws:iam::ACCOUNT_ID:role/nx-prod-euw1-github-actions
       aws-region: eu-west-1
   ```
   Replace `ACCOUNT_ID` with the output from step 3.

2. Remove Azure login step and related Azure secrets.

3. Keep `NX_KEY` GitHub secret if used for Nx cache encryption.

### 5. Verify the Setup

**Local:**
```powershell
# Clear local cache
npx nx reset

# Run a build to verify cache operations
npx nx build api-demo

# Check cache connectivity (if available in Nx)
npx nx cache -- --list
```

**AWS CLI:**
```powershell
# List bucket contents
aws s3 ls s3://nx-prod-euw1-cache/

# Verify bucket exists and is accessible
aws s3api head-bucket --bucket nx-prod-euw1-cache --region eu-west-1
```

**GitHub Actions:**
- Push a commit to trigger the workflow
- Monitor the Actions tab in GitHub
- Check Nx logs for cache hit/miss messages
- Verify S3 bucket receives cache files

## File Structure

```
tools/aws/
├── README.md (this file)
├── cloudformation/
│   └── nx-cache-stack.yaml       # CloudFormation template
└── scripts/
    └── deploy.ps1               # (Optional) Deployment helper script
```

## CloudFormation Template Details

### Parameters

| Parameter            | Default        | Description                                   |
|----------------------|----------------|-----------------------------------------------|
| `AppName`            | `nx`           | Application name (lowercase, hyphens allowed) |
| `Environment`        | `prod`         | Environment: `dev`, `staging`, `prod`         |
| `RegionShorthand`    | `euw1`         | Region shorthand (e.g., `euw1` for eu-west-1) |
| `GitHubOrganization` | `EelcoLos`     | GitHub org name                               |
| `GitHubRepository`   | `nx-tinkering` | GitHub repo name                              |
| `Owner`              | `EelcoLos`     | Resource owner (for tagging)                  |
| `CostCenter`         | `engineering`  | Cost center (for tagging)                     |

### Resources Created

1. **S3 Bucket** (`nx-{environment}-{region}-cache`)
   - Versioning enabled
   - AES-256 encryption
   - Public access blocked
   - 30-day lifecycle policy (auto-cleanup old cache objects)

2. **IAM OIDC Provider** (for GitHub Actions)
   - Trusts `token.actions.githubusercontent.com`
   - Scoped to specific GitHub repository

3. **IAM Role** (`nx-{environment}-{region}-github-actions`)
   - Assumable only via GitHub Actions OIDC
   - Scoped to specific repository (`repo:org/repo:*`)

4. **IAM Policy** (minimal S3 permissions)
   - `s3:GetObject`, `s3:PutObject`, `s3:DeleteObject`
   - `s3:ListBucket` on the cache bucket only

5. **Tags** (applied to all resources)
   - `Application`: `nx-cache`
   - `Environment`: Provided parameter value
   - `Project`: `nx-tinkering`
   - `Owner`: Provided parameter value
   - `CostCenter`: Provided parameter value
   - `ManagedBy`: `CloudFormation`

## Management Commands

### View Stack Status
```powershell
aws cloudformation describe-stacks `
  --stack-name nx-prod-euw1-stack `
  --region eu-west-1
```

### Update Stack (if template changes)
```powershell
aws cloudformation update-stack `
  --stack-name nx-prod-euw1-stack `
  --template-body file://tools/aws/cloudformation/nx-cache-stack.yaml `
  --region eu-west-1 `
  --parameters `
    ParameterKey=AppName,ParameterValue=nx `
    ParameterKey=Environment,ParameterValue=prod `
    ParameterKey=RegionShorthand,ParameterValue=euw1 `
    ParameterKey=GitHubOrganization,ParameterValue=EelcoLos `
    ParameterKey=GitHubRepository,ParameterValue=nx-tinkering

aws cloudformation wait stack-update-complete `
  --stack-name nx-prod-euw1-stack `
  --region eu-west-1
```

### Delete Stack (cleanup)
```powershell
# This will delete the S3 bucket, IAM role, and OIDC provider
aws cloudformation delete-stack `
  --stack-name nx-prod-euw1-stack `
  --region eu-west-1

aws cloudformation wait stack-delete-complete `
  --stack-name nx-prod-euw1-stack `
  --region eu-west-1
```

## Troubleshooting

### Stack creation fails

**Error: "S3 bucket already exists"**
- S3 bucket names are globally unique. If the bucket name is taken, modify the `RegionShorthand` or `Environment` parameter.

**Error: "User is not authorized to perform: iam:CreateRole"**
- Your AWS user needs IAM permissions. Contact your AWS admin or ensure your user has `iam:*` and `s3:*` permissions.

### GitHub Actions workflow fails

**Error: "Unable to assume role"**
- Verify the Role ARN in the workflow is correct (output from step 3)
- Ensure the GitHub repository owner matches `GitHubOrganization` parameter
- Check GitHub Actions token permissions (Settings → Actions → General → Workflow permissions)

**Cache not being created in S3**
- Verify bucket permissions: `aws s3api get-bucket-acl --bucket nx-prod-euw1-cache`
- Check bucket policy: `aws s3api get-bucket-policy --bucket nx-prod-euw1-cache`
- Verify IAM role has S3 permissions: `aws iam get-role-policy --role-name nx-prod-euw1-github-actions --policy-name nx-prod-euw1-s3-policy`

### Local build issues

**Error: "Access Denied" when trying to read cache**
- Verify AWS credentials are configured: `aws sts get-caller-identity`
- Ensure `@nx/aws` plugin is installed: `npm list @nx/aws`
- Check Nx configuration in `nx.json` has `aws` block

## References

- [Nx S3 Cache Plugin Documentation](https://nx.dev/docs/reference/remote-cache-plugins/s3-cache/overview)
- [AWS CloudFormation User Guide](https://docs.aws.amazon.com/cloudformation/)
- [GitHub Actions OIDC with AWS](https://docs.github.com/en/actions/deployment/security-hardening-your-deployments/about-security-hardening-with-openid-connect)

## Cost Considerations

**Estimated Monthly Cost (light usage):**
- **S3 Storage**: ~$0.01-0.10 (depending on cache size; 30-day TTL keeps it minimal)
- **S3 Data Transfer**: ~$0.00 (within AWS, no egress charges to same region)
- **IAM**: FREE
- **CloudFormation**: FREE

**Cost Optimization:**
- Cache objects auto-delete after 30 days (lifecycle policy)
- Consider using S3 Intelligent-Tiering if cache grows significantly

## Support

For issues with this infrastructure:
1. Check CloudFormation stack events: `aws cloudformation describe-stack-events --stack-name nx-prod-euw1-stack --region eu-west-1`
2. Review AWS CloudTrail for API calls: https://console.aws.amazon.com/cloudtrail
3. Check GitHub Actions logs in the repository Actions tab
4. Contact your AWS administrator for permission issues
