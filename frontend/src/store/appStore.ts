import { create } from 'zustand';
import type { MessageDto, SchemaDto, SessionDto } from '../types';

interface AppState {
  schemas: SchemaDto[];
  sessions: SessionDto[];
  activeSessionId: string | null;
  activeSchemaId: string | null;
  messages: Record<string, MessageDto[]>;
  isStreaming: boolean;
  streamingContent: string;

  setSchemas: (schemas: SchemaDto[]) => void;
  addSchema: (schema: SchemaDto) => void;
  removeSchema: (id: string) => void;
  setSessions: (sessions: SessionDto[]) => void;
  addSession: (session: SessionDto) => void;
  setActiveSession: (id: string | null) => void;
  setActiveSchema: (id: string | null) => void;
  setMessages: (sessionId: string, messages: MessageDto[]) => void;
  addMessage: (sessionId: string, message: MessageDto) => void;
  updateLastMessage: (sessionId: string, patch: Partial<MessageDto>) => void;
  setStreaming: (v: boolean) => void;
  setStreamingContent: (v: string) => void;
  appendStreamingContent: (delta: string) => void;
}

export const useAppStore = create<AppState>((set) => ({
  schemas: [],
  sessions: [],
  activeSessionId: null,
  activeSchemaId: null,
  messages: {},
  isStreaming: false,
  streamingContent: '',

  setSchemas: (schemas) => set({ schemas }),
  addSchema: (schema) => set((s) => ({ schemas: [schema, ...s.schemas] })),
  removeSchema: (id) => set((s) => ({ schemas: s.schemas.filter((sc) => sc.id !== id) })),

  setSessions: (sessions) => set({ sessions }),
  addSession: (session) => set((s) => ({ sessions: [session, ...s.sessions] })),
  setActiveSession: (id) => set({ activeSessionId: id, streamingContent: '' }),
  setActiveSchema: (id) => set({ activeSchemaId: id }),

  setMessages: (sessionId, messages) =>
    set((s) => ({ messages: { ...s.messages, [sessionId]: messages } })),

  addMessage: (sessionId, message) =>
    set((s) => ({
      messages: {
        ...s.messages,
        [sessionId]: [...(s.messages[sessionId] ?? []), message]
      }
    })),

  updateLastMessage: (sessionId, patch) =>
    set((s) => {
      const msgs = s.messages[sessionId] ?? [];
      if (!msgs.length) return s;
      const updated = [...msgs.slice(0, -1), { ...msgs[msgs.length - 1], ...patch }];
      return { messages: { ...s.messages, [sessionId]: updated } };
    }),

  setStreaming: (v) => set({ isStreaming: v }),
  setStreamingContent: (v) => set({ streamingContent: v }),
  appendStreamingContent: (delta) =>
    set((s) => ({ streamingContent: s.streamingContent + delta }))
}));
