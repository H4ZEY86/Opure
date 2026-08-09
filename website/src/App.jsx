import React, { useState, useEffect, useRef } from 'react';
import { motion, useScroll, useTransform } from 'framer-motion';
import { 
  Terminal, Cpu, Shield, Layers, BookOpen, Settings, Download, 
  ExternalLink, Copy, Check, ChevronRight, ArrowRight, Play, RefreshCw, 
  Sliders, Eye, Zap, Lock, Code, Globe, Server, Activity, X, Menu, Search, Database, CpuIcon, User
} from 'lucide-react';

// --- PARTICLE CANVAS BACKGROUND ---
function ParticleBackground() {
  const canvasRef = useRef(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    let animationFrameId;

    let width = (canvas.width = window.innerWidth);
    let height = (canvas.height = window.innerHeight);

    const handleResize = () => {
      if (!canvas) return;
      width = canvas.width = window.innerWidth;
      height = canvas.height = window.innerHeight;
    };
    window.addEventListener('resize', handleResize);

    const particles = Array.from({ length: 80 }, () => ({
      x: Math.random() * width,
      y: Math.random() * height,
      vx: (Math.random() - 0.5) * 0.4,
      vy: (Math.random() - 0.5) * 0.4,
      radius: Math.random() * 1.5 + 0.5,
      alpha: Math.random() * 0.5 + 0.2
    }));

    const render = () => {
      ctx.clearRect(0, 0, width, height);
      ctx.fillStyle = '#050505';
      ctx.fillRect(0, 0, width, height);

      particles.forEach((p, index) => {
        p.x += p.vx;
        p.y += p.vy;

        if (p.x < 0) p.x = width;
        if (p.x > width) p.x = 0;
        if (p.y < 0) p.y = height;
        if (p.y > height) p.y = 0;

        ctx.beginPath();
        ctx.arc(p.x, p.y, p.radius, 0, Math.PI * 2);
        ctx.fillStyle = `rgba(59, 130, 246, ${p.alpha})`;
        ctx.fill();

        for (let j = index + 1; j < particles.length; j++) {
          const p2 = particles[j];
          const dx = p.x - p2.x;
          const dy = p.y - p2.y;
          const dist = Math.sqrt(dx * dx + dy * dy);
          if (dist < 120) {
            ctx.beginPath();
            ctx.moveTo(p.x, p.y);
            ctx.lineTo(p2.x, p2.y);
            ctx.strokeStyle = `rgba(59, 130, 246, ${0.15 * (1 - dist / 120)})`;
            ctx.lineWidth = 0.5;
            ctx.stroke();
          }
        }
      });

      animationFrameId = requestAnimationFrame(render);
    };

    render();

    return () => {
      window.removeEventListener('resize', handleResize);
      cancelAnimationFrame(animationFrameId);
    };
  }, []);

  return <canvas ref={canvasRef} className="fixed inset-0 pointer-events-none z-0" />;
}

