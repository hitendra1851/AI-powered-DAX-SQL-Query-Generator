# AWS Deployment Setup

One-time setup steps before the GitHub Actions workflow will run successfully.

---

## 1. Create an AWS IAM user for GitHub Actions

In AWS Console → IAM → Users → Create user:
- Username: `querymind-github-actions`
- Attach policy: `AdministratorAccess` (or scope down — see minimum permissions below)
- Create access key → note `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY`

**Minimum permissions** (if you don't want AdministratorAccess):
```
AmazonEC2FullAccess
AmazonRDSFullAccess
AmazonS3FullAccess
AmazonECR_FullAccess
AWSAppRunnerFullAccess
CloudFrontFullAccess
AWSCloudFormationFullAccess
SecretsManagerReadWrite
AmazonSSMFullAccess
IAMFullAccess
```

---

## 2. Add GitHub Secrets

In your GitHub repo → Settings → Secrets and variables → Actions → New repository secret:

| Secret Name | Value | Notes |
|---|---|---|
| `AWS_ACCESS_KEY_ID` | `AKIA...` | From IAM user created above |
| `AWS_SECRET_ACCESS_KEY` | `...` | From IAM user created above |
| `AWS_REGION` | `us-east-1` | Your preferred AWS region |
| `ANTHROPIC_API_KEY` | `sk-ant-...` | From console.anthropic.com |
| `DB_PASSWORD` | (choose a strong password) | Min 8 chars, letters + numbers |

---

## 3. Trigger deployment

Push to `main` (or `claude/querymind-ai-platform-YXjXt`) — the workflow runs automatically.

Or: GitHub → Actions → Deploy QueryMind to AWS → Run workflow.

**First deploy takes ~15–20 minutes** (RDS provisioning). Subsequent deploys: ~5 minutes.

---

## 4. What gets created in AWS

| Resource | Type | Cost estimate |
|---|---|---|
| RDS PostgreSQL t3.micro | RDS | ~$15/mo |
| App Runner (1 vCPU / 2 GB) | App Runner | ~$20–40/mo (usage-based) |
| CloudFront + S3 (frontend) | CDN | ~$1–5/mo |
| S3 (schema files) | S3 | ~$1/mo |
| ECR (Docker images) | ECR | ~$0.50/mo |
| Secrets Manager (2 secrets) | Secrets Manager | ~$0.80/mo |
| **Total estimate** | | **~$40–65/mo** |

---

## 5. After first deploy

Once the workflow completes, your CloudFront URL is shown in the GitHub Actions summary.

To find it manually:
```bash
aws cloudformation describe-stacks \
  --stack-name QueryMindStack \
  --query "Stacks[0].Outputs" \
  --output table
```

---

## 6. Tear down (stop incurring costs)

```bash
cd infrastructure
npx cdk destroy QueryMindStack
```

Note: RDS uses `RemovalPolicy.SNAPSHOT` so your data is preserved as a snapshot.
Set `deletionProtection: true` in `querymind-stack.ts` before going to production.

---

## Architecture deployed

```
Browser → CloudFront → S3 (React SPA)
                  ↓ /api/* proxy
              App Runner (API)
                  ↓ VPC connector
              RDS PostgreSQL (private subnet)
              S3 (schema files — App Runner has IAM access)
              Secrets Manager (Anthropic API key)
```
