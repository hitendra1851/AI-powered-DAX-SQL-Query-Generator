import { useEffect, useState } from 'react';
import { Search, ChevronDown, ChevronUp } from 'lucide-react';
import { Card, CardContent } from '../components/ui/Card';
import { Badge } from '../components/ui/Badge';
import { api } from '../services/api';
import { useAppStore } from '../store/appStore';
import type { MessageDto } from '../types';

export function QueryHistory() {
  const sessions = useAppStore(s => s.sessions);
  const messages = useAppStore(s => s.messages);
  const setMessages = useAppStore(s => s.setMessages);
  const setSessions = useAppStore(s => s.setSessions);

  const [search, setSearch] = useState('');
  const [expanded, setExpanded] = useState<Set<string>>(new Set());

  useEffect(() => {
    // Load session history for display (simplified — real app would paginate)
  }, []);

  const toggle = async (sessionId: string) => {
    const next = new Set(expanded);
    if (next.has(sessionId)) {
      next.delete(sessionId);
    } else {
      next.add(sessionId);
      if (!messages[sessionId]) {
        const msgs = await api.sessions.history(sessionId);
        setMessages(sessionId, msgs);
      }
    }
    setExpanded(next);
  };

  const allMessages: MessageDto[] = Object.values(messages).flat();
  const filtered = allMessages.filter(m =>
    m.role === 'Assistant' && m.generatedQuery &&
    (search === '' || m.content.toLowerCase().includes(search.toLowerCase()) ||
      m.generatedQuery?.toLowerCase().includes(search.toLowerCase()))
  );

  return (
    <div className="p-8 space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Query History</h1>
        <p className="text-gray-500 mt-1">Browse and search all past generated queries.</p>
      </div>

      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
        <input
          value={search}
          onChange={e => setSearch(e.target.value)}
          placeholder="Search queries..."
          className="w-full pl-10 pr-4 py-2 rounded-xl border border-gray-200 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>

      {filtered.length === 0 ? (
        <div className="text-center py-16 text-gray-400">
          <p>No queries found{search ? ' matching your search' : ' yet'}.</p>
          <p className="text-sm mt-1">Use the Query Studio to generate your first query.</p>
        </div>
      ) : (
        <div className="space-y-3">
          {filtered.map(msg => (
            <Card key={msg.id}>
              <CardContent className="py-4">
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      {msg.dialect && <Badge variant="info">{msg.dialect}</Badge>}
                      <span className="text-xs text-gray-500">{new Date(msg.createdAt).toLocaleString()}</span>
                    </div>
                    <div className="flex items-center gap-3 text-xs text-gray-400">
                      <span>{msg.inputTokens + msg.outputTokens} tokens</span>
                      {msg.feedback && (
                        <Badge variant={msg.feedback.rating >= 4 ? 'success' : 'error'}>
                          {msg.feedback.rating >= 4 ? '👍' : '👎'}
                        </Badge>
                      )}
                    </div>
                  </div>

                  {msg.generatedQuery && (
                    <pre className="text-xs bg-gray-900 text-gray-100 p-3 rounded-lg overflow-x-auto max-h-40">
                      {msg.generatedQuery}
                    </pre>
                  )}

                  {msg.explanation && (
                    <p className="text-sm text-gray-600">{msg.explanation}</p>
                  )}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
