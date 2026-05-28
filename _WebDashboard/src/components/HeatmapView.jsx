import React, { useState } from 'react';
import { Map, Skull, Eye, EyeOff, Navigation, Info } from 'lucide-react';

const HeatmapView = ({ heatmapPoints = [], deathPoints = [] }) => {
    const [showHeatmap, setShowHeatmap] = useState(true);
    const [showDeaths, setShowDeaths] = useState(true);
    const [showPaths, setShowPaths] = useState(true);
    const [hoveredPoint, setHoveredPoint] = useState(null);

    const mapSize = 100; // Simulated world unit size (-50 to 50)
    const mapOffset = 50;

    const toPercent = (val) => {
        return Math.max(0, Math.min(100, ((val + mapOffset) / mapSize) * 100));
    };

    return (
        <div className="glass-panel p-5 flex flex-col h-full relative overflow-hidden group border-violet-500/20 bg-gradient-to-b from-black/30 to-violet-950/10">
            {/* Ambient background glow */}
            <div className="absolute -top-10 -left-10 w-40 h-40 bg-violet-600/10 rounded-full blur-3xl pointer-events-none" />
            <div className="absolute -bottom-10 -right-10 w-40 h-40 bg-cyan-600/10 rounded-full blur-3xl pointer-events-none" />

            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 z-10">
                <span className="flex items-center gap-2 font-bold text-sm tracking-widest text-violet-300">
                    <Map size={16} className="text-violet-400 animate-pulse" /> LIVE SPATIAL TELEMETRY
                </span>
                
                {/* Layer Control Buttons */}
                <div className="flex items-center gap-2 bg-black/40 p-1 rounded-lg border border-white/5">
                    <button
                        onClick={() => setShowHeatmap(!showHeatmap)}
                        className={`px-2 py-1 rounded text-[10px] font-bold tracking-wider transition-colors flex items-center gap-1 ${
                            showHeatmap ? 'bg-cyan-500/20 text-cyan-300' : 'text-gray-500 hover:text-gray-300'
                        }`}
                        title="Toggle Heatmap Density"
                    >
                        {showHeatmap ? <Eye size={10} /> : <EyeOff size={10} />} HEAT
                    </button>
                    <button
                        onClick={() => setShowPaths(!showPaths)}
                        className={`px-2 py-1 rounded text-[10px] font-bold tracking-wider transition-colors flex items-center gap-1 ${
                            showPaths ? 'bg-violet-500/20 text-violet-300' : 'text-gray-500 hover:text-gray-300'
                        }`}
                        title="Toggle Trajectory Line"
                    >
                        {showPaths ? <Eye size={10} /> : <EyeOff size={10} />} PATH
                    </button>
                    <button
                        onClick={() => setShowDeaths(!showDeaths)}
                        className={`px-2 py-1 rounded text-[10px] font-bold tracking-wider transition-colors flex items-center gap-1 ${
                            showDeaths ? 'bg-red-500/20 text-red-300' : 'text-gray-500 hover:text-gray-300'
                        }`}
                        title="Toggle Death Coordinates"
                    >
                        {showDeaths ? <Eye size={10} /> : <EyeOff size={10} />} DEATHS
                    </button>
                </div>
            </div>

            <div className="flex-1 relative mt-4 bg-gray-950/70 border border-violet-500/10 rounded-xl overflow-hidden min-h-[250px] shadow-inner">
                {/* Tech grid overlay */}
                <div
                    className="absolute inset-0 opacity-15 pointer-events-none"
                    style={{
                        backgroundImage: 'linear-gradient(rgba(139, 92, 246, 0.15) 1px, transparent 1px), linear-gradient(90deg, rgba(139, 92, 246, 0.15) 1px, transparent 1px)',
                        backgroundSize: '25px 25px',
                        backgroundPosition: 'center'
                    }}
                />

                {/* Level Map Blueprints (Obstacles & Zones) */}
                <svg className="absolute inset-0 w-full h-full pointer-events-none opacity-20">
                    {/* Level boundary */}
                    <rect x="5%" y="5%" width="90%" height="90%" fill="none" stroke="#8b5cf6" strokeWidth="1" strokeDasharray="4 4" />
                    
                    {/* Simulated map walls / obstacles */}
                    <rect x="25%" y="25%" width="15%" height="6%" fill="#06b6d4" rx="2" />
                    <rect x="60%" y="25%" width="15%" height="6%" fill="#06b6d4" rx="2" />
                    <rect x="25%" y="69%" width="15%" height="6%" fill="#06b6d4" rx="2" />
                    <rect x="60%" y="69%" width="15%" height="6%" fill="#06b6d4" rx="2" />
                    
                    {/* Center arena pillar */}
                    <circle cx="50%" cy="50%" r="6%" fill="none" stroke="#8b5cf6" strokeWidth="2" />
                    <circle cx="50%" cy="50%" r="2%" fill="#8b5cf6" />
                    
                    {/* Simulated Danger Zones */}
                    <rect x="40%" y="10%" width="20%" height="5%" fill="rgba(239, 68, 68, 0.2)" stroke="rgba(239, 68, 68, 0.4)" strokeWidth="1" />
                    <rect x="40%" y="85%" width="20%" height="5%" fill="rgba(239, 68, 68, 0.2)" stroke="rgba(239, 68, 68, 0.4)" strokeWidth="1" />
                </svg>

                {/* Map Center Origin Marker */}
                <div className="absolute top-1/2 left-1/2 w-4 h-4 border border-violet-500/30 rounded-full -translate-x-1/2 -translate-y-1/2 pointer-events-none flex items-center justify-center">
                    <div className="w-1 h-1 bg-violet-500 rounded-full" />
                </div>

                {/* Heatmap density spots */}
                {showHeatmap && heatmapPoints.map((p, i) => (
                    <div
                        key={`h-${i}`}
                        className="absolute w-5 h-5 bg-cyan-500/20 rounded-full blur-[4px] hover:bg-cyan-400/40 hover:scale-125 transition-all duration-300 cursor-crosshair"
                        style={{
                            left: `${toPercent(p.x)}%`,
                            top: `${100 - toPercent(p.y)}%`,
                            transform: 'translate(-50%, -50%)'
                        }}
                        onMouseEnter={() => setHoveredPoint({ x: p.x, y: p.y, type: 'Movement Trace' })}
                        onMouseLeave={() => setHoveredPoint(null)}
                    />
                ))}

                {/* Simulated connection path */}
                {showPaths && heatmapPoints.length > 1 && (
                    <svg className="absolute inset-0 w-full h-full pointer-events-none opacity-40">
                        <polyline
                            points={heatmapPoints.slice(-15).map(p => `${toPercent(p.x)}% ${100 - toPercent(p.y)}%`).join(', ')}
                            fill="none"
                            stroke="url(#pathGradient)"
                            strokeWidth="2.5"
                            strokeLinecap="round"
                            strokeLinejoin="round"
                        />
                        <defs>
                            <linearGradient id="pathGradient" x1="0%" y1="0%" x2="100%" y2="100%">
                                <stop offset="0%" stopColor="#8b5cf6" />
                                <stop offset="100%" stopColor="#06b6d4" />
                            </linearGradient>
                        </defs>
                    </svg>
                )}

                {/* Skull Markers (Death Points) */}
                {showDeaths && deathPoints.map((p, i) => (
                    <button
                        key={`d-${i}`}
                        className="absolute text-red-500 hover:text-red-400 hover:scale-125 transition-transform duration-200 z-10 focus:outline-none"
                        style={{
                            left: `${toPercent(p.x)}%`,
                            top: `${100 - toPercent(p.y)}%`,
                            transform: 'translate(-50%, -50%)'
                        }}
                        onClick={() => setHoveredPoint({ x: p.x, y: p.y, type: 'Player Fatality' })}
                        onMouseEnter={() => setHoveredPoint({ x: p.x, y: p.y, type: 'Player Fatality' })}
                        onMouseLeave={() => setHoveredPoint(null)}
                    >
                        <Skull size={18} className="drop-shadow-[0_0_6px_rgba(239,68,68,0.8)] animate-pulse" />
                    </button>
                ))}

                {/* Hover Coordinate HUD Tooltip */}
                {hoveredPoint && (
                    <div className="absolute bottom-3 left-3 bg-black/80 backdrop-blur-md border border-violet-500/30 p-2 rounded-lg text-[10px] font-mono shadow-2xl z-20 pointer-events-none flex flex-col gap-1 min-w-[120px]">
                        <span className="text-violet-300 font-bold tracking-wider border-b border-white/10 pb-1 mb-1">
                            {hoveredPoint.type.toUpperCase()}
                        </span>
                        <div className="flex justify-between gap-4">
                            <span className="text-gray-500">COORD X:</span>
                            <span className="text-cyan-400 font-bold">{hoveredPoint.x.toFixed(2)}</span>
                        </div>
                        <div className="flex justify-between gap-4">
                            <span className="text-gray-500">COORD Y:</span>
                            <span className="text-cyan-400 font-bold">{hoveredPoint.y.toFixed(2)}</span>
                        </div>
                    </div>
                )}

                {/* Stats HUD overlay */}
                <div className="absolute bottom-2 right-3 flex gap-3 text-[9px] text-gray-400 font-mono tracking-wider bg-black/40 px-2 py-1 rounded border border-white/5 pointer-events-none">
                    <span className="flex items-center gap-1"><Navigation size={8} /> HEAT: {heatmapPoints.length}</span>
                    <span className="text-red-400 flex items-center gap-1"><Skull size={8} /> DEATHS: {deathPoints.length}</span>
                </div>
            </div>
        </div>
    );
};

export default HeatmapView;