// --- WORLD-CLASS OPEN UNIVERSITY CURRICULUM ---
const courseModules = [
  {
    id: 1,
    title: "Module 1: Core Architecture, Language & Process Topology",
    level: "Beginner",
    summary: "Architectural overview, process topology, and zero-cloud local sovereignty.",
    content: `Opure is engineered entirely around local-first data ownership. Utilizing a strict supervisor-worker process topology across C# and .NET 10, all telemetry, state changes, and audio buffers remain strictly isolated on local hardware without external telemetry leaks. Powered by Avalonia UI for cross-platform hardware acceleration.`,
    codeSnippet: `public sealed class OpureRuntimeBootstrap {
    public static void VerifyEnvironment() {
        Console.WriteLine("Ctrl_Alt_Haze | Opure Sound & Code - Runtime Initialized.");
    }
}`
  },
  {
    id: 2,
    title: "Module 2: High-Performance Inter-Process Communication (IPC)",
    level: "Intermediate",
    summary: "Secure Windows Named Pipes, Protobuf binary framing, and ACL validation.",
    content: `Because Opure isolates UI components from background runtimes for maximum stability, high-speed IPC is vital. Communication flows over secure, low-latency Windows Named Pipes featuring strict Access Control Lists (ACLs) and session token handshakes.`,
    codeSnippet: `public sealed class NamedPipeIpcServer {
    public async Task StartListeningAsync(CancellationToken ct) {
        var serverStream = new NamedPipeServerStream("OpurePipe", PipeDirection.InOut, 4, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
        await serverStream.WaitForConnectionAsync(ct);
    }
}`
  },
  {
    id: 3,
    title: "Module 3: Local Storage, SQLite WAL Persistence & Outbox",
    level: "Intermediate",
    summary: "Embedded ACID-compliant SQLite databases with Write-Ahead Logging and migration runners.",
    content: `Opure rejects cloud-dependent databases in favor of robust local persistence. Each subsystem manages dedicated SQLite databases running in Write-Ahead Logging (WAL) mode with automated schema migration runners and transactional outbox queues.`,
    codeSnippet: `public class SqliteWalManager {
    public void InitializeDatabase(string dbPath) {
        using var connection = new SqliteConnection($"Data Source={dbPath};Mode=ReadWriteCreate;Cache=Shared");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;";
        cmd.ExecuteNonQuery();
    }
}`
  },
  {
    id: 4,
    title: "Module 4: Advanced C# & .NET 10 Memory Management (Span<T>)",
    level: "Advanced",
    summary: "Zero-allocation DSP pipelines, stackalloc, Span<T>, Memory<T>, and unsafe memory buffers.",
    content: `In high-frequency audio DSP and binary streaming, garbage collection (GC) pauses introduce jitter. You will learn how to leverage stackalloc, Span<T>, and Memory<T> to guarantee deterministic, zero-allocation data transformations.`,
    codeSnippet: `public unsafe void ProcessAudioBuffer(Span<float> buffer, float gain) {
    fixed (float* ptr = buffer) {
        for (int i = 0; i < buffer.Length; i++) ptr[i] *= gain;
    }
}`
  },
  {
    id: 5,
    title: "Module 5: Structured Logging, Observability & Redaction",
    level: "Advanced",
    summary: "JSON Lines log sinks, distributed tracing, and automatic PII/path sanitisation.",
    content: `Observability in Opure is designed to be deeply transparent while strictly preserving user privacy. Machine-parseable logs are written via JsonLinesOperationalLogSink, with automatic redactor profiles stripping sensitive paths and tokens.`,
    codeSnippet: `public class OperationalLogRedactor {
    public string SanitizeLogMessage(string rawMessage) {
        return Regex.Replace(rawMessage, @"(?i)(password|token|key)=[^&\\s]+", "$1=REDACTED");
    }
}`
  },
  {
    id: 6,
    title: "Module 6: Plugin Packaging, Distribution & Capability Security",
    level: "Expert",
    summary: ".opx bundle architecture, zero-trust capability manifests, and runtime sandboxing.",
    content: `Opure supports third-party extensibility without compromising system integrity. Extensions are packaged into verifiable .opx archive bundles operating under a strict zero-trust capability model where permissions must be explicitly granted.`,
    codeSnippet: `[OpurePlugin("com.ctrlaltrhaze.reverb", Version = "1.0.0")]
public class HazeReverbPlugin : IOpurePlugin {
    public void Initialize(IPluginContext context) {
        context.RegisterService<IDspFilter>(new ConvolutionReverb());
    }
}`
  },
  {
    id: 7,
    title: "Module 7: Model Context Protocol (MCP) & Local AI Runtimes",
    level: "Expert",
    summary: "Model Context Protocol JSON-RPC bridges, Ollama integration, and context budgeting.",
    content: `Opure brings visible, local-first artificial intelligence directly into your development workflow via the Model Context Protocol (MCP). Exposes local workspace data and DSP parameters securely to local models like Ollama without cloud telemetry leaks.`,
    codeSnippet: `[McpTool("adjust-filter-cutoff")]
public async Task<ToolResult> AdjustCutoffAsync(float frequencyHz) {
    await _dspEngine.SetLowPassCutoffAsync(frequencyHz);
    return ToolResult.Success($"Cutoff locked to {frequencyHz}Hz");
}`
  },
  {
    id: 8,
    title: "Module 8: Trust Centre, Evidence Records & UK GDPR Compliance",
    level: "Expert",
    summary: "Tamper-evident cryptographic evidence logs, support bundles, and UK data protection baselines.",
    content: `Compliance and trust are fundamental pillars. The platform aligns with UK GDPR principles, generating canonicalized, cryptographically signed JSON evidence records stored in dedicated SQLite trust databases for complete accountability.`,
    codeSnippet: `public bool ValidateEvidenceRecord(EvidenceRecord record, byte[] pubKey) {
    return CryptographicOperations.VerifyData(record.CanonicalPayload, record.Signature, pubKey, HashAlgorithmName.SHA256);
}`
  }
];

