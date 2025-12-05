namespace Modulus.UI.Abstractions;

/// <summary>
/// Defines available icon kinds for the Modulus icon system.
/// Platform-agnostic enum shared across all UI implementations.
/// Icons are based on Fluent System Icons (MIT License).
/// </summary>
public enum IconKind
{
    None = 0,
    
    // Navigation
    Home,
    Settings,
    Menu,
    ChevronRight,
    ChevronDown,
    ChevronLeft,
    ChevronUp,
    ArrowLeft,
    ArrowRight,
    ArrowUp,
    ArrowDown,
    Back,
    Forward,
    History,
    
    // Files & Folders
    Folder,
    FolderOpen,
    File,
    Document,
    Image,
    Archive,
    Attachment,
    
    // Actions
    Add,
    Delete,
    Edit,
    Save,
    Copy,
    Cut,
    Paste,
    Undo,
    Redo,
    Refresh,
    Search,
    Filter,
    Sort,
    SelectAll,
    Clear,
    
    // Communication
    Mail,
    Chat,
    Notification,
    Bell,
    Comment,
    Send,
    Phone,
    Video,
    
    // User & Security
    Person,
    People,
    Lock,
    Unlock,
    Shield,
    Key,
    Password,
    Fingerprint,
    
    // Media
    Play,
    Pause,
    Stop,
    Volume,
    VolumeMute,
    SkipNext,
    SkipPrevious,
    FastForward,
    Rewind,
    Microphone,
    Camera,
    
    // Status
    Info,
    Warning,
    Error,
    Success,
    Question,
    Loading,
    Sync,
    Online,
    Offline,
    
    // Common
    Star,
    Heart,
    Bookmark,
    Pin,
    Link,
    Unlink,
    Code,
    Terminal,
    Tag,
    Flag,
    Trophy,
    Gift,
    Lightning,
    Fire,
    
    // Layout
    Grid,
    List,
    Table,
    Dashboard,
    Panel,
    Split,
    
    // View
    Eye,
    EyeOff,
    ZoomIn,
    ZoomOut,
    Fullscreen,
    ExitFullscreen,
    Expand,
    Collapse,
    
    // Window
    Minimize,
    Maximize,
    Restore,
    Window,
    
    // Development
    Bug,
    Database,
    Server,
    Api,
    Plugin,
    Extension,
    Branch,
    Merge,
    AppsAddIn,
    
    // Data & Charts
    Chart,
    PieChart,
    BarChart,
    LineChart,
    Analytics,
    
    // Hardware
    Cpu,
    Memory,
    Disk,
    Network,
    Wifi,
    WifiOff,
    Bluetooth,
    Usb,
    Battery,
    Power,
    
    // Text Formatting
    Bold,
    Italic,
    Underline,
    Strikethrough,
    TextFormat,
    AlignLeft,
    AlignCenter,
    AlignRight,
    
    // Weather
    Sun,
    Moon,
    CloudSun,
    Rain,
    Snow,
    
    // E-commerce
    Cart,
    Payment,
    Receipt,
    Wallet,
    CreditCard,
    
    // Misc
    Calendar,
    Clock,
    Timer,
    Alarm,
    Location,
    Globe,
    Cloud,
    CloudUpload,
    CloudDownload,
    Download,
    Upload,
    Share,
    Print,
    Help,
    MoreHorizontal,
    MoreVertical,
    Close,
    Check,
    Block,
    Compass,
    QrCode,
    Barcode,
    Sparkle,
    Robot
}


