import { useEffect, useState } from 'react';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import { Card, CardContent, CardHeader } from '../components/ui/Card';
import { Badge } from '../components/ui/Badge';
import { api } from '../services/api';
import type { TenantUsageDto } from '../types';

export function AdminPanel() {
  const [usage, setUsage] = useState<TenantUsageDto | null>(null);

  useEffect(() => {
    api.admin.usage().then(setUsage).catch(console.error);
  }, []);

  if (!usage) return (
    <div className="p-8 text-gray-500">Loading usage data...</div>
  );

  const queryPct = Math.min(Math.round((usage.monthlyQueryCount / usage.queryLimit) * 100), 100);
  const isUnlimited = usage.queryLimit === 2147483647;

  const tokenData = [
    { name: 'Input', tokens: usage.totalInputTokens },
    { name: 'Output', tokens: usage.totalOutputTokens }
  ];

  return (
    <div className="p-8 space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Admin Panel</h1>
        <p className="text-gray-500 mt-1">Monitor usage and manage your tenant.</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <Card>
          <CardHeader><h3 className="font-semibold text-gray-900">Plan</h3></CardHeader>
          <CardContent>
            <Badge variant={usage.plan === 'Enterprise' ? 'success' : usage.plan === 'Pro' ? 'info' : 'default'} className="text-base px-4 py-1">
              {usage.plan}
            </Badge>
          </CardContent>
        </Card>

        <Card>
          <CardHeader><h3 className="font-semibold text-gray-900">Monthly Queries</h3></CardHeader>
          <CardContent>
            <div className="space-y-2">
              <div className="flex justify-between text-sm text-gray-600">
                <span className="font-bold text-xl text-gray-900">{usage.monthlyQueryCount.toLocaleString()}</span>
                <span className="text-gray-400">{isUnlimited ? '∞' : usage.queryLimit.toLocaleString()}</span>
              </div>
              {!isUnlimited && (
                <div className="w-full bg-gray-200 rounded-full h-2">
                  <div
                    className={`h-2 rounded-full ${queryPct > 80 ? 'bg-red-500' : queryPct > 60 ? 'bg-yellow-500' : 'bg-blue-600'}`}
                    style={{ width: `${queryPct}%` }}
                  />
                </div>
              )}
              <p className="text-xs text-gray-400">Resets {new Date(usage.queryCountResetAt).toLocaleDateString()}</p>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader><h3 className="font-semibold text-gray-900">Resources</h3></CardHeader>
          <CardContent>
            <dl className="space-y-2 text-sm">
              <div className="flex justify-between">
                <dt className="text-gray-500">Schemas</dt>
                <dd className="font-medium">{usage.totalSchemas}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-gray-500">Sessions</dt>
                <dd className="font-medium">{usage.totalSessions}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-gray-500">Total Tokens</dt>
                <dd className="font-medium">{(usage.totalInputTokens + usage.totalOutputTokens).toLocaleString()}</dd>
              </div>
            </dl>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader><h3 className="font-semibold text-gray-900">Token Usage Breakdown</h3></CardHeader>
        <CardContent>
          <ResponsiveContainer width="100%" height={200}>
            <BarChart data={tokenData}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="name" />
              <YAxis />
              <Tooltip formatter={(v: number) => v.toLocaleString()} />
              <Bar dataKey="tokens" fill="#3b82f6" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </CardContent>
      </Card>
    </div>
  );
}
