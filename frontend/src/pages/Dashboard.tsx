import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Database, MessageSquare, Zap, TrendingUp } from 'lucide-react';
import { Card, CardContent, CardHeader } from '../components/ui/Card';
import { Badge } from '../components/ui/Badge';
import { Button } from '../components/ui/Button';
import { api } from '../services/api';
import type { TenantUsageDto } from '../types';
import { useAppStore } from '../store/appStore';

export function Dashboard() {
  const [usage, setUsage] = useState<TenantUsageDto | null>(null);
  const schemas = useAppStore(s => s.schemas);
  const setSchemas = useAppStore(s => s.setSchemas);
  const navigate = useNavigate();

  useEffect(() => {
    api.admin.usage().then(setUsage).catch(console.error);
    api.schemas.list().then(setSchemas).catch(console.error);
  }, [setSchemas]);

  const queryPct = usage ? Math.round((usage.monthlyQueryCount / usage.queryLimit) * 100) : 0;

  return (
    <div className="p-8 space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
        <p className="text-gray-500 mt-1">Welcome to QueryMind AI — your intelligent DAX & SQL generator.</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCard
          icon={<MessageSquare className="h-5 w-5 text-blue-600" />}
          label="Queries This Month"
          value={usage?.monthlyQueryCount ?? 0}
          sub={`of ${usage?.queryLimit === 2147483647 ? '∞' : usage?.queryLimit} limit`}
        />
        <StatCard
          icon={<Database className="h-5 w-5 text-purple-600" />}
          label="Schemas Loaded"
          value={usage?.totalSchemas ?? 0}
          sub="ready to query"
        />
        <StatCard
          icon={<Zap className="h-5 w-5 text-yellow-600" />}
          label="Total Sessions"
          value={usage?.totalSessions ?? 0}
          sub="conversation sessions"
        />
        <StatCard
          icon={<TrendingUp className="h-5 w-5 text-green-600" />}
          label="Tokens Used"
          value={((usage?.totalInputTokens ?? 0) + (usage?.totalOutputTokens ?? 0)).toLocaleString()}
          sub="input + output"
        />
      </div>

      {usage && (
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <h2 className="font-semibold text-gray-900">Monthly Usage</h2>
              <Badge variant={usage.plan === 'Enterprise' ? 'success' : usage.plan === 'Pro' ? 'info' : 'default'}>
                {usage.plan}
              </Badge>
            </div>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              <div className="flex justify-between text-sm text-gray-600">
                <span>{usage.monthlyQueryCount} queries used</span>
                <span>{usage.queryLimit === 2147483647 ? 'Unlimited' : `${usage.queryLimit} total`}</span>
              </div>
              <div className="w-full bg-gray-200 rounded-full h-2">
                <div
                  className={`h-2 rounded-full ${queryPct > 80 ? 'bg-red-500' : queryPct > 60 ? 'bg-yellow-500' : 'bg-blue-600'}`}
                  style={{ width: `${Math.min(queryPct, 100)}%` }}
                />
              </div>
              <p className="text-xs text-gray-400">
                Resets {new Date(usage.queryCountResetAt).toLocaleDateString()}
              </p>
            </div>
          </CardContent>
        </Card>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <h2 className="font-semibold text-gray-900">Recent Schemas</h2>
          </CardHeader>
          <CardContent>
            {schemas.length === 0 ? (
              <div className="text-center py-6 text-gray-500">
                <Database className="h-10 w-10 mx-auto mb-2 text-gray-300" />
                <p>No schemas yet</p>
                <Button size="sm" className="mt-3" onClick={() => navigate('/schemas')}>
                  Upload Schema
                </Button>
              </div>
            ) : (
              <ul className="space-y-2">
                {schemas.slice(0, 5).map(s => (
                  <li key={s.id} className="flex items-center justify-between py-2 border-b border-gray-100 last:border-0">
                    <div>
                      <p className="text-sm font-medium text-gray-900">{s.name}</p>
                      <p className="text-xs text-gray-500">{s.type}</p>
                    </div>
                    <Badge variant={s.isProcessed ? 'success' : 'warning'}>
                      {s.isProcessed ? 'Ready' : 'Processing'}
                    </Badge>
                  </li>
                ))}
              </ul>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <h2 className="font-semibold text-gray-900">Quick Start</h2>
          </CardHeader>
          <CardContent>
            <ol className="space-y-3 text-sm text-gray-700">
              {[
                { n: '1', text: 'Upload a schema file (PBIX JSON, SQL DDL, CSV, or Fabric JSON)', to: '/schemas' },
                { n: '2', text: 'Open Query Studio and select your schema', to: '/studio' },
                { n: '3', text: 'Ask in plain English — get DAX or SQL instantly', to: '/studio' }
              ].map(step => (
                <li key={step.n} className="flex items-start gap-3">
                  <span className="flex-shrink-0 w-6 h-6 bg-blue-100 text-blue-700 rounded-full flex items-center justify-center text-xs font-bold">
                    {step.n}
                  </span>
                  <button onClick={() => navigate(step.to)} className="text-left hover:text-blue-600 transition-colors">
                    {step.text}
                  </button>
                </li>
              ))}
            </ol>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

function StatCard({ icon, label, value, sub }: { icon: React.ReactNode; label: string; value: string | number; sub: string }) {
  return (
    <Card>
      <CardContent className="pt-6">
        <div className="flex items-center gap-3">
          <div className="p-2 bg-gray-50 rounded-lg">{icon}</div>
          <div>
            <p className="text-2xl font-bold text-gray-900">{value}</p>
            <p className="text-xs text-gray-500 mt-0.5">{sub}</p>
          </div>
        </div>
        <p className="text-sm font-medium text-gray-600 mt-3">{label}</p>
      </CardContent>
    </Card>
  );
}
