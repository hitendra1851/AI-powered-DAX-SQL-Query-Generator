import type { MessageDto, SchemaDto, SessionDto, StreamEvent, TemplateDto, TenantUsageDto } from '../types';

const BASE = '/api';

function getHeaders(extra?: Record<string, string>): HeadersInit {
  const apiKey = localStorage.getItem('qm_api_key');
  return {
    'Content-Type': 'application/json',
    ...(apiKey ? { 'X-Api-Key': apiKey } : {}),
    ...extra
  };
}

async function get<T>(path: string): Promise<T> {
  const res = await fetch(`${BASE}${path}`, { headers: getHeaders() });
  if (!res.ok) throw new Error((await res.json()).error ?? res.statusText);
  return res.json();
}

async function del(path: string): Promise<void> {
  const res = await fetch(`${BASE}${path}`, { method: 'DELETE', headers: getHeaders() });
  if (!res.ok) throw new Error((await res.json()).error ?? res.statusText);
}

export const api = {
  schemas: {
    list: () => get<SchemaDto[]>('/schemas'),
    upload: async (file: File, description?: string): Promise<SchemaDto> => {
      const form = new FormData();
      form.append('file', file);
      if (description) form.append('description', description);
      const apiKey = localStorage.getItem('qm_api_key');
      const res = await fetch(`${BASE}/schemas/upload`, {
        method: 'POST',
        headers: apiKey ? { 'X-Api-Key': apiKey } : {},
        body: form
      });
      if (!res.ok) throw new Error((await res.json()).error ?? res.statusText);
      return res.json();
    },
    delete: (id: string) => del(`/schemas/${id}`)
  },

  sessions: {
    create: (body: { schemaId?: string; defaultDialect: string; title?: string }): Promise<SessionDto> =>
      fetch(`${BASE}/sessions`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify(body)
      }).then(r => r.json()),

    history: (id: string) => get<MessageDto[]>(`/sessions/${id}/history`),

    sendMessage: async (
      sessionId: string,
      message: string,
      dialect: string,
      onEvent: (e: StreamEvent) => void
    ): Promise<void> => {
      const apiKey = localStorage.getItem('qm_api_key');
      const res = await fetch(`${BASE}/sessions/${sessionId}/messages`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(apiKey ? { 'X-Api-Key': apiKey } : {})
        },
        body: JSON.stringify({ message, dialect })
      });

      if (!res.ok || !res.body) {
        const err = await res.json().catch(() => ({ error: res.statusText }));
        onEvent({ type: 'error', message: err.error });
        return;
      }

      const reader = res.body.getReader();
      const decoder = new TextDecoder();
      let buffer = '';

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n');
        buffer = lines.pop() ?? '';
        for (const line of lines) {
          if (!line.startsWith('data: ')) continue;
          try {
            const evt: StreamEvent = JSON.parse(line.slice(6));
            onEvent(evt);
          } catch { /* ignore parse errors */ }
        }
      }
    }
  },

  feedback: {
    submit: (messageId: string, rating: number, comment?: string) =>
      fetch(`${BASE}/feedback`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify({ messageId, rating, comment })
      }).then(r => r.json())
  },

  templates: {
    list: (dialect?: string) => get<TemplateDto[]>(`/templates${dialect ? `?dialect=${dialect}` : ''}`)
  },

  admin: {
    usage: () => get<TenantUsageDto>('/admin/usage')
  }
};
