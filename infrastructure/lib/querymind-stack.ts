import * as cdk from 'aws-cdk-lib';
import * as ec2 from 'aws-cdk-lib/aws-ec2';
import * as rds from 'aws-cdk-lib/aws-rds';
import * as s3 from 'aws-cdk-lib/aws-s3';
import * as ecr from 'aws-cdk-lib/aws-ecr';
import * as iam from 'aws-cdk-lib/aws-iam';
import * as cloudfront from 'aws-cdk-lib/aws-cloudfront';
import * as origins from 'aws-cdk-lib/aws-cloudfront-origins';
import * as secretsmanager from 'aws-cdk-lib/aws-secretsmanager';
import * as ssm from 'aws-cdk-lib/aws-ssm';
import * as apprunner from 'aws-cdk-lib/aws-apprunner';
import { Construct } from 'constructs';

export class QueryMindStack extends cdk.Stack {
  constructor(scope: Construct, id: string, props?: cdk.StackProps) {
    super(scope, id, props);

    const imageTag = this.node.tryGetContext('imageTag') ?? 'latest';

    // ── Secrets ─────────────────────────────────────────────────────────────
    const anthropicSecret = new secretsmanager.Secret(this, 'AnthropicSecret', {
      secretName: '/querymind/anthropic-api-key',
      description: 'Anthropic API key for QueryMind AI',
      secretStringValue: cdk.SecretValue.unsafePlainText(
        process.env.ANTHROPIC_API_KEY ?? 'REPLACE_ME'
      )
    });

    const dbPasswordSecret = new secretsmanager.Secret(this, 'DbPasswordSecret', {
      secretName: '/querymind/db-password',
      description: 'PostgreSQL admin password',
      secretStringValue: cdk.SecretValue.unsafePlainText(
        process.env.DB_PASSWORD ?? 'REPLACE_ME'
      )
    });

    // ── VPC ─────────────────────────────────────────────────────────────────
    // No NAT gateway — App Runner has its own internet access, RDS is isolated
    const vpc = new ec2.Vpc(this, 'Vpc', {
      maxAzs: 2,
      natGateways: 0,
      subnetConfiguration: [
        { name: 'Public', subnetType: ec2.SubnetType.PUBLIC, cidrMask: 24 },
        { name: 'Private', subnetType: ec2.SubnetType.PRIVATE_ISOLATED, cidrMask: 24 }
      ]
    });

    // ── Security Groups ──────────────────────────────────────────────────────
    const dbSg = new ec2.SecurityGroup(this, 'DbSg', {
      vpc,
      description: 'QueryMind RDS PostgreSQL'
    });

    const appRunnerSg = new ec2.SecurityGroup(this, 'AppRunnerSg', {
      vpc,
      description: 'QueryMind App Runner VPC connector'
    });

    // App Runner VPC connector → RDS
    dbSg.addIngressRule(appRunnerSg, ec2.Port.tcp(5432), 'Allow App Runner to reach RDS');

    // ── RDS PostgreSQL ───────────────────────────────────────────────────────
    const dbInstance = new rds.DatabaseInstance(this, 'Postgres', {
      engine: rds.DatabaseInstanceEngine.postgres({
        version: rds.PostgresEngineVersion.VER_16
      }),
      instanceType: ec2.InstanceType.of(ec2.InstanceClass.T3, ec2.InstanceSize.MICRO),
      vpc,
      vpcSubnets: { subnetType: ec2.SubnetType.PRIVATE_ISOLATED },
      securityGroups: [dbSg],
      databaseName: 'querymind',
      credentials: rds.Credentials.fromPassword('querymind', dbPasswordSecret.secretValue),
      storageEncrypted: true,
      multiAz: false,
      allocatedStorage: 20,
      maxAllocatedStorage: 100,
      deletionProtection: false,   // Set true before production go-live
      removalPolicy: cdk.RemovalPolicy.SNAPSHOT,
      backupRetention: cdk.Duration.days(7),
      enablePerformanceInsights: false
    });

    // ── ECR Repository ───────────────────────────────────────────────────────
    const ecrRepo = new ecr.Repository(this, 'ApiRepo', {
      repositoryName: 'querymind-api',
      removalPolicy: cdk.RemovalPolicy.RETAIN,
      lifecycleRules: [
        { maxImageCount: 10, description: 'Keep last 10 images' }
      ]
    });

    // ── S3 — Schema file storage ─────────────────────────────────────────────
    const schemaBucket = new s3.Bucket(this, 'SchemaBucket', {
      encryption: s3.BucketEncryption.S3_MANAGED,
      blockPublicAccess: s3.BlockPublicAccess.BLOCK_ALL,
      versioned: false,
      removalPolicy: cdk.RemovalPolicy.RETAIN,
      lifecycleRules: [
        { expiration: cdk.Duration.days(365), prefix: 'tenants/' }
      ]
    });

    // ── IAM — App Runner instance role ───────────────────────────────────────
    const appRunnerRole = new iam.Role(this, 'AppRunnerRole', {
      assumedBy: new iam.ServicePrincipal('tasks.apprunner.amazonaws.com'),
      description: 'QueryMind App Runner instance role'
    });

    schemaBucket.grantReadWrite(appRunnerRole);
    anthropicSecret.grantRead(appRunnerRole);
    dbPasswordSecret.grantRead(appRunnerRole);

    // ── IAM — App Runner ECR access role ────────────────────────────────────
    const ecrAccessRole = new iam.Role(this, 'EcrAccessRole', {
      assumedBy: new iam.ServicePrincipal('build.apprunner.amazonaws.com')
    });
    ecrRepo.grantPull(ecrAccessRole);

    // ── App Runner VPC Connector ─────────────────────────────────────────────
    const vpcConnector = new apprunner.CfnVpcConnector(this, 'VpcConnector', {
      subnets: vpc.selectSubnets({ subnetType: ec2.SubnetType.PRIVATE_ISOLATED }).subnetIds,
      securityGroups: [appRunnerSg.securityGroupId],
      vpcConnectorName: 'querymind-connector'
    });

    // ── Connection string assembled from DB endpoint + secret ───────────────
    const pgConnectionString = `Host=${dbInstance.instanceEndpoint.hostname};Database=querymind;Username=querymind;Password=${process.env.DB_PASSWORD ?? 'REPLACE_ME'};SslMode=Require;Trust Server Certificate=true`;

    // ── App Runner Service ───────────────────────────────────────────────────
    const appRunnerService = new apprunner.CfnService(this, 'ApiService', {
      serviceName: 'querymind-api',
      sourceConfiguration: {
        authenticationConfiguration: {
          accessRoleArn: ecrAccessRole.roleArn
        },
        autoDeploymentsEnabled: false,
        imageRepository: {
          imageIdentifier: `${ecrRepo.repositoryUri}:${imageTag}`,
          imageRepositoryType: 'ECR',
          imageConfiguration: {
            port: '8080',
            runtimeEnvironmentVariables: [
              { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' },
              { name: 'ASPNETCORE_URLS', value: 'http://+:8080' },
              { name: 'AWS__Region', value: this.region },
              { name: 'AWS__SchemaBucket', value: schemaBucket.bucketName },
              { name: 'Cors__Origins__0', value: '' }  // Filled in after CloudFront URL known
            ],
            runtimeEnvironmentSecrets: [
              {
                name: 'Anthropic__ApiKey',
                value: anthropicSecret.secretArn
              }
            ]
          }
        }
      },
      instanceConfiguration: {
        instanceRoleArn: appRunnerRole.roleArn,
        cpu: '1 vCPU',
        memory: '2 GB'
      },
      networkConfiguration: {
        egressConfiguration: {
          // DEFAULT keeps App Runner internet access (for Anthropic API calls)
          // VPC connector provides access to RDS in private subnets
          egressType: 'VPC',
          vpcConnectorArn: vpcConnector.attrVpcConnectorArn
        },
        ingressConfiguration: { isPubliclyAccessible: true }
      },
      healthCheckConfiguration: {
        protocol: 'HTTP',
        path: '/api/health',
        interval: 20,
        timeout: 5,
        healthyThreshold: 1,
        unhealthyThreshold: 5
      },
      autoScalingConfigurationArn: undefined  // Uses App Runner default
    });

    // Make App Runner wait for RDS to be ready
    appRunnerService.addDependency(dbInstance.node.defaultChild as cdk.CfnResource);

    // ── S3 — Frontend static hosting ────────────────────────────────────────
    const frontendBucket = new s3.Bucket(this, 'FrontendBucket', {
      encryption: s3.BucketEncryption.S3_MANAGED,
      blockPublicAccess: s3.BlockPublicAccess.BLOCK_ALL,
      removalPolicy: cdk.RemovalPolicy.DESTROY,
      autoDeleteObjects: true
    });

    // ── CloudFront — Frontend CDN ────────────────────────────────────────────
    const oac = new cloudfront.S3OriginAccessControl(this, 'OAC', {
      description: 'QueryMind frontend OAC'
    });

    const distribution = new cloudfront.Distribution(this, 'FrontendCdn', {
      defaultBehavior: {
        origin: origins.S3BucketOrigin.withOriginAccessControl(frontendBucket, { originAccessControl: oac }),
        viewerProtocolPolicy: cloudfront.ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
        cachePolicy: cloudfront.CachePolicy.CACHING_OPTIMIZED,
        allowedMethods: cloudfront.AllowedMethods.ALLOW_GET_HEAD_OPTIONS
      },
      additionalBehaviors: {
        '/api/*': {
          origin: new origins.HttpOrigin(`${appRunnerService.attrServiceUrl}`, {
            protocolPolicy: cloudfront.OriginProtocolPolicy.HTTPS_ONLY
          }),
          viewerProtocolPolicy: cloudfront.ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
          cachePolicy: cloudfront.CachePolicy.CACHING_DISABLED,
          allowedMethods: cloudfront.AllowedMethods.ALLOW_ALL,
          originRequestPolicy: cloudfront.OriginRequestPolicy.ALL_VIEWER_EXCEPT_HOST_HEADER
        }
      },
      defaultRootObject: 'index.html',
      errorResponses: [
        // SPA routing — all 403/404 from S3 → return index.html with 200
        { httpStatus: 403, responseHttpStatus: 200, responsePagePath: '/index.html' },
        { httpStatus: 404, responseHttpStatus: 200, responsePagePath: '/index.html' }
      ],
      minimumProtocolVersion: cloudfront.SecurityPolicyProtocol.TLS_V1_2_2021,
      comment: 'QueryMind frontend'
    });

    // ── SSM Parameters (read back in GitHub Actions) ─────────────────────────
    new ssm.StringParameter(this, 'FrontendBucketParam', {
      parameterName: '/querymind/frontend-bucket',
      stringValue: frontendBucket.bucketName
    });

    new ssm.StringParameter(this, 'CfDistributionParam', {
      parameterName: '/querymind/cloudfront-distribution-id',
      stringValue: distribution.distributionId
    });

    new ssm.StringParameter(this, 'AppRunnerArnParam', {
      parameterName: '/querymind/apprunner-service-arn',
      stringValue: appRunnerService.attrServiceArn
    });

    new ssm.StringParameter(this, 'ApiUrlParam', {
      parameterName: '/querymind/api-url',
      stringValue: appRunnerService.attrServiceUrl
    });

    // ── CDK Stack Outputs (for cdk deploy --outputs-file) ────────────────────
    new cdk.CfnOutput(this, 'FrontendUrl', {
      value: `https://${distribution.distributionDomainName}`,
      description: 'QueryMind frontend URL'
    });

    new cdk.CfnOutput(this, 'ApiUrl', {
      value: `https://${appRunnerService.attrServiceUrl}`,
      description: 'App Runner API URL'
    });

    new cdk.CfnOutput(this, 'FrontendBucketName', {
      value: frontendBucket.bucketName,
      description: 'S3 bucket for frontend static files'
    });

    new cdk.CfnOutput(this, 'CloudFrontDistributionId', {
      value: distribution.distributionId,
      description: 'CloudFront distribution ID for cache invalidation'
    });

    new cdk.CfnOutput(this, 'AppRunnerServiceArn', {
      value: appRunnerService.attrServiceArn,
      description: 'App Runner service ARN for deployment trigger'
    });

    new cdk.CfnOutput(this, 'SchemaBucketName', {
      value: schemaBucket.bucketName,
      description: 'S3 bucket for schema files'
    });

    new cdk.CfnOutput(this, 'EcrRepoUri', {
      value: ecrRepo.repositoryUri,
      description: 'ECR repository URI'
    });

    new cdk.CfnOutput(this, 'DbEndpoint', {
      value: dbInstance.instanceEndpoint.hostname,
      description: 'RDS PostgreSQL endpoint (private — only accessible from App Runner)'
    });
  }
}