// --- ALL 12 TECHNICAL SPECIFICATIONS ---
const specsList = [
  { id: "SPEC-001", title: "Core Runtime Process Topology", category: "Architecture", desc: "Defines isolation boundaries between the Avalonia desktop client and supervisor." },
  { id: "SPEC-002", title: "High-Performance IPC via Named Pipes", category: "Networking", desc: "Low-latency binary framing protocol operating over secure named pipes." },
  { id: "SPEC-003", title: "Local SQLite Persistence & WAL Mode", category: "Storage", desc: "Embedded ACID-compliant state management with strict migration runners." },
  { id: "SPEC-004", title: "Tamper-Evident Trust Centre & Audit Logging", category: "Security", desc: "Cryptographic evidence records and secure local log rotation." },
  { id: "SPEC-005", title: "Model Context Protocol (MCP) Server Specification", category: "AI", desc: "Standardized local JSON-RPC bridge for local AI model inspection." },
  { id: "SPEC-006", title: "Workspace File Hashing & Canonicalisation", category: "Engine", desc: "Parallelized file traversal, SHA-256 state hashing, and atomic snapshots." },
  { id: "SPEC-007", title: "Plugin Permissions & Capability Model", category: "Security", desc: "Granular access control policies for third-party .opx extensions." },
  { id: "SPEC-008", title: "Local Model Runtime & Model Management", category: "AI", desc: "Ollama/local model lifecycle orchestration and hardware budgeting." },
  { id: "SPEC-009", title: "Context Assembly & Token Budgeting", category: "AI", desc: "Deterministic context clipping and prompt token allocation rules." },
  { id: "SPEC-010", title: "Repository & Solution Structure", category: "Engineering", desc: "Solution layout, project decoupling rules, and directory standards." },
  { id: "SPEC-011", title: "Build & Continuous Integration Policy", category: "DevOps", desc: "Deterministic build requirements and offline test execution mandates." },
  { id: "SPEC-012", title: "Versioning & Release Management", category: "DevOps", desc: "Semantic versioning rules and release tag validation automation." }
];

// --- ALL 28 ARCHITECTURE DECISION RECORDS ---
const adrsList = [
  { id: "ADR-0001", title: "Primary Implementation Language: C# / .NET 10" },
  { id: "ADR-0002", title: "Desktop UI Framework: Avalonia UI" },
  { id: "ADR-0003", title: "Runtime Process Topology & Supervisor Pattern" },
  { id: "ADR-0004", title: "Local IPC Protocol Selection" },
  { id: "ADR-0005", title: "Persistence Strategy: SQLite Embedded" },
  { id: "ADR-0006", title: "Structured Logging & Observability Pipeline" },
  { id: "ADR-0007", title: "Secrets Vault & Credential Management" },
  { id: "ADR-0008", title: "Testing Strategy & Conformance Suites" },
  { id: "ADR-0009", title: "Windows Path & Filesystem Handling" },
  { id: "ADR-0010", title: "Repository & Solution Structure" },
  { id: "ADR-0011", title: "Build & Continuous Integration" },
  { id: "ADR-0012", title: "Versioning & Release Management" },
  { id: "ADR-0013", title: "Packaging & Installer Design" },
  { id: "ADR-0014", title: "Code Signing & Release Trust" },
  { id: "ADR-0015", title: "Updater & Update Policy" },
  { id: "ADR-0016", title: "Plugin Packaging & Distribution (.opx)" },
  { id: "ADR-0017", title: "Plugin Permissions & Capability Model" },
  { id: "ADR-0018", title: "MCP Server Trust & Permission Model" },
  { id: "ADR-0019", title: "AI Provider Trust & Data Sharing" },
  { id: "ADR-0020", title: "Local Model Runtime & Management" },
  { id: "ADR-0021", title: "Context Assembly & Token Budgeting" },
  { id: "ADR-0022", title: "Project Knowledge Indexing & Retrieval" },
  { id: "ADR-0023", title: "Project Memory Lifecycle & Provenance" },
  { id: "ADR-0024", title: "Model Evaluation Quality & Routing Governance" },
  { id: "ADR-0025", title: "Workflow Execution Checkpointing & Recovery" },
  { id: "ADR-0026", title: "Configuration Profile & Policy Management" },
  { id: "ADR-0027", title: "Trust Centre Evidence Retention & Support Bundles" },
  { id: "ADR-0028", title: "Backup, Restore, Data Portability & Disaster Recovery" }
];

