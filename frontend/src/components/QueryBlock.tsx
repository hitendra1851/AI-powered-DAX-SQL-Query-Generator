import { useState, useEffect } from 'react';
import Prism from 'prismjs';
import 'prismjs/components/prism-sql';
import { Copy, Check, ThumbsUp, ThumbsDown, ExternalLink } from 'lucide-react';
import { Button } from './ui/Button';
import type { MessageDto } from '../types';
import { api } from '../services/api';

interface QueryBlockProps {
  message: MessageDto;
}

export function QueryBlock({ message }: QueryBlockProps) {
  const [copied, setCopied] = useState(false);
  const [feedbackGiven, setFeedbackGiven] = useState<'up' | 'down' | null>(
    message.feedback ? (message.feedback.rating >= 4 ? 'up' : 'down') : null
  );

  useEffect(() => {
    Prism.highlightAll();
  }, [message.generatedQuery]);

  const copy = async () => {
    if (!message.generatedQuery) return;
    await navigator.clipboard.writeText(message.generatedQuery);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const giveFeedback = async (rating: number) => {
    await api.feedback.submit(message.id, rating);
    setFeedbackGiven(rating >= 4 ? 'up' : 'down');
  };

  const openInPowerBi = () => {
    if (!message.generatedQuery) return;
    const encoded = encodeURIComponent(message.generatedQuery);
    window.open(`https://app.powerbi.com/groups/me/reports?q=${encoded}`, '_blank');
  };

  const lang = message.dialect === 'Dax' ? 'sql' : 'sql';

  return (
    <div className="space-y-3">
      <p className="text-gray-700 whitespace-pre-wrap">{message.content}</p>

      {message.generatedQuery && (
        <div className="rounded-lg border border-gray-200 overflow-hidden">
          <div className="flex items-center justify-between px-4 py-2 bg-gray-50 border-b border-gray-200">
            <span className="text-xs font-mono text-gray-500">
              {message.dialect ?? 'DAX'}
            </span>
            <div className="flex items-center gap-2">
              <Button size="sm" variant="ghost" onClick={copy} className="gap-1.5">
                {copied ? <Check className="h-3.5 w-3.5 text-green-600" /> : <Copy className="h-3.5 w-3.5" />}
                {copied ? 'Copied' : 'Copy'}
              </Button>
              {message.dialect === 'Dax' && (
                <Button size="sm" variant="ghost" onClick={openInPowerBi} className="gap-1.5">
                  <ExternalLink className="h-3.5 w-3.5" />
                  Open in Power BI
                </Button>
              )}
            </div>
          </div>
          <pre className="overflow-x-auto p-4 bg-gray-900 text-sm">
            <code className={`language-${lang} text-gray-100`}>
              {message.generatedQuery}
            </code>
          </pre>
        </div>
      )}

      {message.explanation && (
        <p className="text-sm text-gray-600 italic border-l-2 border-blue-200 pl-3">
          {message.explanation}
        </p>
      )}

      {message.schemaContextUsed && (
        <details className="text-xs text-gray-500">
          <summary className="cursor-pointer hover:text-gray-700">Schema context used</summary>
          <pre className="mt-1 bg-gray-50 p-2 rounded text-xs overflow-auto max-h-32">
            {message.schemaContextUsed}
          </pre>
        </details>
      )}

      <div className="flex items-center justify-between text-xs text-gray-400">
        <span>
          {message.inputTokens + message.outputTokens} tokens • {message.latencyMs}ms
        </span>
        <div className="flex items-center gap-1">
          <button
            onClick={() => giveFeedback(5)}
            className={`p-1 rounded hover:bg-green-50 ${feedbackGiven === 'up' ? 'text-green-600' : 'text-gray-400'}`}
          >
            <ThumbsUp className="h-3.5 w-3.5" />
          </button>
          <button
            onClick={() => giveFeedback(1)}
            className={`p-1 rounded hover:bg-red-50 ${feedbackGiven === 'down' ? 'text-red-600' : 'text-gray-400'}`}
          >
            <ThumbsDown className="h-3.5 w-3.5" />
          </button>
        </div>
      </div>
    </div>
  );
}
