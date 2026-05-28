import React, { useState, useEffect } from 'react';
import {
  Activity, Users, Zap, Skull, TrendingUp,
  Clock, ShieldAlert, Gift, Terminal
} from 'lucide-react';
import { motion, AnimatePresence } from 'framer-motion';
import { LineChart, Line, XAxis, YAxis, Tooltip, ResponsiveContainer } from 'recharts';
import HeatmapView from './components/HeatmapView';
import ExperimentControl from './components/ExperimentControl';

// Components
const MetricCard = ({ title, value, subvalue, icon: Icon, color }) => (
  <motion.div
    initial={{ opacity: 0, y: 20 }}
    animate={{ opacity: 1, y: 0 }}
    className="glass-panel p-5 relative overflow-hidden group"
  >
    <div className={`absolute top-0 right-0 p-4 opacity-10 group-hover:opacity-20 transition-opacity text-${color}-500`}>
      <Icon size={64} />
    </div>
    <div className="flex items-center gap-2 card-title">
      <Icon size={16} className={`text-${color}-400`} />
      {title}
    </div>
    <div className="text-3xl font-bold mt-1 tracking-tight">{value}</div>
    {subvalue && <div className="text-sm text-gray-400 mt-1">{subvalue}</div>}
  </motion.div>
);

const OfferCard = ({ active }) => {
  const [loading, setLoading] = React.useState(false);

  const triggerOffer = async () => {
    setLoading(true);
    try {
      await fetch('http://localhost:3001/api/command', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          type: 'TRIGGER_OFFER',
          payload: JSON.stringify({ reason: 'manual_override' })
        })
      });
      alert('Offer Triggered!');
    } catch (e) {
      console.error(e);
      alert('Failed to trigger offer. Ensure server.js is running.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="glass-panel p-0 overflow-hidden flex flex-col h-full relative">
      <div className="absolute top-4 right-4 bg-red-500 text-white text-xs font-bold px-2 py-1 rounded shadow-lg animate-pulse">
        LIVE OFFER
      </div>
      <div className="h-48 overflow-hidden bg-black/50 relative">
        <img src="/offer.png" alt="Offer" className="w-full h-full object-cover opacity-90 hover:scale-110 transition-transform duration-700" />
        <div className="absolute inset-0 bg-gradient-to-t from-black/80 to-transparent" />
        <div className="absolute bottom-4 left-4">
          <h3 className="text-xl font-bold text-white neon-text">Starter Bundle</h3>
          <p className="text-cyan-400 font-mono text-sm">300% VALUE</p>
        </div>
      </div>
      <div className="p-4 flex-1 flex flex-col justify-between bg-black/20">
        <div className="space-y-2">
          <div className="flex justify-between text-sm">
            <span className="text-gray-400">Trigger:</span>
            <span className="text-white">Churn Risk {'>'} 70%</span>
          </div>
          <div className="flex justify-between text-sm">
            <span className="text-gray-400">Expires:</span>
            <span className="text-yellow-400 font-mono">04:59</span>
          </div>
        </div>
        <button
          onClick={triggerOffer}
          disabled={loading}
          className="w-full mt-4 py-2 bg-gradient-to-r from-violet-600 to-cyan-500 rounded font-bold text-sm hover:opacity-90 transition-opacity disabled:opacity-50"
        >
          {loading ? 'SENDING...' : 'FORCE TRIGGER'}
        </button>
      </div>
    </div>
  );
};

const LiveLog = ({ logs }) => (
  <div className="glass-panel p-4 flex flex-col h-full">
    <div className="card-title"><Terminal size={14} /> System Logs</div>
    <div className="flex-1 overflow-y-auto font-mono text-xs space-y-2 text-gray-300 pr-2">
      <AnimatePresence>
        {logs.map((log, i) => (
          <motion.div
            key={i}
            initial={{ opacity: 0, x: -20 }}
            animate={{ opacity: 1, x: 0 }}
            className="border-l-2 border-gray-700 pl-2 py-1 hover:bg-white/5 transition-colors"
          >
            <span className="text-gray-500 mr-2">[{log.split(']')[0].replace('[', '')}]</span>
            <span className={log.includes('Flag') ? 'text-yellow-400' : log.includes('Offer') ? 'text-cyan-400' : 'text-gray-300'}>
              {log.split(']').slice(1).join(']')}
            </span>
          </motion.div>
        ))}
      </AnimatePresence>
    </div>
  </div>
);

const dummyChartData = Array.from({ length: 20 }, (_, i) => ({
  name: i,
  value: 50 + Math.random() * 30 - 15,
  risk: 30 + Math.random() * 20
}));