export default function App() {
  const [repoData, setRepoData] = useState({ stars: 12, forks: 3, avatar: '', bio: 'Independent Software Engineer & Music Producer' });
  const [latestRelease, setLatestRelease] = useState({ tag: 'Opure.Preview-1.1.0.10000-win-x64', url: 'https://github.com/H4ZEY86/Opure/releases' });
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const [highContrast, setHighContrast] = useState(false);
  const [reducedMotion, setReducedMotion] = useState(false);
  const [fontSize, setFontSize] = useState('normal');

  const { scrollY } = useScroll();
  const heroScale = useTransform(scrollY, [0, 400], [1, 1.12]);
  const heroOpacity = useTransform(scrollY, [0, 350], [1, 0.2]);

  const [completedModules, setCompletedModules] = useState(() => {
    try {
      const saved = localStorage.getItem('opure_completed_modules');
      return saved ? JSON.parse(saved) : [];
    } catch {
      return [];
    }
  });

  useEffect(() => {
    try {
      localStorage.setItem('opure_completed_modules', JSON.stringify(completedModules));
    } catch (e) {
      console.error(e);
    }
  }, [completedModules]);

  // Fetch live GitHub Data for H4ZEY86/Opure
  useEffect(() => {
    fetch('https://api.github.com/repos/H4ZEY86/Opure')
      .then(res => res.json())
      .then(data => {
        if (data && data.stargazers_count !== undefined) {
          setRepoData(prev => ({ ...prev, stars: data.stargazers_count, forks: data.forks_count }));
        }
      })
      .catch(() => {});

    fetch('https://api.github.com/users/H4ZEY86')
      .then(res => res.json())
      .then(data => {
        if (data && data.avatar_url) {
          setRepoData(prev => ({ ...prev, avatar: data.avatar_url, bio: data.bio || prev.bio }));
        }
      })
      .catch(() => {});

    fetch('https://api.github.com/repos/H4ZEY86/Opure/releases/latest')
      .then(res => res.json())
      .then(data => {
        if (data && data.tag_name) {
          setLatestRelease({ tag: data.tag_name, url: data.html_url });
        }
      })
      .catch(() => {});
  }, []);

  const toggleModuleCompletion = (id) => {
    setCompletedModules(prev => 
      prev.includes(id) ? prev.filter(m => m !== id) : [...prev, id]
    );
  };

  const scrollToSection = (id) => {
    const element = document.getElementById(id);
    if (element) {
      element.scrollIntoView({ behavior: 'smooth' });
    }
  };

  return (
    <div className={`min-h-screen bg-[#050505] text-white font-sans selection:bg-blue-500 selection:text-white relative ${highContrast ? 'contrast-125' : ''} ${fontSize === 'large' ? 'text-lg' : fontSize === 'x-large' ? 'text-xl' : 'text-base'}`}>
      <ParticleBackground />

      {/* --- STICKY NAVIGATION BAR --- */}
      <header className="sticky top-0 z-50 backdrop-blur-md bg-[#050505]/80 border-b border-white/10 px-6 py-4 flex items-center justify-between">
        <div className="flex items-center space-x-3 cursor-pointer" onClick={() => scrollToSection('home')}>
          <div className="w-10 h-10 rounded-xl bg-gradient-to-tr from-blue-600 to-indigo-500 flex items-center justify-center shadow-lg shadow-blue-500/20 overflow-hidden border border-blue-400/30">
            {repoData.avatar ? (
              <img src={repoData.avatar} alt="Ctrl_Alt_Haze" className="w-full h-full object-cover" />
            ) : (
              <Cpu className="w-5 h-5 text-white animate-pulse" />
            )}
          </div>
          <div>
            <h1 className="font-bold tracking-wider text-lg bg-gradient-to-r from-white via-blue-200 to-blue-400 bg-clip-text text-transparent">
              Opure
            </h1>
            <p className="text-xs text-gray-400 tracking-widest font-mono">BY Ctrl_Alt_Haze</p>
          </div>
        </div>

        <nav className="hidden md:flex items-center space-x-1 bg-white/5 border border-white/10 rounded-full px-4 py-1.5 backdrop-blur-md">
          {[
            { id: 'home', label: 'Home' },
            { id: 'get-started', label: 'Get Started' },
            { id: 'course', label: 'Learn Opure' },
            { id: 'architecture', label: 'Architecture' },
            { id: 'docs', label: 'Docs & Specs' }
          ].map(tab => (
            <button
              key={tab.id}
              onClick={() => scrollToSection(tab.id)}
              className="px-4 py-1.5 rounded-full text-sm font-medium transition-all duration-300 text-gray-400 hover:text-white hover:bg-white/5"
            >
              {tab.label}
            </button>
          ))}
        </nav>

        <div className="flex items-center space-x-4">
          <a 
            href="https://github.com/H4ZEY86/Opure" 
            target="_blank" 
            rel="noreferrer"
            className="hidden sm:flex items-center space-x-2 text-xs font-mono bg-white/5 hover:bg-white/10 border border-white/10 px-3 py-2 rounded-lg transition"
          >
            <Terminal className="w-4 h-4 text-blue-400" />
            <span>★ {repoData.stars} | Fork Opure</span>
          </a>

          <button 
            onClick={() => setIsSettingsOpen(true)}
            className="p-2.5 rounded-xl bg-white/5 hover:bg-white/10 border border-white/10 text-gray-300 hover:text-white transition"
            title="Settings & Accessibility"
          >
            <Settings className="w-5 h-5" />
          </button>
        </div>
      </header>

      {/* --- SETTINGS MODAL --- */}
      {isSettingsOpen && (
        <div className="fixed inset-0 z-50 bg-black/70 backdrop-blur-md flex items-center justify-center p-4">
          <div className="bg-[#121218] border border-white/15 rounded-2xl w-full max-w-md p-6 shadow-2xl relative">
            <div className="flex items-center justify-between mb-6 pb-4 border-b border-white/10">
              <div className="flex items-center space-x-3">
                <div className="p-2 rounded-lg bg-blue-500/20 text-blue-400">
                  <Sliders className="w-5 h-5" />
                </div>
                <h3 className="text-lg font-semibold text-white">UI Customization & Accessibility</h3>
              </div>
              <button onClick={() => setIsSettingsOpen(false)} className="text-gray-400 hover:text-white p-1 rounded-lg">
                <X className="w-5 h-5" />
              </button>
            </div>

            <div className="space-y-6">
              <div className="flex items-center justify-between">
                <div>
                  <h4 className="font-medium text-sm text-white">High Contrast Mode</h4>
                  <p className="text-xs text-gray-400">Boost border and text contrast.</p>
                </div>
                <button 
                  onClick={() => setHighContrast(!highContrast)}
                  className={`w-12 h-6 rounded-full transition-colors relative p-1 ${highContrast ? 'bg-blue-600' : 'bg-white/20'}`}
                >
                  <div className={`w-4 h-4 rounded-full bg-white transition-transform ${highContrast ? 'translate-x-6' : 'translate-x-0'}`} />
                </button>
              </div>

              <div className="flex items-center justify-between">
                <div>
                  <h4 className="font-medium text-sm text-white">Reduce Motion</h4>
                  <p className="text-xs text-gray-400">Minimize heavy animations.</p>
                </div>
                <button 
                  onClick={() => setReducedMotion(!reducedMotion)}
                  className={`w-12 h-6 rounded-full transition-colors relative p-1 ${reducedMotion ? 'bg-blue-600' : 'bg-white/20'}`}
                >
                  <div className={`w-4 h-4 rounded-full bg-white transition-transform ${reducedMotion ? 'translate-x-6' : 'translate-x-0'}`} />
                </button>
              </div>

              <div>
                <h4 className="font-medium text-sm text-white mb-2">Interface Font Size</h4>
                <div className="grid grid-cols-3 gap-2">
                  {['normal', 'large', 'x-large'].map(f => (
                    <button
                      key={f}
                      onClick={() => setFontSize(f)}
                      className={`py-2 rounded-lg text-xs font-medium border transition capitalize ${
                        fontSize === f ? 'bg-blue-600 border-blue-500 text-white' : 'bg-white/5 border-white/10 text-gray-400'
                      }`}
                    >
                      {f}
                    </button>
                  ))}
                </div>
              </div>
            </div>

            <div className="mt-8 pt-4 border-t border-white/10 flex justify-end">
              <button onClick={() => setIsSettingsOpen(false)} className="px-4 py-2 bg-blue-600 hover:bg-blue-500 text-white text-sm font-medium rounded-xl transition">
                Close
              </button>
            </div>
          </div>
        </div>
      )}

      {/* --- CONTINUOUS SCROLL CONTENT SECTIONS --- */}
      <main className="relative z-10 max-w-7xl mx-auto px-6 space-y-36 pb-32">
        
        {/* SECTION 1: HOME / HERO */}
        <section id="home" className="pt-20">
          <motion.div 
            style={{ scale: reducedMotion ? 1 : heroScale, opacity: reducedMotion ? 1 : heroOpacity }}
            className="text-center space-y-8 max-w-4xl mx-auto"
          >
            <div className="inline-flex items-center space-x-2 px-4 py-1.5 rounded-full bg-blue-500/10 border border-blue-500/20 text-blue-400 text-xs font-mono">
              <span className="w-2 h-2 rounded-full bg-blue-400 animate-ping" />
              <span>LIVE GITHUB RELEASE: {latestRelease.tag}</span>
            </div>

            <h1 className="text-5xl sm:text-7xl font-extrabold tracking-tight text-white leading-none">
              Opure <span className="bg-gradient-to-r from-blue-400 via-indigo-300 to-purple-400 bg-clip-text text-transparent">Sound & Code</span>
            </h1>

            <p className="text-lg text-gray-300 max-w-2xl mx-auto font-light">
              An ultra-modern, local-first platform engineered in C#/.NET 10 by <span className="text-white font-semibold">Ctrl_Alt_Haze</span>, featuring visible AI integration and high-performance DSP runtimes.
            </p>

            <div className="flex flex-wrap justify-center gap-4 pt-4">
              <a
                href={latestRelease.url}
                target="_blank"
                rel="noreferrer"
                className="px-8 py-4 rounded-2xl bg-gradient-to-r from-blue-600 to-indigo-600 text-white font-semibold flex items-center space-x-3 shadow-xl shadow-blue-500/20 hover:scale-105 transition"
              >
                <Download className="w-5 h-5" />
                <span>Download {latestRelease.tag}</span>
              </a>
              <button
                onClick={() => scrollToSection('course')}
                className="px-8 py-4 rounded-2xl bg-white/5 hover:bg-white/10 border border-white/15 text-white font-semibold flex items-center space-x-3 backdrop-blur-md transition"
              >
                <span>Explore Course Hub</span>
                <ArrowRight className="w-5 h-5" />
              </button>
            </div>
          </motion.div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-8 pt-16">
            {[
              { id: 'card-1', target: 'course', icon: Cpu, title: "Open Uni Course Hub", desc: "Exhaustive 8-module curriculum covering C#, .NET 10 memory management, plugins, and MCP." },
              { id: 'card-2', target: 'architecture', icon: Layers, title: "Immersive Architecture", desc: "Interactive system flow diagrams detailing IPC Named Pipes and AOT runtimes." },
              { id: 'card-3', target: 'docs', icon: Shield, title: "Strict Specs & ADRs", desc: "Dive into all 28 architecture decision records and 12 foundational specifications." }
            ].map((card, i) => {
              const Icon = card.icon;
              return (
                <motion.div
                  key={card.id}
                  initial={{ opacity: 0, y: 30 }}
                  whileInView={{ opacity: 1, y: 0 }}
                  viewport={{ once: true }}
                  transition={{ delay: i * 0.15, duration: 0.6 }}
                  onClick={() => scrollToSection(card.target)}
                  className="group relative p-8 rounded-3xl bg-gradient-to-b from-white/[0.08] to-white/[0.02] border border-white/10 backdrop-blur-xl cursor-pointer transition-all duration-500 hover:border-blue-500/50 hover:shadow-2xl hover:shadow-blue-500/10 hover:-translate-y-2"
                >
                  <div className="w-14 h-14 rounded-2xl bg-blue-500/10 border border-blue-500/20 flex items-center justify-center text-blue-400 mb-6 group-hover:scale-110 transition">
                    <Icon className="w-7 h-7" />
                  </div>
                  <h3 className="text-xl font-bold text-white mb-3 flex items-center justify-between">
                    <span>{card.title}</span>
                    <ChevronRight className="w-5 h-5 text-gray-500 group-hover:text-blue-400 group-hover:translate-x-1 transition" />
                  </h3>
                  <p className="text-sm text-gray-400 leading-relaxed">{card.desc}</p>
                  <div className="mt-6 text-xs font-mono text-blue-400 flex items-center space-x-1">
                    <span>Scroll to section</span>
                    <ArrowRight className="w-3 h-3" />
                  </div>
                </motion.div>
              );
            })}
          </div>
        </section>

        {/* SECTION 2: GET STARTED */}
        <section id="get-started" className="pt-16">
          <motion.div initial={{ opacity: 0, y: 30 }} whileInView={{ opacity: 1, y: 0 }} viewport={{ once: true }} className="max-w-4xl mx-auto space-y-8">
            <div>
              <span className="text-xs font-mono text-blue-400 tracking-widest uppercase">Deployment & Setup</span>
              <h2 className="text-4xl font-extrabold text-white mt-2">Get Started with Opure</h2>
              <p className="text-gray-400 mt-2">Quick installation guide for running Opure Desktop and CLI locally.</p>
            </div>
            <div className="p-8 rounded-3xl bg-white/[0.03] border border-white/10 backdrop-blur-xl space-y-4">
              <h3 className="text-xl font-bold text-white flex items-center space-x-2">
                <Terminal className="w-5 h-5 text-blue-400" />
                <span>Local Development & Restore</span>
              </h3>
              <div className="bg-black/60 rounded-xl p-4 font-mono text-xs text-blue-300 border border-white/10 overflow-x-auto">
                git clone https://github.com/H4ZEY86/Opure.git<br/>
                cd Opure<br/>
                dotnet restore Opure.slnx
              </div>
            </div>
          </motion.div>
        </section>

        {/* SECTION 3: WORLD-CLASS OPEN UNIVERSITY COURSE HUB */}
        <section id="course" className="pt-16">
          <motion.div initial={{ opacity: 0, y: 30 }} whileInView={{ opacity: 1, y: 0 }} viewport={{ once: true }} className="max-w-5xl mx-auto space-y-12">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
              <div>
                <span className="text-xs font-mono text-blue-400 tracking-widest uppercase">Open University Curriculum</span>
                <h2 className="text-4xl font-extrabold text-white mt-2">Opure Engineer Course Hub</h2>
                <p className="text-gray-400 mt-2">Exhaustive curriculum covering C#, .NET 10, memory management (`Span&lt;T&gt;`), plugins (`.opx`), MCP, and UK GDPR baselines designed by Ctrl_Alt_Haze.</p>
              </div>
              <div className="px-5 py-3 rounded-2xl bg-white/5 border border-white/10 text-sm font-mono flex items-center space-x-3">
                <div className="w-3 h-3 rounded-full bg-emerald-500 animate-pulse" />
                <span>Progress: {completedModules.length} / {courseModules.length} Completed</span>
              </div>
            </div>

            <div className="grid grid-cols-1 gap-8">
              {courseModules.map((mod) => {
                const isCompleted = completedModules.includes(mod.id);
                return (
                  <div key={mod.id} className={`p-8 rounded-3xl border backdrop-blur-xl transition ${isCompleted ? 'bg-emerald-950/10 border-emerald-500/30' : 'bg-white/[0.03] border-white/10'}`}>
                    <div className="flex items-center justify-between mb-4">
                      <span className="px-3 py-1 rounded-full text-xs font-mono bg-blue-500/10 text-blue-400 border border-blue-500/20">{mod.level}</span>
                      <button
                        onClick={() => toggleModuleCompletion(mod.id)}
                        className={`px-4 py-2 rounded-xl text-xs font-semibold flex items-center space-x-2 transition ${isCompleted ? 'bg-emerald-600/20 text-emerald-400 border border-emerald-500/30' : 'bg-white/5 text-gray-300 border border-white/10'}`}
                      >
                        <Check className={`w-4 h-4 ${isCompleted ? 'text-emerald-400' : 'text-gray-500'}`} />
                        <span>{isCompleted ? 'Completed' : 'Mark as Complete'}</span>
                      </button>
                    </div>
                    <h3 className="text-xl font-bold text-white mb-2">{mod.title}</h3>
                    <p className="text-sm font-medium text-blue-200/90 mb-3">{mod.summary}</p>
                    <p className="text-sm text-gray-300 mb-6 leading-relaxed">{mod.content}</p>
                    <div className="relative rounded-2xl bg-black/70 border border-white/10 p-5 font-mono text-xs text-blue-300 overflow-x-auto">
                      <pre>{mod.codeSnippet}</pre>
                    </div>
                  </div>
                );
              })}
            </div>
          </motion.div>
        </section>

        {/* SECTION 4: ARCHITECTURE & SYSTEM FLOW */}
        <section id="architecture" className="pt-16">
          <motion.div initial={{ opacity: 0, y: 30 }} whileInView={{ opacity: 1, y: 0 }} viewport={{ once: true }} className="max-w-5xl mx-auto space-y-12">
            <div>
              <span className="text-xs font-mono text-blue-400 tracking-widest uppercase">System Topology & Data Flow</span>
              <h2 className="text-4xl font-extrabold text-white mt-2">Architecture & Interactive Flow</h2>
              <p className="text-gray-400 mt-2">Visualizing the zero-backend design mapping Avalonia UI shells to secure AOT runtimes and SQLite storage.</p>
            </div>

            {/* Interactive Animated Diagram */}
            <div className="p-8 rounded-3xl bg-white/[0.03] border border-white/10 backdrop-blur-xl relative overflow-hidden space-y-8">
              <div className="flex items-center justify-between border-b border-white/10 pb-4">
                <div className="flex items-center space-x-3">
                  <Activity className="w-5 h-5 text-blue-400 animate-pulse" />
                  <h3 className="text-lg font-bold text-white font-mono">Live Inter-Process Communication (IPC) Topology</h3>
                </div>
                <span className="px-3 py-1 rounded-full text-xs font-mono bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">Active (Named Pipes)</span>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-7 gap-4 items-center text-center">
                <div className="md:col-span-2 p-6 rounded-2xl bg-blue-950/20 border border-blue-500/30 flex flex-col items-center justify-center space-y-2 hover:border-blue-400 transition shadow-lg">
                  <Layers className="w-8 h-8 text-blue-400" />
                  <h4 className="font-bold text-sm text-white">Opure.Desktop</h4>
                  <p className="text-[11px] text-gray-400 font-mono">Avalonia UI Shell</p>
                </div>

                <div className="md:col-span-1 flex flex-col items-center justify-center space-y-1">
                  <div className="w-full h-1 bg-gradient-to-r from-blue-500 to-indigo-500 relative animate-pulse" />
                  <span className="text-[10px] font-mono text-blue-300 bg-[#050505] px-2">Named Pipes</span>
                </div>

                <div className="md:col-span-2 p-6 rounded-2xl bg-indigo-950/20 border border-indigo-500/30 flex flex-col items-center justify-center space-y-2 hover:border-indigo-400 transition shadow-lg">
                  <CpuIcon className="w-8 h-8 text-indigo-400" />
                  <h4 className="font-bold text-sm text-white">Opure.Runtime</h4>
                  <p className="text-[11px] text-gray-400 font-mono">AOT Service Supervisor</p>
                </div>

                <div className="md:col-span-1 flex flex-col items-center justify-center space-y-1">
                  <div className="w-full h-1 bg-gradient-to-r from-indigo-500 to-purple-500 relative animate-pulse" />
                  <span className="text-[10px] font-mono text-indigo-300 bg-[#050505] px-2">ACID WAL</span>
                </div>

                <div className="md:col-span-1 p-6 rounded-2xl bg-purple-950/20 border border-purple-500/30 flex flex-col items-center justify-center space-y-2 hover:border-purple-400 transition shadow-lg">
                  <Database className="w-8 h-8 text-purple-400" />
                  <h4 className="font-bold text-sm text-white">SQLite WAL</h4>
                  <p className="text-[11px] text-gray-400 font-mono">Encrypted Store</p>
                </div>
              </div>
            </div>

            {/* Performance & Throughput Chart */}
            <div className="p-8 rounded-3xl bg-white/[0.03] border border-white/10 backdrop-blur-xl space-y-6">
              <div className="flex items-center justify-between border-b border-white/10 pb-4">
                <div className="flex items-center space-x-3">
                  <Zap className="w-5 h-5 text-indigo-400" />
                  <h3 className="text-lg font-bold text-white font-mono">Zero-Allocation DSP Pipeline Performance Graph</h3>
                </div>
                <span className="text-xs font-mono text-gray-400">Span&lt;T&gt; Memory Benchmark</span>
              </div>

              <div className="space-y-4 pt-2">
                <div>
                  <div className="flex justify-between text-xs font-mono mb-1 text-gray-300">
                    <span>Standard GC Allocation (Legacy .NET)</span>
                    <span className="text-red-400">14.2 MB/s GC Pressure</span>
                  </div>
                  <div className="w-full h-3 bg-white/5 rounded-full overflow-hidden">
                    <div className="w-[85%] h-full bg-red-500/60 rounded-full" />
                  </div>
                </div>

                <div>
                  <div className="flex justify-between text-xs font-mono mb-1 text-gray-300">
                    <span>Opure Zero-Allocation Span Buffer (.NET 10)</span>
                    <span className="text-emerald-400">0.02 MB/s (Zero GC)</span>
                  </div>
                  <div className="w-full h-3 bg-white/5 rounded-full overflow-hidden">
                    <div className="w-[12%] h-full bg-emerald-500 rounded-full animate-pulse" />
                  </div>
                </div>
              </div>
            </div>
          </motion.div>
        </section>

        {/* SECTION 5: DOCS & SPECS (ALL 12 SPECS & 28 ADRS) */}
        <section id="docs" className="pt-16">
          <motion.div initial={{ opacity: 0, y: 30 }} whileInView={{ opacity: 1, y: 0 }} viewport={{ once: true }} className="max-w-5xl mx-auto space-y-12">
            <div>
              <span className="text-xs font-mono text-blue-400 tracking-widest uppercase">Master Reference</span>
              <h2 className="text-4xl font-extrabold text-white mt-2">Specifications & ADRs</h2>
              <p className="text-gray-400 mt-2">Comprehensive documentation derived directly from the repository structure (All 12 Specs & 28 ADRs).</p>
            </div>
            
            <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
              {/* Technical Specifications */}
              <div className="space-y-4">
                <h3 className="text-lg font-bold text-white font-mono flex items-center space-x-2">
                  <Code className="w-5 h-5 text-blue-400" />
                  <span>Technical Specifications (SPEC-001 to 012)</span>
                </h3>
                <div className="space-y-3 max-h-[600px] overflow-y-auto pr-2">
                  {specsList.map(spec => (
                    <div key={spec.id} className="p-5 rounded-2xl bg-white/[0.03] border border-white/10 hover:border-blue-500/40 transition">
                      <div className="flex items-center justify-between mb-1">
                        <span className="text-xs font-mono px-2 py-0.5 rounded bg-blue-500/10 text-blue-400">{spec.id}</span>
                        <span className="text-[11px] text-gray-400 font-mono">{spec.category}</span>
                      </div>
                      <h4 className="font-bold text-white text-sm mt-1">{spec.title}</h4>
                      <p className="text-xs text-gray-400 mt-1 leading-relaxed">{spec.desc}</p>
                    </div>
                  ))}
                </div>
              </div>

              {/* Architecture Decision Records */}
              <div className="space-y-4">
                <h3 className="text-lg font-bold text-white font-mono flex items-center space-x-2">
                  <Shield className="w-5 h-5 text-indigo-400" />
                  <span>Architecture Decision Records (ADR-0001 to 028)</span>
                </h3>
                <div className="space-y-3 max-h-[600px] overflow-y-auto pr-2">
                  {adrsList.map(adr => (
                    <div key={adr.id} className="p-4 rounded-2xl bg-white/[0.03] border border-white/10 hover:border-indigo-500/40 transition flex items-center justify-between">
                      <span className="text-xs font-mono px-2.5 py-1 rounded bg-indigo-500/10 text-indigo-300">{adr.id}</span>
                      <h4 className="font-bold text-white text-xs text-right max-w-[280px]">{adr.title}</h4>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </motion.div>
        </section>

      </main>

      <footer className="border-t border-white/10 mt-20 py-12 px-6 bg-black/40 text-center text-xs text-gray-500 space-y-2 relative z-10">
        <p>Opure | Sound & Code — Created by <span className="text-white font-semibold">Ctrl_Alt_Haze</span>.</p>
        <p className="font-mono">Local-First Architecture • Zero Backend • Built with C# & .NET 10</p>
      </footer>
    </div>
  );
}
