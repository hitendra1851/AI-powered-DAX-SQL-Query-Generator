import { useEffect, useRef, useState } from 'react';
import { Send, Plus, Bot, User, Loader2 } from 'lucide-react';
import { Button } from '../components/ui/Button';
import { QueryBlock } from '../components/QueryBlock';
import { api } from '../services/api';
import { useAppStore } from '../store/appStore';
import type { MessageDto, QueryDialect } from '../types';

const dialects: QueryDialect[] = ['Dax', 'TSql', 'PostgreSql', 'SparkSql', 'Soql', 'BigQuery', 'Snowflake'];

export function QueryStudio() {
  const schemas = useAppStore(s => s.schemas);
  const activeSessionId = useAppStore(s => s.activeSessionId);
  const activeSchemaId = useAppStore(s => s.activeSchemaId);
  const messages = useAppStore(s => activeSessionId ? (s.messages[activeSessionId] ?? []) : []);
  const isStreaming = useAppStore(s => s.isStreaming);
  const streamingContent = useAppStore(s => s.streamingContent);
  const store = useAppStore();

  const [input, setInput] = useState('');
  const [dialect, setDialect] = useState<QueryDialect>('Dax');
  const [selectedSchema, setSelectedSchema] = useState<string>('');
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, streamingContent]);

  useEffect(() => {
    if (schemas.length > 0 && !selectedSchema) setSelectedSchema(schemas[0].id);
  }, [schemas, selectedSchema]);

  const createSession = async () => {
    const session = await api.sessions.create({
      schemaId: selectedSchema || undefined,
      defaultDialect: dialect
    });
    store.addSession(session);
    store.setActiveSession(session.id);
    store.setMessages(session.id, []);
  };

  const sendMessage = async () => {
    if (!input.trim() || !activeSessionId || isStreaming) return;
    const text = input.trim();
    setInput('');

    const userMsg: MessageDto = {
      id: crypto.randomUUID(),
      role: 'User',
      content: text,
      generatedQuery: null,
      dialect: null,
      explanation: null,
      schemaContextUsed: null,
      inputTokens: 0,
      outputTokens: 0,
      latencyMs: 0,
      feedback: null,
      createdAt: new Date().toISOString()
    };
    store.addMessage(activeSessionId, userMsg);
    store.setStreaming(true);
    store.setStreamingContent('');

    await api.sessions.sendMessage(activeSessionId, text, dialect, (evt) => {
      if (evt.type === 'delta' && evt.text) {
        store.appendStreamingContent(evt.text);
      } else if (evt.type === 'complete') {
        const assistantMsg: MessageDto = {
          id: evt.messageId ?? crypto.randomUUID(),
          role: 'Assistant',
          content: store.getState().streamingContent,
          generatedQuery: evt.generatedQuery ?? null,
          dialect,
          explanation: evt.explanation ?? null,
          schemaContextUsed: null,
          inputTokens: evt.inputTokens ?? 0,
          outputTokens: evt.outputTokens ?? 0,
          latencyMs: evt.latencyMs ?? 0,
          feedback: null,
          createdAt: new Date().toISOString()
        };
        store.addMessage(activeSessionId, assistantMsg);
        store.setStreaming(false);
        store.setStreamingContent('');
      } else if (evt.type === 'error') {
        store.setStreaming(false);
        store.setStreamingContent('');
      }
    });
  };

  return (
    <div className="flex h-full">
      {/* Session sidebar */}
      <div className="w-56 border-r border-gray-200 bg-white flex flex-col p-3 space-y-2">
        <div className="space-y-2">
          <select
            value={selectedSchema}
            onChange={e => setSelectedSchema(e.target.value)}
            className="w-full text-xs rounded-lg border border-gray-200 py-1.5 px-2"
          >
            <option value="">No schema</option>
            {schemas.filter(s => s.isProcessed).map(s => (
              <option key={s.id} value={s.id}>{s.name}</option>
            ))}
          </select>
          <select
            value={dialect}
            onChange={e => setDialect(e.target.value as QueryDialect)}
            className="w-full text-xs rounded-lg border border-gray-200 py-1.5 px-2"
          >
            {dialects.map(d => <option key={d} value={d}>{d}</option>)}
          </select>
          <Button size="sm" className="w-full gap-1.5" onClick={createSession}>
            <Plus className="h-3.5 w-3.5" /> New Session
          </Button>
        </div>
      </div>

      {/* Chat area */}
      <div className="flex-1 flex flex-col">
        {!activeSessionId ? (
          <div className="flex-1 flex items-center justify-center text-gray-400">
            <div className="text-center">
              <Bot className="h-12 w-12 mx-auto mb-3 text-gray-300" />
              <p className="font-medium">Start a new session</p>
              <p className="text-sm mt-1">Select a schema and click "New Session" to begin</p>
            </div>
          </div>
        ) : (
          <>
            <div className="flex-1 overflow-auto p-6 space-y-6">
              {messages.map((msg) => (
                <div key={msg.id} className={`flex gap-3 ${msg.role === 'User' ? 'flex-row-reverse' : ''}`}>
                  <div className={`flex-shrink-0 w-8 h-8 rounded-full flex items-center justify-center ${msg.role === 'User' ? 'bg-blue-600' : 'bg-gray-700'}`}>
                    {msg.role === 'User'
                      ? <User className="h-4 w-4 text-white" />
                      : <Bot className="h-4 w-4 text-white" />
                    }
                  </div>
                  <div className={`flex-1 max-w-3xl ${msg.role === 'User' ? 'text-right' : ''}`}>
                    {msg.role === 'User' ? (
                      <div className="inline-block bg-blue-600 text-white rounded-xl px-4 py-2.5 text-sm">
                        {msg.content}
                      </div>
                    ) : (
                      <div className="bg-white border border-gray-200 rounded-xl p-4">
                        <QueryBlock message={msg} />
                      </div>
                    )}
                  </div>
                </div>
              ))}

              {isStreaming && (
                <div className="flex gap-3">
                  <div className="flex-shrink-0 w-8 h-8 rounded-full bg-gray-700 flex items-center justify-center">
                    <Bot className="h-4 w-4 text-white" />
                  </div>
                  <div className="flex-1 max-w-3xl bg-white border border-gray-200 rounded-xl p-4">
                    {streamingContent ? (
                      <p className="text-gray-700 text-sm whitespace-pre-wrap">{streamingContent}</p>
                    ) : (
                      <div className="flex items-center gap-2 text-gray-400">
                        <Loader2 className="h-4 w-4 animate-spin" />
                        <span className="text-sm">Generating query...</span>
                      </div>
                    )}
                  </div>
                </div>
              )}
              <div ref={messagesEndRef} />
            </div>

            <div className="border-t border-gray-200 p-4">
              <div className="flex gap-3 max-w-4xl mx-auto">
                <textarea
                  value={input}
                  onChange={e => setInput(e.target.value)}
                  onKeyDown={e => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); sendMessage(); } }}
                  placeholder="Ask a question about your data... e.g. 'Show me total sales by region for this year'"
                  rows={2}
                  disabled={isStreaming}
                  className="flex-1 resize-none rounded-xl border border-gray-300 px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50"
                />
                <Button onClick={sendMessage} disabled={!input.trim() || isStreaming} className="self-end gap-2">
                  {isStreaming ? <Loader2 className="h-4 w-4 animate-spin" /> : <Send className="h-4 w-4" />}
                  Send
                </Button>
              </div>
            </div>
          </>
        )}
      </div>
    </div>
  );
}
