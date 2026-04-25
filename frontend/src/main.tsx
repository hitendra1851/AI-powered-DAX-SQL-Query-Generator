import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { Layout } from './components/Layout';
import { Dashboard } from './pages/Dashboard';
import { SchemaManager } from './pages/SchemaManager';
import { QueryStudio } from './pages/QueryStudio';
import { QueryHistory } from './pages/QueryHistory';
import { AdminPanel } from './pages/AdminPanel';
import { Settings } from './pages/Settings';
import './index.css';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Layout />}>
          <Route index element={<Dashboard />} />
          <Route path="schemas" element={<SchemaManager />} />
          <Route path="studio" element={<QueryStudio />} />
          <Route path="history" element={<QueryHistory />} />
          <Route path="admin" element={<AdminPanel />} />
          <Route path="settings" element={<Settings />} />
        </Route>
      </Routes>
    </BrowserRouter>
  </React.StrictMode>
);
