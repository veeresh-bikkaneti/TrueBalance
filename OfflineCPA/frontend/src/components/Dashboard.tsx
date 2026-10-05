import React, { useEffect, useState } from 'react';
import axios from 'axios';
import { format } from 'date-fns';
import { Download, FileText, Activity, AlertCircle, RefreshCw, Upload } from 'lucide-react';

interface Transaction {
  id: string;
  date: string;
  description: string;
  referenceDocumentPath: string | null;
}

const API_BASE = 'http://localhost:5030/api';

const Dashboard: React.FC = () => {
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const fetchTransactions = async () => {
    try {
      setLoading(true);
      setError(null);
      const res = await axios.get<Transaction[]>(`${API_BASE}/transactions`);
      setTransactions(res.data);
    } catch (err) {
      setError('Failed to load transactions. Ensure the backend is running.');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchTransactions();
  }, []);

  const handleExport = async () => {
    try {
      const res = await axios.get(`${API_BASE}/export`, {
        responseType: 'blob',
      });
      const url = window.URL.createObjectURL(new Blob([res.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', `OfflineCPA_1120S_Export_${format(new Date(), 'yyyyMMdd')}.xlsx`);
      document.body.appendChild(link);
      link.click();
      link.parentNode?.removeChild(link);
    } catch (err) {
      console.error('Export failed', err);
      alert('Failed to export. Please try again.');
    }
  };

  const handleUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    if (!e.target.files || e.target.files.length === 0) return;
    const file = e.target.files[0];
    const formData = new FormData();
    formData.append('file', file);

    try {
      setLoading(true);
      await axios.post(`${API_BASE}/upload`, formData, {
        headers: {
          'Content-Type': 'multipart/form-data'
        }
      });
      fetchTransactions();
    } catch (err) {
      console.error('Upload failed', err);
      alert('Failed to upload file.');
      setLoading(false);
    }
  };

  return (
    <div className="main-content fade-in">
      <div className="dashboard-header">
        <div className="title-section">
          <h1>Tax Preparation (1120-S)</h1>
          <p>Local OCR processing & securely offline ledger</p>
        </div>
        <div style={{ display: 'flex', gap: '1rem' }}>
          <label className="button-primary" style={{ cursor: 'pointer', background: 'var(--surface-hover)', boxShadow: 'none' }}>
            <Upload size={20} />
            Upload File
            <input type="file" style={{ display: 'none' }} onChange={handleUpload} accept=".pdf,.png,.jpg,.jpeg,.csv" />
          </label>
          <button className="button-primary" onClick={handleExport}>
            <Download size={20} />
            Export Book to Excel
          </button>
        </div>
      </div>

      <div className="stats-grid">
        <div className="glass-card stat-item">
          <div className="stat-label">Total Transactions</div>
          <div className="stat-value">{transactions.length}</div>
        </div>
        <div className="glass-card stat-item">
          <div className="stat-label">Processed Documents</div>
          <div className="stat-value" style={{ color: 'var(--primary)' }}>
            {transactions.filter(t => t.referenceDocumentPath).length}
          </div>
        </div>
        <div className="glass-card stat-item">
          <div className="stat-label">System Status</div>
          <div className="stat-value" style={{ color: 'var(--success)' }}>
            Secure & Local
          </div>
        </div>
      </div>

      <div className="glass-card">
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
          <h2 style={{ fontSize: '1.25rem', fontWeight: 600, display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            <Activity size={20} color="var(--primary)" />
            Recent Bookkeeping Logs
          </h2>
          <button 
            onClick={fetchTransactions}
            style={{ background: 'transparent', border: 'none', color: 'var(--text-muted)', cursor: 'pointer' }}
            title="Refresh logs"
          >
            <RefreshCw size={18} className={loading ? "spin" : ""} />
          </button>
        </div>

        {loading ? (
          <div className="empty-state">Loading offline logs...</div>
        ) : error ? (
          <div className="empty-state" style={{ color: 'var(--danger)' }}>
            <AlertCircle className="empty-icon" size={48} color="var(--danger)" />
            <p>{error}</p>
          </div>
        ) : transactions.length === 0 ? (
          <div className="empty-state">
            <FileText className="empty-icon" size={48} />
            <p>No transactions found. Drop a PDF/Image into the Input Directory.</p>
          </div>
        ) : (
          <div className="table-container">
            <table>
              <thead>
                <tr>
                  <th>Date</th>
                  <th>Description</th>
                  <th>Reference File</th>
                </tr>
              </thead>
              <tbody>
                {transactions.map(txn => (
                  <tr key={txn.id}>
                    <td>{format(new Date(txn.date), 'MMM dd, yyyy')}</td>
                    <td>{txn.description}</td>
                    <td>
                      {txn.referenceDocumentPath ? (
                        <span style={{ fontSize: '0.875rem', color: 'var(--text-muted)', background: 'rgba(0,0,0,0.2)', padding: '0.25rem 0.5rem', borderRadius: '0.25rem' }}>
                          {txn.referenceDocumentPath.split(/[\/\\]/).pop()}
                        </span>
                      ) : '-'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};

export default Dashboard;
