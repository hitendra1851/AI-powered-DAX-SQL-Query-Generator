import { useEffect, useRef, useState } from 'react';
import { Upload, Trash2, FileJson, FileText, Table, RefreshCw } from 'lucide-react';
import { Card, CardContent, CardHeader } from '../components/ui/Card';
import { Button } from '../components/ui/Button';
import { Badge } from '../components/ui/Badge';
import { api } from '../services/api';
import { useAppStore } from '../store/appStore';
import type { SchemaDto, SchemaType } from '../types';

const schemaTypeIcons: Record<SchemaType, React.ReactNode> = {
  PbixJson: <FileJson className="h-4 w-4 text-yellow-600" />,
  SqlDdl: <FileText className="h-4 w-4 text-blue-600" />,
  CsvHeaders: <Table className="h-4 w-4 text-green-600" />,
  TabularBim: <FileJson className="h-4 w-4 text-purple-600" />,
  FabricLakehouse: <FileJson className="h-4 w-4 text-orange-600" />,
  Soql: <FileJson className="h-4 w-4 text-teal-600" />
};

export function SchemaManager() {
  const schemas = useAppStore(s => s.schemas);
  const setSchemas = useAppStore(s => s.setSchemas);
  const addSchema = useAppStore(s => s.addSchema);
  const removeSchema = useAppStore(s => s.removeSchema);

  const [uploading, setUploading] = useState(false);
  const [dragOver, setDragOver] = useState(false);
  const [selected, setSelected] = useState<SchemaDto | null>(null);
  const fileRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    api.schemas.list().then(setSchemas).catch(console.error);
  }, [setSchemas]);

  const handleFile = async (file: File) => {
    setUploading(true);
    try {
      const schema = await api.schemas.upload(file, file.name.replace(/\.[^.]+$/, ''));
      addSchema(schema);
    } catch (err) {
      alert('Upload failed: ' + (err instanceof Error ? err.message : 'Unknown error'));
    } finally {
      setUploading(false);
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setDragOver(false);
    const file = e.dataTransfer.files[0];
    if (file) handleFile(file);
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Delete this schema? This cannot be undone.')) return;
    await api.schemas.delete(id);
    removeSchema(id);
    if (selected?.id === id) setSelected(null);
  };

  return (
    <div className="p-8 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Schema Manager</h1>
          <p className="text-gray-500 mt-1">Upload and manage your database schemas.</p>
        </div>
        <Button onClick={() => fileRef.current?.click()} disabled={uploading} className="gap-2">
          {uploading ? <RefreshCw className="h-4 w-4 animate-spin" /> : <Upload className="h-4 w-4" />}
          {uploading ? 'Uploading...' : 'Upload Schema'}
        </Button>
        <input ref={fileRef} type="file" hidden accept=".json,.sql,.ddl,.csv,.bim" onChange={e => e.target.files?.[0] && handleFile(e.target.files[0])} />
      </div>

      <div
        onDrop={handleDrop}
        onDragOver={e => { e.preventDefault(); setDragOver(true); }}
        onDragLeave={() => setDragOver(false)}
        className={`border-2 border-dashed rounded-xl p-8 text-center transition-colors ${dragOver ? 'border-blue-500 bg-blue-50' : 'border-gray-300 hover:border-gray-400'}`}
      >
        <Upload className="h-8 w-8 mx-auto mb-3 text-gray-400" />
        <p className="text-gray-600">Drop your schema file here or <button onClick={() => fileRef.current?.click()} className="text-blue-600 underline">browse</button></p>
        <p className="text-sm text-gray-400 mt-1">Supports: .json (PBIX, BIM, Fabric), .sql, .ddl, .csv</p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-1 space-y-2">
          <h2 className="text-sm font-semibold text-gray-500 uppercase tracking-wide">{schemas.length} Schemas</h2>
          {schemas.length === 0 ? (
            <Card><CardContent className="py-8 text-center text-gray-500">No schemas uploaded yet</CardContent></Card>
          ) : (
            schemas.map(s => (
              <div
                key={s.id}
                onClick={() => setSelected(s)}
                className={`flex items-center gap-3 p-3 rounded-lg border cursor-pointer transition-colors ${selected?.id === s.id ? 'border-blue-500 bg-blue-50' : 'border-gray-200 bg-white hover:border-gray-300'}`}
              >
                {schemaTypeIcons[s.type]}
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-gray-900 truncate">{s.name}</p>
                  <p className="text-xs text-gray-500">{s.type}</p>
                </div>
                <Badge variant={s.isProcessed ? 'success' : s.processingError ? 'error' : 'warning'}>
                  {s.isProcessed ? 'Ready' : s.processingError ? 'Error' : 'Processing'}
                </Badge>
              </div>
            ))
          )}
        </div>

        <div className="lg:col-span-2">
          {selected ? (
            <Card>
              <CardHeader>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    {schemaTypeIcons[selected.type]}
                    <h2 className="font-semibold text-gray-900">{selected.name}</h2>
                  </div>
                  <Button variant="danger" size="sm" onClick={() => handleDelete(selected.id)} className="gap-1.5">
                    <Trash2 className="h-3.5 w-3.5" /> Delete
                  </Button>
                </div>
              </CardHeader>
              <CardContent>
                <dl className="space-y-3 text-sm">
                  <div className="flex justify-between"><dt className="text-gray-500">Type</dt><dd className="font-medium">{selected.type}</dd></div>
                  <div className="flex justify-between"><dt className="text-gray-500">Size</dt><dd className="font-medium">{(selected.fileSizeBytes / 1024).toFixed(1)} KB</dd></div>
                  <div className="flex justify-between"><dt className="text-gray-500">Status</dt>
                    <dd><Badge variant={selected.isProcessed ? 'success' : selected.processingError ? 'error' : 'warning'}>{selected.isProcessed ? 'Processed' : selected.processingError ? 'Error' : 'Processing'}</Badge></dd>
                  </div>
                  <div className="flex justify-between"><dt className="text-gray-500">Uploaded</dt><dd className="font-medium">{new Date(selected.createdAt).toLocaleDateString()}</dd></div>
                  {selected.description && <div className="flex justify-between"><dt className="text-gray-500">Description</dt><dd className="font-medium">{selected.description}</dd></div>}
                  {selected.processingError && (
                    <div className="mt-2 p-3 bg-red-50 rounded-lg text-red-700 text-xs">
                      <strong>Error:</strong> {selected.processingError}
                    </div>
                  )}
                </dl>
              </CardContent>
            </Card>
          ) : (
            <div className="flex items-center justify-center h-64 text-gray-400">
              Select a schema to view details
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
