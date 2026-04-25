import { useState } from 'react';
import { Eye, EyeOff, Save } from 'lucide-react';
import { Card, CardContent, CardHeader } from '../components/ui/Card';
import { Button } from '../components/ui/Button';

export function Settings() {
  const [apiKey, setApiKey] = useState(() => localStorage.getItem('qm_api_key') ?? '');
  const [showKey, setShowKey] = useState(false);
  const [saved, setSaved] = useState(false);

  const save = () => {
    localStorage.setItem('qm_api_key', apiKey);
    setSaved(true);
    setTimeout(() => setSaved(false), 2000);
  };

  return (
    <div className="p-8 space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Settings</h1>
        <p className="text-gray-500 mt-1">Configure your QueryMind AI preferences.</p>
      </div>

      <Card>
        <CardHeader>
          <h2 className="font-semibold text-gray-900">API Authentication</h2>
          <p className="text-sm text-gray-500">Set your QueryMind API key to authenticate requests.</p>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">API Key</label>
              <div className="flex gap-2">
                <div className="relative flex-1">
                  <input
                    type={showKey ? 'text' : 'password'}
                    value={apiKey}
                    onChange={e => setApiKey(e.target.value)}
                    placeholder="qm_sk_..."
                    className="w-full rounded-lg border border-gray-300 px-4 py-2.5 pr-10 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                  <button
                    type="button"
                    onClick={() => setShowKey(!showKey)}
                    className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600"
                  >
                    {showKey ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                  </button>
                </div>
                <Button onClick={save} className="gap-2">
                  {saved ? '✓ Saved' : <><Save className="h-4 w-4" /> Save</>}
                </Button>
              </div>
              <p className="mt-1 text-xs text-gray-400">
                Stored locally in your browser. Alternatively, use Azure Entra ID SSO for enterprise auth.
              </p>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <h2 className="font-semibold text-gray-900">About QueryMind AI</h2>
        </CardHeader>
        <CardContent>
          <dl className="space-y-2 text-sm">
            <div className="flex justify-between"><dt className="text-gray-500">Version</dt><dd className="font-medium">1.0.0</dd></div>
            <div className="flex justify-between"><dt className="text-gray-500">AI Model</dt><dd className="font-medium">Claude claude-sonnet-4-20250514</dd></div>
            <div className="flex justify-between"><dt className="text-gray-500">Supported Dialects</dt><dd className="font-medium">DAX, T-SQL, PostgreSQL, Spark SQL, SOQL, BigQuery, Snowflake, DuckDB</dd></div>
            <div className="flex justify-between"><dt className="text-gray-500">Schema Formats</dt><dd className="font-medium">PBIX JSON, SQL DDL, CSV, BIM, Fabric Lakehouse, Salesforce</dd></div>
          </dl>
        </CardContent>
      </Card>
    </div>
  );
}
