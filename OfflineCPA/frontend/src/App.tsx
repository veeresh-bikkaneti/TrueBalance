
import Dashboard from './components/Dashboard';
import { BookOpen, FolderSync, ShieldCheck } from 'lucide-react';
import './index.css';

function App() {
  return (
    <div className="app-container">
      {/* Sidebar Navigation */}
      <aside className="sidebar">
        <div className="brand">
          <BookOpen size={24} color="var(--primary)" />
          OfflineCPA
        </div>
        <nav className="nav-links">
          <a className="nav-link active">
            <FolderSync size={18} />
            1120-S Dashboard
          </a>
          <a className="nav-link">
            <ShieldCheck size={18} />
            Security Rules
          </a>
        </nav>
      </aside>

      {/* Main Content Area */}
      <Dashboard />
    </div>
  );
}

export default App;