export default function App() {
  const [data, setData] = useState(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const res = await fetch('/data.json');
        const json = await res.json();
        setData(json);
      } catch (e) {
        console.log("Waiting for Unity data...");
      }
    };

    fetchData();
    const interval = setInterval(fetchData, 1000); // 1s polling
    return () => clearInterval(interval);
  }, []);

  // Use dummy logs if no data yet
  const logs = data?.recent_logs?.slice().reverse() || ["Waiting for connection...", "Checking local json..."];
  const user = data?.user_id || "Unknown";
  const churn = (parseFloat(data?.churn_prob || 0) * 100).toFixed(1);
  const score = (parseFloat(data?.retention_score || 0)).toFixed(0);

  return (
    <div className="grid-layout">
      {/* LEFT COLUMN: User Profile */}
      <div className="flex flex-col gap-4">
        <div className="glass-panel p-6 text-center relative overflow-hidden">
          <div className="w-24 h-24 rounded-full bg-gradient-to-tr from-violet-500 to-cyan-400 mx-auto p-[2px]">
            <div className="w-full h-full rounded-full bg-gray-900 flex items-center justify-center overflow-hidden">
              <img src={`https://api.dicebear.com/7.x/avataaars/svg?seed=${user}`} alt="Avatar" />
            </div>
          </div>
          <h2 className="text-xl font-bold mt-4">{user}</h2>
          <div className="inline-block px-3 py-1 rounded-full bg-violet-500/20 text-violet-300 text-xs font-bold mt-2 border border-violet-500/30">
            {data?.segment || "Analyzing..."}
          </div>

          <div className="mt-6 grid grid-cols-2 gap-4 text-left">
            <div>
              <div className="text-xs text-gray-500 uppercase">State</div>
              <div className="font-mono text-green-400">Active</div>
            </div>
            <div>
              <div className="text-xs text-gray-500 uppercase">Duration</div>
              <div className="font-mono text-white">{(data?.session_duration / 60).toFixed(1)}m</div>
            </div>
          </div>
        </div>

        <div className="glass-panel p-4 flex-1">
          <div className="card-title"><ShieldAlert size={14} /> Active Flags</div>
          <div className="space-y-2">
            {data?.active_flags?.length > 0 ? (
              data.active_flags.map((flag, i) => (
                <div key={i} className="bg-red-500/10 border border-red-500/30 text-red-200 px-3 py-2 rounded text-sm flex items-center gap-2">
                  <div className="w-2 h-2 rounded-full bg-red-500 animate-pulse" />
                  {flag}
                </div>
              ))
            ) : (
              <div className="text-gray-500 text-sm italic py-4 text-center">No active flags</div>
            )}
          </div>
        </div>
      </div>

      {/* CENTER COLUMN: Metrics & Charts */}
      <div className="flex flex-col gap-4">
        <div className="grid grid-cols-3 gap-4">
          <MetricCard
            title="Churn Risk"
            value={`${churn}%`}
            subvalue={churn > 50 ? "CRITICAL LEVEL" : "Stable"}
            icon={Skull}
            color={churn > 50 ? "red" : "green"}
          />
          <MetricCard
            title="Retention Score"
            value={score}
            subvalue="Good Engagement"
            icon={Activity}
            color="violet"
          />
          <MetricCard
            title="Offers"
            value={data?.active_offers?.length || 0}
            icon={Gift}
            color="cyan"
          />
        </div>
 
        {/* Analytics history line chart */}
        <div className="glass-panel p-4 h-[200px] flex flex-col relative overflow-hidden bg-black/30 border-violet-500/10">
          <div className="card-title text-violet-300 flex justify-between items-center mb-1">
            <span className="flex items-center gap-2"><Activity size={14} /> HISTORICAL RUN</span>
            <span className="text-[10px] text-gray-500 font-mono">Engagement vs Risk</span>
          </div>
          <div className="flex-1 w-full mt-1">
            <ResponsiveContainer width="100%" height="90%">
              <LineChart data={dummyChartData}>
                <XAxis dataKey="name" stroke="#555" fontSize={8} tickLine={false} />
                <Tooltip contentStyle={{ background: '#111', border: '1px solid #333', borderRadius: 8, fontSize: 10, color: '#fff' }} />
                <Line type="monotone" dataKey="value" stroke="#8b5cf6" strokeWidth={2} dot={false} name="Engagement" />
                <Line type="monotone" dataKey="risk" stroke="#ef4444" strokeWidth={2} dot={false} strokeDasharray="3 3" name="Churn Risk" />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </div>

        <div className="glass-panel p-4 flex-1 min-h-[300px] flex flex-col">
          <HeatmapView
            heatmapPoints={data?.heatmap_points}
            deathPoints={data?.death_points}
          />
        </div>

        <LiveLog logs={logs} />
      </div>

      {/* RIGHT COLUMN: Action Center */}
      <div className="flex flex-col gap-4">
        <div className="glass-panel p-4 bg-gradient-to-br from-violet-900/40 to-transparent border-violet-500/30">
          <div className="card-title text-violet-300"><Zap size={14} /> AI Recommendation</div>
          <p className="text-sm text-gray-300 mt-2 mb-4 leading-relaxed">
            Player is exhibiting signs of frustration (High Retry Count).
            <br /><br />
            <strong>Suggestion:</strong> Trigger "Difficulty Assist" bundle to prevent churn.
          </p>
          <button className="w-full py-2 bg-white/10 hover:bg-white/20 rounded border border-white/10 text-sm transition-colors">
            Analyze with LLM
          </button>
        </div>
        <div className="min-h-[220px]">
          <ExperimentControl />
        </div>

        <OfferCard />

        <div className="glass-panel p-4 flex-1">
          <div className="card-title"><Users size={14} /> Global Stats</div>
          <div className="space-y-4 mt-4">
            <div>
              <div className="flex justify-between text-xs text-gray-400 mb-1">Active Users</div>
              <div className="text-2xl font-bold">1,248</div>
              <div className="w-full bg-gray-700 h-1 mt-2 rounded">
                <div className="bg-cyan-500 w-[70%] h-full rounded" />
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
