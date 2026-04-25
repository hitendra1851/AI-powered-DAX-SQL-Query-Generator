export type SchemaType = 'PbixJson' | 'SqlDdl' | 'CsvHeaders' | 'TabularBim' | 'FabricLakehouse' | 'Soql';
export type QueryDialect = 'Dax' | 'TSql' | 'PostgreSql' | 'MySql' | 'SparkSql' | 'Soql' | 'BigQuery' | 'Snowflake' | 'DuckDb';
export type PlanType = 'Starter' | 'Pro' | 'Enterprise';
export type MessageRole = 'User' | 'Assistant' | 'System';

export interface SchemaDto {
  id: string;
  name: string;
  type: SchemaType;
  fileSizeBytes: number;
  isProcessed: boolean;
  description: string | null;
  processingError: string | null;
  createdAt: string;
}

export interface SessionDto {
  id: string;
  schemaId: string | null;
  schemaName: string | null;
  defaultDialect: QueryDialect;
  title: string | null;
  messageCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface FeedbackDto {
  id: string;
  rating: number;
  comment: string | null;
}

export interface MessageDto {
  id: string;
  role: MessageRole;
  content: string;
  generatedQuery: string | null;
  dialect: QueryDialect | null;
  explanation: string | null;
  schemaContextUsed: string | null;
  inputTokens: number;
  outputTokens: number;
  latencyMs: number;
  feedback: FeedbackDto | null;
  createdAt: string;
}

export interface TenantUsageDto {
  tenantId: string;
  tenantName: string;
  plan: PlanType;
  monthlyQueryCount: number;
  queryLimit: number;
  totalSessions: number;
  totalSchemas: number;
  totalInputTokens: number;
  totalOutputTokens: number;
  queryCountResetAt: string;
}

export interface TemplateDto {
  id: string;
  title: string;
  description: string;
  category: string;
  dialect: QueryDialect;
  queryText: string;
  naturalLanguagePrompt: string;
  tags: string[];
  usageCount: number;
}

export interface StreamEvent {
  type: 'delta' | 'complete' | 'error';
  text?: string;
  messageId?: string;
  generatedQuery?: string;
  explanation?: string;
  inputTokens?: number;
  outputTokens?: number;
  latencyMs?: number;
  message?: string;
}
