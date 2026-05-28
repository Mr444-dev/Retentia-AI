import React, { useState } from 'react';
import { TestTube, FlaskConical, Gauge, Check, Info, BarChart2 } from 'lucide-react';

const ExperimentControl = () => {
    const [activeGroup, setActiveGroup] = useState('Control');
    const [loading, setLoading] = useState(false);

    const experiments = [
        { 
            id: 'Control', 
            name: 'Control Group', 
            desc: 'Baseline Game Balance', 
            color: 'violet',
            sampleSize: 1240,
            retention: '42.1%',
            delta: 'Baseline',
            deltaColor: 'text-gray-400',
            sig: 'N/A'
        },
        { 
            id: 'VariantA', 
            name: 'Variant A (Hard Mode)', 
            desc: '+50% Enemy Health', 
            color: 'red',
            sampleSize: 1198,
            retention: '36.8%',
            delta: '-5.3%',
            deltaColor: 'text-red-400',
            sig: 'p < 0.01 (High)'
        },
        { 
            id: 'VariantB', 
            name: 'Variant B (Easy Mode)', 
            desc: '-20% Respawn Cooldown', 
            color: 'green',
            sampleSize: 1205,
            retention: '49.4%',
            delta: '+7.3%',
            deltaColor: 'text-green-400',
            sig: 'p < 0.05 (Sig)'
        }
    ];

    const setExperiment = async (group) => {
        setLoading(true);
        try {
            await fetch('http://localhost:3001/api/command', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    type: 'SET_EXPERIMENT',
                    payload: JSON.stringify({ group: group })
                })
            });
            setActiveGroup(group);
        } catch (e) {
            console.error(e);
            alert('Failed to set experiment. Ensure server.js is running.');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="glass-panel p-5 flex flex-col h-full bg-gradient-to-b from-transparent to-violet-900/10 border-violet-500/20">
            <div className="card-title text-violet-300 font-bold tracking-widest text-sm flex justify-between">
                <span className="flex items-center gap-2"><TestTube size={16} className="text-violet-400" /> A/B EXPERIMENT LAB</span>
                <span className="text-[9px] bg-violet-500/20 text-violet-300 px-2 py-0.5 rounded border border-violet-500/30">MULTIVARIATE</span>
            </div>

            <div className="flex-1 space-y-4 mt-3 overflow-y-auto">
                <p className="text-xs text-gray-400 leading-relaxed">
                    Test different retention strategies in real-time by remotely overriding active gameplay balance variables.
                </p>

                {/* Experiments Stack */}
                <div className="space-y-2">
                    {experiments.map((exp) => {
                        const isActive = activeGroup === exp.id;
                        return (
                            <button
                                key={exp.id}
                                onClick={() => setExperiment(exp.id)}
                                disabled={loading}
                                className={`w-full p-3 rounded-xl border transition-all text-left flex flex-col gap-2 relative overflow-hidden group
                                    ${isActive
                                        ? `bg-${exp.color}-500/10 border-${exp.color}-500/50 text-white shadow-[0_0_15px_rgba(139,92,246,0.15)]`
                                        : 'bg-black/30 border-white/5 text-gray-400 hover:bg-white/5 hover:border-white/10'
                                    }`}
                            >
                                {/* Active subtle border glow */}
                                {isActive && (
                                    <div className={`absolute top-0 left-0 w-1 h-full bg-${exp.color}-500`} />
                                )}

                                <div className="flex items-center justify-between">
                                    <div className="flex items-center gap-2">
                                        <FlaskConical size={14} className={isActive ? `text-${exp.color}-400` : 'text-gray-600'} />
                                        <span className={`text-xs font-bold ${isActive ? 'text-white' : 'text-gray-300'}`}>
                                            {exp.name}
                                        </span>
                                    </div>
                                    {isActive ? (
                                        <span className={`text-[9px] font-bold px-1.5 py-0.5 rounded bg-${exp.color}-500/20 text-${exp.color}-300 border border-${exp.color}-500/30 flex items-center gap-1`}>
                                            <Check size={8} strokeWidth={4} /> OVERRIDE ACTIVE
                                        </span>
                                    ) : (
                                        <span className="text-[9px] text-gray-600 font-mono">IDLE</span>
                                    )}
                                </div>

                                <div className="text-[10px] text-gray-400 font-mono pl-5">
                                    {exp.desc}
                                </div>

                                {/* Simulated stats shown in a grid */}
                                <div className="grid grid-cols-3 gap-1 pt-2 mt-1 border-t border-white/5 font-mono text-[9px] text-gray-500">
                                    <div>
                                        <div className="text-gray-600 font-sans">N (Users)</div>
                                        <div className={isActive ? "text-gray-300" : ""}>{exp.sampleSize}</div>
                                    </div>
                                    <div>
                                        <div className="text-gray-600 font-sans">D1 Retention</div>
                                        <div className={isActive ? "text-gray-300" : ""}>{exp.retention}</div>
                                    </div>
                                    <div>
                                        <div className="text-gray-600 font-sans">Delta</div>
                                        <div className={exp.deltaColor + " font-bold"}>{exp.delta}</div>
                                    </div>
                                </div>
                            </button>
                        );
                    })}
                </div>
            </div>

            <div className="mt-4 pt-3 border-t border-white/5 flex justify-between items-center text-[10px] text-gray-500 font-mono">
                <span className="flex items-center gap-1">
                    <BarChart2 size={12} className="text-gray-600" /> Stats Confidence
                </span>
                <span className="text-gray-400">
                    Confidence: <span className="text-violet-400">95% CI</span>
                </span>
            </div>
        </div>
    );
};

export default ExperimentControl;
