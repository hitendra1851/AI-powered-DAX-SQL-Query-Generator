import { NavLink, Outlet } from 'react-router-dom';
import { Brain, Database, MessageSquare, History, Settings, BarChart3, ChevronRight } from 'lucide-react';
import { clsx } from 'clsx';

const navItems = [
  { to: '/', icon: BarChart3, label: 'Dashboard', end: true },
  { to: '/schemas', icon: Database, label: 'Schemas' },
  { to: '/studio', icon: MessageSquare, label: 'Query Studio' },
  { to: '/history', icon: History, label: 'History' },
  { to: '/admin', icon: BarChart3, label: 'Admin' },
  { to: '/settings', icon: Settings, label: 'Settings' }
];

export function Layout() {
  return (
    <div className="flex h-screen bg-gray-50">
      <aside className="w-60 bg-gray-900 flex flex-col">
        <div className="flex items-center gap-2.5 px-5 py-4 border-b border-gray-700">
          <Brain className="h-6 w-6 text-blue-400" />
          <span className="font-semibold text-white text-lg">QueryMind</span>
          <span className="text-xs text-gray-400 ml-auto">AI</span>
        </div>

        <nav className="flex-1 px-3 py-4 space-y-1">
          {navItems.map(({ to, icon: Icon, label, end }) => (
            <NavLink
              key={to}
              to={to}
              end={end}
              className={({ isActive }) =>
                clsx(
                  'flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors',
                  isActive
                    ? 'bg-blue-600 text-white'
                    : 'text-gray-400 hover:text-white hover:bg-gray-800'
                )
              }
            >
              <Icon className="h-4 w-4" />
              {label}
            </NavLink>
          ))}
        </nav>

        <div className="px-4 py-3 border-t border-gray-700 text-xs text-gray-500">
          QueryMind AI v1.0.0
        </div>
      </aside>

      <main className="flex-1 overflow-auto">
        <Outlet />
      </main>
    </div>
  );
}
