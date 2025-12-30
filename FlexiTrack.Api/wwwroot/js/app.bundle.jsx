// ============================================
// Auth Context
// ============================================

const AuthContext = React.createContext(null);

function AuthProvider({ children }) {
    const [user, setUser] = React.useState(() => {
        const stored = localStorage.getItem('user');
        return stored ? JSON.parse(stored) : null;
    });
    const [profile, setProfile] = React.useState(null);

    React.useEffect(() => {
        if (user?.token) {
            fetchProfile(user.token);
        } else {
            setProfile(null);
        }
    }, [user]);

    const fetchProfile = async (token) => {
        try {
            const res = await fetch('/api/users/profile', {
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (res.ok) {
                const data = await res.json();
                setProfile(data);
            } else {
                logout();
            }
        } catch (err) {
            console.error('Failed to fetch profile');
        }
    };

    const login = (userData) => {
        localStorage.setItem('user', JSON.stringify(userData));
        setUser(userData);
    };

    const logout = () => {
        localStorage.removeItem('user');
        setUser(null);
        setProfile(null);
    };

    const value = {
        user,
        profile,
        login,
        logout,
        isAuthenticated: !!user,
        isSystemAdmin: profile?.isSystemAdmin || false,
        isCompanyAdmin: profile?.isCompanyAdmin || false,
        isAnyAdmin: profile?.isSystemAdmin || profile?.isCompanyAdmin || false
    };

    return (
        <AuthContext.Provider value={value}>
            {children}
        </AuthContext.Provider>
    );
}

function useAuth() {
    const context = React.useContext(AuthContext);
    if (!context) {
        throw new Error('useAuth must be used within an AuthProvider');
    }
    return context;
}

// ============================================
// Components
// ============================================

function FormInput({ label, type = 'text', value, onChange, required = false, placeholder = '', autoComplete = 'on' }) {
    return (
        <div className="form-group">
            <label>{label}</label>
            <input
                type={type}
                value={value}
                onChange={(e) => onChange(e.target.value)}
                required={required}
                placeholder={placeholder}
                autoComplete={autoComplete}
            />
        </div>
    );
}

function Message({ message }) {
    if (!message) return null;

    return (
        <div className={`message ${message.type}`}>
            {message.text}
        </div>
    );
}

function Layout({ children, title, showNav = false, onLogout, isAdmin = false, userName = null }) {
    const history = ReactRouterDOM.useHistory();

    return (
        <div className="layout">
            {showNav && (
                <nav className="navbar">
                    <div className="nav-brand" onClick={() => history.push('/dashboard')}>
                        FlexiTrack
                    </div>
                    <div className="nav-links">
                        {isAdmin && <a onClick={() => history.push('/admin/users')}>Users</a>}
                        {isAdmin && <a onClick={() => history.push('/admin/companies')}>Companies</a>}
                        {userName && <a onClick={() => history.push('/profile')} title="Settings">{userName}</a>}
                        <a onClick={onLogout} className="logout">Logout</a>
                    </div>
                </nav>
            )}
            <div className="container">
                {title && <h1>{title}</h1>}
                {children}
            </div>
        </div>
    );
}

function ProtectedRoute({ children, requireAdmin = false, requireSystemAdmin = false, ...rest }) {
    const { isAuthenticated, isSystemAdmin, isAnyAdmin } = useAuth();

    return (
        <ReactRouterDOM.Route
            {...rest}
            render={({ location }) => {
                if (!isAuthenticated) {
                    return <ReactRouterDOM.Redirect to={{ pathname: '/login', state: { from: location } }} />;
                }
                if (requireSystemAdmin && !isSystemAdmin) {
                    return <ReactRouterDOM.Redirect to="/dashboard" />;
                }
                if (requireAdmin && !isAnyAdmin) {
                    return <ReactRouterDOM.Redirect to="/dashboard" />;
                }
                return children;
            }}
        />
    );
}

// ============================================
// Pages
// ============================================

function LoginPage() {
    const [email, setEmail] = React.useState('');
    const [password, setPassword] = React.useState('');
    const [message, setMessage] = React.useState(null);
    const [loading, setLoading] = React.useState(false);

    const { login, isAuthenticated } = useAuth();
    const history = ReactRouterDOM.useHistory();
    const location = ReactRouterDOM.useLocation();

    React.useEffect(() => {
        if (isAuthenticated) {
            history.replace('/dashboard');
        }
    }, [isAuthenticated]);

    React.useEffect(() => {
        const state = location.state;
        if (state?.message) {
            setMessage(state.message);
        }
    }, [location]);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setMessage(null);
        setLoading(true);

        try {
            const res = await fetch('/api/users/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email, password })
            });
            const data = await res.json();

            if (data.success && data.token) {
                login({ email, token: data.token });
                history.push('/dashboard');
            } else {
                setMessage({ type: 'error', text: data.error || 'Login failed' });
            }
        } catch (err) {
            setMessage({ type: 'error', text: 'An error occurred' });
        } finally {
            setLoading(false);
        }
    };

    if (isAuthenticated) {
        return null;
    }

    return (
        <Layout title="Login">
            <Message message={message} />
            <form onSubmit={handleSubmit}>
                <FormInput
                    label="Email"
                    type="email"
                    value={email}
                    onChange={setEmail}
                    required
                />
                <FormInput
                    label="Password"
                    type="password"
                    value={password}
                    onChange={setPassword}
                    required
                />
                <button type="submit" disabled={loading}>
                    {loading ? 'Logging in...' : 'Login'}
                </button>
            </form>
            <div className="links">
                <a onClick={() => history.push('/register')}>Register</a>
                <a onClick={() => history.push('/forgot-password')}>Forgot Password?</a>
            </div>
        </Layout>
    );
}

function RegisterPage() {
    const [firstName, setFirstName] = React.useState('');
    const [lastName, setLastName] = React.useState('');
    const [email, setEmail] = React.useState('');
    const [password, setPassword] = React.useState('');
    const [confirmPassword, setConfirmPassword] = React.useState('');
    const [message, setMessage] = React.useState(null);
    const [loading, setLoading] = React.useState(false);

    const { isAuthenticated } = useAuth();
    const history = ReactRouterDOM.useHistory();

    React.useEffect(() => {
        if (isAuthenticated) {
            history.replace('/dashboard');
        }
    }, [isAuthenticated]);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setMessage(null);
        setLoading(true);

        try {
            const res = await fetch('/api/users/register', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ firstName, lastName, email, password, confirmPassword })
            });
            const data = await res.json();

            if (data.success) {
                history.push('/login', {
                    message: { type: 'success', text: 'Registration successful! Please login.' }
                });
            } else {
                setMessage({ type: 'error', text: data.errors?.join(', ') || 'Registration failed' });
            }
        } catch (err) {
            setMessage({ type: 'error', text: 'An error occurred' });
        } finally {
            setLoading(false);
        }
    };

    if (isAuthenticated) {
        return null;
    }

    return (
        <Layout title="Register">
            <Message message={message} />
            <form onSubmit={handleSubmit}>
                <FormInput
                    label="First Name"
                    value={firstName}
                    onChange={setFirstName}
                    required
                />
                <FormInput
                    label="Last Name"
                    value={lastName}
                    onChange={setLastName}
                    required
                />
                <FormInput
                    label="Email"
                    type="email"
                    value={email}
                    onChange={setEmail}
                    required
                    autoComplete="off"
                />
                <FormInput
                    label="Password"
                    type="password"
                    value={password}
                    onChange={setPassword}
                    required
                    autoComplete="new-password"
                />
                <FormInput
                    label="Confirm Password"
                    type="password"
                    value={confirmPassword}
                    onChange={setConfirmPassword}
                    required
                    autoComplete="new-password"
                />
                <button type="submit" disabled={loading}>
                    {loading ? 'Registering...' : 'Register'}
                </button>
            </form>
            <div className="links">
                <a onClick={() => history.push('/login')}>Back to Login</a>
            </div>
        </Layout>
    );
}

function ForgotPasswordPage() {
    const [email, setEmail] = React.useState('');
    const [message, setMessage] = React.useState(null);
    const [loading, setLoading] = React.useState(false);

    const { isAuthenticated } = useAuth();
    const history = ReactRouterDOM.useHistory();

    React.useEffect(() => {
        if (isAuthenticated) {
            history.replace('/dashboard');
        }
    }, [isAuthenticated]);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setMessage(null);
        setLoading(true);

        try {
            const res = await fetch('/api/users/forgot-password', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email })
            });
            const data = await res.json();
            setMessage({ type: 'success', text: data.message });
        } catch (err) {
            setMessage({ type: 'error', text: 'An error occurred' });
        } finally {
            setLoading(false);
        }
    };

    if (isAuthenticated) {
        return null;
    }

    return (
        <Layout title="Forgot Password">
            <Message message={message} />
            <form onSubmit={handleSubmit}>
                <FormInput
                    label="Email"
                    type="email"
                    value={email}
                    onChange={setEmail}
                    required
                />
                <button type="submit" disabled={loading}>
                    {loading ? 'Sending...' : 'Reset Password'}
                </button>
            </form>
            <div className="links">
                <a onClick={() => history.push('/login')}>Back to Login</a>
            </div>
        </Layout>
    );
}

function formatTimeNoSeconds(time) {
    if (!time) return '';
    return time.substring(0, 5);
}

function calculateDuration(startTime, endTime) {
    if (!startTime || !endTime) return '';
    const [startH, startM] = startTime.split(':').map(Number);
    const [endH, endM] = endTime.split(':').map(Number);
    let totalMinutes = (endH * 60 + endM) - (startH * 60 + startM);
    if (totalMinutes < 0) totalMinutes += 24 * 60;
    const hours = Math.floor(totalMinutes / 60);
    const minutes = totalMinutes % 60;
    if (hours === 0) return `${minutes}m`;
    if (minutes === 0) return `${hours}h`;
    return `${hours}h ${minutes}m`;
}

function DashboardPage() {
    const { user, profile, logout, isSystemAdmin, isCompanyAdmin } = useAuth();
    const history = ReactRouterDOM.useHistory();

    const [taskDate, setTaskDate] = React.useState(new Date().toISOString().split('T')[0]);
    const [startTime, setStartTime] = React.useState('06:00');
    const [endTime, setEndTime] = React.useState('');
    const [client, setClient] = React.useState('');
    const [taskDescription, setTaskDescription] = React.useState('');
    const [message, setMessage] = React.useState(null);
    const [logging, setLogging] = React.useState(false);
    const [taskLogs, setTaskLogs] = React.useState([]);

    // Edit state
    const [editingId, setEditingId] = React.useState(null);
    const [editDate, setEditDate] = React.useState('');
    const [editStartTime, setEditStartTime] = React.useState('');
    const [editEndTime, setEditEndTime] = React.useState('');
    const [editClient, setEditClient] = React.useState('');
    const [editDescription, setEditDescription] = React.useState('');
    const [saving, setSaving] = React.useState(false);

    // Client autocomplete state
    const [showClientSuggestions, setShowClientSuggestions] = React.useState(false);
    const [filteredClients, setFilteredClients] = React.useState([]);

    // Derive unique clients from task logs, sorted alphabetically
    const clients = React.useMemo(() => {
        return [...new Set(taskLogs.map(log => log.client).filter(c => c))].sort((a, b) => a.localeCompare(b));
    }, [taskLogs]);

    // Date filter state
    const [dateFilter, setDateFilter] = React.useState('today');
    const [clientFilter, setClientFilter] = React.useState('all');

    const getDateFilteredTaskLogs = () => {
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        const todayStr = today.toISOString().split('T')[0];

        if (dateFilter === 'today') {
            return taskLogs.filter(log => log.date === todayStr);
        } else if (dateFilter === 'week') {
            const dayOfWeek = today.getDay();
            const mondayOffset = dayOfWeek === 0 ? -6 : 1 - dayOfWeek;
            const monday = new Date(today);
            monday.setDate(today.getDate() + mondayOffset);
            const mondayStr = monday.toISOString().split('T')[0];
            return taskLogs.filter(log => log.date >= mondayStr && log.date <= todayStr);
        } else if (dateFilter === 'lastweek') {
            const dayOfWeek = today.getDay();
            const mondayOffset = dayOfWeek === 0 ? -6 : 1 - dayOfWeek;
            const thisMonday = new Date(today);
            thisMonday.setDate(today.getDate() + mondayOffset);
            const lastMonday = new Date(thisMonday);
            lastMonday.setDate(thisMonday.getDate() - 7);
            const lastSunday = new Date(thisMonday);
            lastSunday.setDate(thisMonday.getDate() - 1);
            const lastMondayStr = lastMonday.toISOString().split('T')[0];
            const lastSundayStr = lastSunday.toISOString().split('T')[0];
            return taskLogs.filter(log => log.date >= lastMondayStr && log.date <= lastSundayStr);
        } else if (dateFilter === '4weeks') {
            const fourWeeksAgo = new Date(today);
            fourWeeksAgo.setDate(today.getDate() - 28);
            const fourWeeksStr = fourWeeksAgo.toISOString().split('T')[0];
            return taskLogs.filter(log => log.date >= fourWeeksStr && log.date <= todayStr);
        }

        return taskLogs;
    };

    const dateFilteredTaskLogs = getDateFilteredTaskLogs();

    // Get distinct clients from date-filtered logs, sorted alphabetically
    const visibleClients = [...new Set(dateFilteredTaskLogs.map(log => log.client).filter(c => c))].sort((a, b) => a.localeCompare(b));

    const getFilteredTaskLogs = () => {
        let filtered = dateFilteredTaskLogs;

        // Client filtering
        if (clientFilter === 'none') {
            filtered = filtered.filter(log => !log.client);
        } else if (clientFilter !== 'all') {
            filtered = filtered.filter(log => log.client === clientFilter);
        }

        return filtered;
    };

    const filteredTaskLogs = getFilteredTaskLogs();

    const getTotalDuration = () => {
        let totalMinutes = 0;
        filteredTaskLogs.forEach(log => {
            if (log.startTime && log.endTime) {
                const [startH, startM] = log.startTime.split(':').map(Number);
                const [endH, endM] = log.endTime.split(':').map(Number);
                let mins = (endH * 60 + endM) - (startH * 60 + startM);
                if (mins < 0) mins += 24 * 60;
                totalMinutes += mins;
            }
        });
        const hours = Math.floor(totalMinutes / 60);
        const minutes = totalMinutes % 60;
        if (totalMinutes === 0) return '0h';
        if (hours === 0) return `${minutes}m`;
        if (minutes === 0) return `${hours}h`;
        return `${hours}h ${minutes}m`;
    };

    const getChartData = () => {
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        const todayStr = today.toISOString().split('T')[0];
        const dayOfWeek = today.getDay();
        const mondayOffset = dayOfWeek === 0 ? -6 : 1 - dayOfWeek;

        const days = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

        // Apply client filter to task logs for chart
        const clientFilteredLogs = clientFilter === 'all'
            ? taskLogs
            : clientFilter === 'none'
                ? taskLogs.filter(log => !log.client)
                : taskLogs.filter(log => log.client === clientFilter);

        const getWeekData = (mondayDate) => {
            const summary = [];
            for (let i = 0; i < 7; i++) {
                const date = new Date(mondayDate);
                date.setDate(mondayDate.getDate() + i);
                const dateStr = date.toISOString().split('T')[0];

                let totalMinutes = 0;
                clientFilteredLogs.filter(log => log.date === dateStr).forEach(log => {
                    if (log.startTime && log.endTime) {
                        const [startH, startM] = log.startTime.split(':').map(Number);
                        const [endH, endM] = log.endTime.split(':').map(Number);
                        let mins = (endH * 60 + endM) - (startH * 60 + startM);
                        if (mins < 0) mins += 24 * 60;
                        totalMinutes += mins;
                    }
                });

                summary.push({
                    day: days[i],
                    date: dateStr,
                    minutes: totalMinutes,
                    hours: (totalMinutes / 60).toFixed(1),
                    isToday: dateStr === todayStr
                });
            }
            return summary;
        };

        if (dateFilter === 'week' || dateFilter === 'today') {
            // This week (Mon-Sun)
            const thisMonday = new Date(today);
            thisMonday.setDate(today.getDate() + mondayOffset);
            return { title: 'This Week', data: getWeekData(thisMonday), type: 'daily' };
        } else if (dateFilter === 'lastweek') {
            // Last week (Mon-Sun)
            const thisMonday = new Date(today);
            thisMonday.setDate(today.getDate() + mondayOffset);
            const lastMonday = new Date(thisMonday);
            lastMonday.setDate(thisMonday.getDate() - 7);
            return { title: 'Last Week', data: getWeekData(lastMonday), type: 'daily' };
        } else if (dateFilter === '4weeks') {
            // Last 4 weeks - show weekly totals
            const thisMonday = new Date(today);
            thisMonday.setDate(today.getDate() + mondayOffset);
            const weeklySummary = [];

            for (let w = 3; w >= 0; w--) {
                const weekMonday = new Date(thisMonday);
                weekMonday.setDate(thisMonday.getDate() - (w * 7));
                const weekSunday = new Date(weekMonday);
                weekSunday.setDate(weekMonday.getDate() + 6);

                const weekMondayStr = weekMonday.toISOString().split('T')[0];
                const weekSundayStr = weekSunday.toISOString().split('T')[0];

                let totalMinutes = 0;
                clientFilteredLogs.filter(log => log.date >= weekMondayStr && log.date <= weekSundayStr).forEach(log => {
                    if (log.startTime && log.endTime) {
                        const [startH, startM] = log.startTime.split(':').map(Number);
                        const [endH, endM] = log.endTime.split(':').map(Number);
                        let mins = (endH * 60 + endM) - (startH * 60 + startM);
                        if (mins < 0) mins += 24 * 60;
                        totalMinutes += mins;
                    }
                });

                const isCurrentWeek = weekMondayStr <= todayStr && weekSundayStr >= todayStr;
                const weekLabel = `${weekMonday.getDate()}/${weekMonday.getMonth() + 1}`;

                weeklySummary.push({
                    day: weekLabel,
                    date: weekMondayStr,
                    minutes: totalMinutes,
                    hours: (totalMinutes / 60).toFixed(1),
                    isToday: isCurrentWeek
                });
            }

            return { title: 'Last 4 Weeks', data: weeklySummary, type: 'weekly' };
        }

        // Default to this week
        const thisMonday = new Date(today);
        thisMonday.setDate(today.getDate() + mondayOffset);
        return { title: 'This Week', data: getWeekData(thisMonday), type: 'daily' };
    };

    const chartData = getChartData();
    const maxHours = Math.max(...chartData.data.map(d => d.minutes / 60), 8);

    const fetchTaskLogs = async () => {
        if (!user?.token) return;
        try {
            const res = await fetch('/api/tasks/logs', {
                headers: { 'Authorization': `Bearer ${user.token}` }
            });
            if (res.ok) {
                const data = await res.json();
                setTaskLogs(data.taskLogs || []);
            }
        } catch (err) {
            console.error('Failed to fetch task logs:', err);
        }
    };

    React.useEffect(() => {
        fetchTaskLogs();
    }, [user?.token]);

    // Auto-set start time based on last task for selected date or user's default
    React.useEffect(() => {
        const tasksOnDate = taskLogs
            .filter(log => log.date === taskDate)
            .sort((a, b) => a.endTime.localeCompare(b.endTime));

        if (tasksOnDate.length > 0) {
            const lastTask = tasksOnDate[tasksOnDate.length - 1];
            const lastEndTime = lastTask.endTime.substring(0, 5);
            setStartTime(lastEndTime);
        } else {
            // Use user's default start time or fall back to 06:00
            setStartTime(profile?.defaultStartTime || '06:00');
        }
    }, [taskDate, taskLogs, profile?.defaultStartTime]);

    const handleClientChange = (value) => {
        setClient(value);
        if (value.length > 0) {
            const lowerValue = value.toLowerCase();
            const filtered = clients.filter(c =>
                c.toLowerCase().includes(lowerValue)
            );
            // Sort to show "starts with" matches first
            filtered.sort((a, b) => {
                const aStarts = a.toLowerCase().startsWith(lowerValue);
                const bStarts = b.toLowerCase().startsWith(lowerValue);
                if (aStarts && !bStarts) return -1;
                if (!aStarts && bStarts) return 1;
                return a.localeCompare(b);
            });
            setFilteredClients(filtered);
            setShowClientSuggestions(filtered.length > 0);
        } else {
            setFilteredClients(clients);
            setShowClientSuggestions(clients.length > 0);
        }
    };

    const selectClient = (selectedClient) => {
        setClient(selectedClient);
        setShowClientSuggestions(false);
    };

    const timeToMinutes = (time) => {
        const [h, m] = time.split(':').map(Number);
        return h * 60 + m;
    };

    const checkTimeOverlap = (date, start, end, excludeId = null) => {
        const startMins = timeToMinutes(start);
        const endMins = timeToMinutes(end);

        const logsOnDate = taskLogs.filter(log =>
            log.date === date && (excludeId === null || log.id !== excludeId)
        );

        for (const log of logsOnDate) {
            const logStart = timeToMinutes(log.startTime);
            const logEnd = timeToMinutes(log.endTime);

            // Check for overlap: new range overlaps if it starts before existing ends AND ends after existing starts
            if (startMins < logEnd && endMins > logStart) {
                return `Time overlaps with existing task: ${log.startTime.substring(0,5)} - ${log.endTime.substring(0,5)}`;
            }
        }
        return null;
    };

    const handleLogTask = async (e) => {
        e.preventDefault();
        setMessage(null);

        // Validate date is not in the future
        const today = new Date().toISOString().split('T')[0];
        if (taskDate > today) {
            setMessage({ type: 'error', text: 'Date cannot be in the future' });
            return;
        }

        // Validate end time is after start time
        if (timeToMinutes(endTime) <= timeToMinutes(startTime)) {
            setMessage({ type: 'error', text: 'End time must be after start time' });
            return;
        }

        // Check for overlapping times
        const overlapError = checkTimeOverlap(taskDate, startTime, endTime);
        if (overlapError) {
            setMessage({ type: 'error', text: overlapError });
            return;
        }

        setLogging(true);

        try {
            const res = await fetch('/api/tasks', {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${user.token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    date: taskDate,
                    startTime,
                    endTime,
                    client: client || null,
                    description: taskDescription
                })
            });

            if (res.ok) {
                setMessage({ type: 'success', text: 'Task logged successfully' });
                setTaskDescription('');
                setClient('');
                fetchTaskLogs();
            } else {
                const data = await res.json();
                setMessage({ type: 'error', text: data.error || 'Failed to log task' });
            }
        } catch (err) {
            setMessage({ type: 'error', text: 'An error occurred' });
        } finally {
            setLogging(false);
        }
    };

    const startEdit = (log) => {
        setEditingId(log.id);
        setEditDate(log.date);
        setEditStartTime(log.startTime);
        setEditEndTime(log.endTime);
        setEditClient(log.client || '');
        setEditDescription(log.description);
    };

    const cancelEdit = () => {
        setEditingId(null);
        setEditDate('');
        setEditStartTime('');
        setEditEndTime('');
        setEditClient('');
        setEditDescription('');
    };

    const handleSaveEdit = async () => {
        setMessage(null);

        // Validate date is not in the future
        const today = new Date().toISOString().split('T')[0];
        if (editDate > today) {
            setMessage({ type: 'error', text: 'Date cannot be in the future' });
            return;
        }

        // Validate end time is after start time
        if (timeToMinutes(editEndTime) <= timeToMinutes(editStartTime)) {
            setMessage({ type: 'error', text: 'End time must be after start time' });
            return;
        }

        // Check for overlapping times (exclude current task being edited)
        const overlapError = checkTimeOverlap(editDate, editStartTime, editEndTime, editingId);
        if (overlapError) {
            setMessage({ type: 'error', text: overlapError });
            return;
        }

        setSaving(true);

        try {
            const res = await fetch(`/api/tasks/${editingId}`, {
                method: 'PUT',
                headers: {
                    'Authorization': `Bearer ${user.token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    date: editDate,
                    startTime: editStartTime,
                    endTime: editEndTime,
                    client: editClient || null,
                    description: editDescription
                })
            });

            if (res.ok) {
                setMessage({ type: 'success', text: 'Task updated successfully' });
                cancelEdit();
                fetchTaskLogs();
            } else {
                const data = await res.json();
                setMessage({ type: 'error', text: data.error || 'Failed to update task' });
            }
        } catch (err) {
            setMessage({ type: 'error', text: 'An error occurred' });
        } finally {
            setSaving(false);
        }
    };

    const handleDelete = async (id) => {
        if (!confirm('Are you sure you want to delete this task log?')) return;

        setMessage(null);
        try {
            const res = await fetch(`/api/tasks/${id}`, {
                method: 'DELETE',
                headers: {
                    'Authorization': `Bearer ${user.token}`
                }
            });

            if (res.ok) {
                setMessage({ type: 'success', text: 'Task deleted successfully' });
                fetchTaskLogs();
            } else {
                const data = await res.json();
                setMessage({ type: 'error', text: data.error || 'Failed to delete task' });
            }
        } catch (err) {
            setMessage({ type: 'error', text: 'An error occurred' });
        }
    };

    const exportToCsv = () => {
        if (filteredTaskLogs.length === 0) {
            setMessage({ type: 'error', text: 'No task logs to export' });
            return;
        }

        const headers = ['Date', 'Start Time', 'End Time', 'Duration', 'Client', 'Description'];
        const rows = filteredTaskLogs.map(log => {
            const duration = calculateDuration(log.startTime, log.endTime);
            return [
                log.date,
                formatTimeNoSeconds(log.startTime),
                formatTimeNoSeconds(log.endTime),
                duration,
                log.client || '',
                `"${(log.description || '').replace(/"/g, '""')}"`
            ];
        });

        const csvContent = [
            headers.join(','),
            ...rows.map(row => row.join(','))
        ].join('\n');

        const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
        const link = document.createElement('a');
        const url = URL.createObjectURL(blob);
        link.setAttribute('href', url);
        link.setAttribute('download', `task-logs-${taskDate}.csv`);
        link.style.visibility = 'hidden';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    };

    if (!profile) {
        return (
            <Layout title="Dashboard" showNav onLogout={logout}>
                <p>Loading...</p>
            </Layout>
        );
    }

    return (
        <Layout
            showNav
            onLogout={logout}
            isAdmin={isSystemAdmin}
            userName={profile.firstName}
        >
            <div className="dashboard">

                <div className="card">
                    <h2>Log Task</h2>
                    <Message message={message} />
                    <form onSubmit={handleLogTask}>
                        <div className="form-group">
                            <label>Date</label>
                            <input
                                type="date"
                                value={taskDate}
                                onChange={(e) => setTaskDate(e.target.value)}
                                max={new Date().toISOString().split('T')[0]}
                                required
                            />
                        </div>
                        <div className="form-group">
                            <label>Start Time</label>
                            <input
                                type="time"
                                value={startTime}
                                onChange={(e) => setStartTime(e.target.value)}
                                required
                            />
                        </div>
                        <div className="form-group">
                            <label>End Time</label>
                            <input
                                type="time"
                                value={endTime}
                                onChange={(e) => setEndTime(e.target.value)}
                                required
                            />
                        </div>
                        <div className="form-group" style={{ position: 'relative' }}>
                            <label>Client</label>
                            <input
                                type="text"
                                value={client}
                                onChange={(e) => handleClientChange(e.target.value)}
                                onFocus={() => {
                                    if (clients.length > 0) {
                                        if (client) {
                                            const lowerValue = client.toLowerCase();
                                            const filtered = clients.filter(c => c.toLowerCase().includes(lowerValue));
                                            filtered.sort((a, b) => {
                                                const aStarts = a.toLowerCase().startsWith(lowerValue);
                                                const bStarts = b.toLowerCase().startsWith(lowerValue);
                                                if (aStarts && !bStarts) return -1;
                                                if (!aStarts && bStarts) return 1;
                                                return a.localeCompare(b);
                                            });
                                            setFilteredClients(filtered);
                                        } else {
                                            setFilteredClients(clients);
                                        }
                                        setShowClientSuggestions(true);
                                    }
                                }}
                                onBlur={() => setTimeout(() => setShowClientSuggestions(false), 150)}
                                placeholder="Client name (optional)"
                                autoComplete="off"
                            />
                            {showClientSuggestions && filteredClients.length > 0 && (
                                <div style={{
                                    position: 'absolute',
                                    top: '100%',
                                    left: 0,
                                    right: 0,
                                    background: 'white',
                                    border: '1px solid #ddd',
                                    borderRadius: '4px',
                                    maxHeight: '150px',
                                    overflowY: 'auto',
                                    zIndex: 1000,
                                    boxShadow: '0 2px 4px rgba(0,0,0,0.1)'
                                }}>
                                    {filteredClients.map((c, idx) => (
                                        <div
                                            key={idx}
                                            onClick={() => selectClient(c)}
                                            style={{
                                                padding: '0.5rem 0.75rem',
                                                cursor: 'pointer',
                                                borderBottom: idx < filteredClients.length - 1 ? '1px solid #eee' : 'none'
                                            }}
                                            onMouseEnter={(e) => e.target.style.background = '#f0f0f0'}
                                            onMouseLeave={(e) => e.target.style.background = 'white'}
                                        >
                                            {c}
                                        </div>
                                    ))}
                                </div>
                            )}
                        </div>
                        <div className="form-group">
                            <label>Description</label>
                            <input
                                type="text"
                                value={taskDescription}
                                onChange={(e) => setTaskDescription(e.target.value)}
                                placeholder="What did you work on?"
                                required
                            />
                        </div>
                        <button type="submit" disabled={logging}>
                            {logging ? 'Logging...' : 'Log Task'}
                        </button>
                    </form>
                </div>

                <div className="card">
                    <h2>{chartData.title}</h2>
                    {(() => {
                        const weeklyTarget = profile?.weeklyHoursTarget;
                        const targetHours = weeklyTarget ? (chartData.type === 'weekly' ? weeklyTarget : weeklyTarget / 7) : null;
                        const targetLinePosition = targetHours ? Math.min((targetHours / maxHours) * 120, 120) : null;

                        return (
                            <div style={{ position: 'relative', height: '150px', padding: '0 0.5rem' }}>
                                {targetLinePosition && (
                                    <div style={{
                                        position: 'absolute',
                                        bottom: `${targetLinePosition + 30}px`,
                                        left: 0,
                                        right: 0,
                                        borderTop: '2px dashed #dc3545',
                                        zIndex: 10
                                    }}>
                                        <span style={{
                                            position: 'absolute',
                                            right: 0,
                                            top: '-18px',
                                            fontSize: '0.7rem',
                                            color: '#dc3545',
                                            background: '#f8f9fa',
                                            padding: '0 4px'
                                        }}>
                                            {targetHours.toFixed(1)}h target
                                        </span>
                                    </div>
                                )}
                                <div style={{ display: 'flex', alignItems: 'flex-end', justifyContent: 'space-between', height: '100%', gap: '0.5rem' }}>
                                    {chartData.data.map((day, idx) => (
                                        <div key={idx} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', flex: 1 }}>
                                            <div style={{
                                                width: '100%',
                                                maxWidth: chartData.type === 'weekly' ? '80px' : '50px',
                                                height: `${Math.max((day.minutes / 60 / maxHours) * 120, day.minutes > 0 ? 4 : 0)}px`,
                                                background: day.isToday ? '#007bff' : '#28a745',
                                                borderRadius: '4px 4px 0 0',
                                                transition: 'height 0.3s'
                                            }} title={`${day.hours}h`}></div>
                                            <div style={{ fontSize: '0.75rem', marginTop: '0.25rem', fontWeight: day.isToday ? 'bold' : 'normal', color: day.isToday ? '#007bff' : '#666' }}>
                                                {day.day}
                                            </div>
                                            <div style={{ fontSize: '0.7rem', color: '#999' }}>
                                                {day.hours}h
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            </div>
                        );
                    })()}
                    <div style={{ textAlign: 'center', marginTop: '0.75rem', color: '#666', fontSize: '0.9rem' }}>
                        Total: <strong>{(chartData.data.reduce((sum, d) => sum + d.minutes, 0) / 60).toFixed(1)}h</strong>
                        {profile?.weeklyHoursTarget && (
                            <span style={{ marginLeft: '1rem', color: '#dc3545' }}>
                                Target: <strong>{profile.weeklyHoursTarget}h/week</strong>
                            </span>
                        )}
                    </div>
                </div>

                <div className="card">
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem', flexWrap: 'wrap', gap: '0.5rem' }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
                            <h2 style={{ margin: 0 }}>Task Logs</h2>
                            <span style={{ color: '#666', fontSize: '0.95rem' }}>
                                Total: <strong>{getTotalDuration()}</strong>
                            </span>
                        </div>
                        <div style={{ display: 'flex', gap: '0.5rem' }}>
                            <button
                                className={dateFilter === 'today' ? 'btn-small' : 'btn-small btn-outline'}
                                onClick={() => setDateFilter('today')}
                                style={dateFilter !== 'today' ? { background: 'white', color: '#007bff', border: '1px solid #007bff' } : {}}
                            >
                                Today
                            </button>
                            <button
                                className={dateFilter === 'week' ? 'btn-small' : 'btn-small btn-outline'}
                                onClick={() => setDateFilter('week')}
                                style={dateFilter !== 'week' ? { background: 'white', color: '#007bff', border: '1px solid #007bff' } : {}}
                            >
                                This Week
                            </button>
                            <button
                                className={dateFilter === 'lastweek' ? 'btn-small' : 'btn-small btn-outline'}
                                onClick={() => setDateFilter('lastweek')}
                                style={dateFilter !== 'lastweek' ? { background: 'white', color: '#007bff', border: '1px solid #007bff' } : {}}
                            >
                                Last Week
                            </button>
                            <button
                                className={dateFilter === '4weeks' ? 'btn-small' : 'btn-small btn-outline'}
                                onClick={() => setDateFilter('4weeks')}
                                style={dateFilter !== '4weeks' ? { background: 'white', color: '#007bff', border: '1px solid #007bff' } : {}}
                            >
                                Last 4 Weeks
                            </button>
                            <select
                                value={clientFilter}
                                onChange={(e) => setClientFilter(e.target.value)}
                                style={{ padding: '0.4rem 0.8rem', fontSize: '0.85rem', borderRadius: '4px', border: '1px solid #007bff', color: '#007bff', background: 'white', cursor: 'pointer' }}
                            >
                                <option value="all">All Clients</option>
                                <option value="none">No Client</option>
                                {visibleClients.map((c, idx) => (
                                    <option key={idx} value={c}>{c}</option>
                                ))}
                            </select>
                            <button
                                className="btn-small"
                                onClick={exportToCsv}
                                style={{ background: '#28a745' }}
                                title="Export to CSV"
                            >
                                Export CSV
                            </button>
                        </div>
                    </div>
                    {filteredTaskLogs.length === 0 ? (
                        <p>No task logs for this period.</p>
                    ) : (
                        <table className="data-table">
                            <thead>
                                <tr>
                                    <th>Date</th>
                                    <th>Time</th>
                                    <th>Duration</th>
                                    <th>Client</th>
                                    <th>Description</th>
                                    <th>Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                {filteredTaskLogs.map((log) => (
                                    editingId === log.id ? (
                                        <tr key={log.id}>
                                            <td>
                                                <input
                                                    type="date"
                                                    value={editDate}
                                                    onChange={(e) => setEditDate(e.target.value)}
                                                    max={new Date().toISOString().split('T')[0]}
                                                    style={{ width: '130px' }}
                                                />
                                            </td>
                                            <td>
                                                <input
                                                    type="time"
                                                    value={editStartTime}
                                                    onChange={(e) => setEditStartTime(e.target.value)}
                                                    style={{ width: '90px' }}
                                                />
                                                {' - '}
                                                <input
                                                    type="time"
                                                    value={editEndTime}
                                                    onChange={(e) => setEditEndTime(e.target.value)}
                                                    style={{ width: '90px' }}
                                                />
                                            </td>
                                            <td>{calculateDuration(editStartTime, editEndTime)}</td>
                                            <td>
                                                <input
                                                    type="text"
                                                    value={editClient}
                                                    onChange={(e) => setEditClient(e.target.value)}
                                                    placeholder="Client"
                                                    style={{ width: '100px' }}
                                                />
                                            </td>
                                            <td>
                                                <input
                                                    type="text"
                                                    value={editDescription}
                                                    onChange={(e) => setEditDescription(e.target.value)}
                                                    placeholder="Description"
                                                    style={{ width: '100%' }}
                                                />
                                            </td>
                                            <td>
                                                <button
                                                    className="btn-small"
                                                    onClick={handleSaveEdit}
                                                    disabled={saving}
                                                >
                                                    {saving ? 'Saving...' : 'Save'}
                                                </button>
                                                <button
                                                    className="btn-small"
                                                    onClick={cancelEdit}
                                                    disabled={saving}
                                                >
                                                    Cancel
                                                </button>
                                            </td>
                                        </tr>
                                    ) : (
                                        <tr key={log.id}>
                                            <td>{log.date}</td>
                                            <td>{formatTimeNoSeconds(log.startTime)} - {formatTimeNoSeconds(log.endTime)}</td>
                                            <td>{calculateDuration(log.startTime, log.endTime)}</td>
                                            <td>{log.client || '-'}</td>
                                            <td>{log.description}</td>
                                            <td>
                                                <span
                                                    onClick={() => startEdit(log)}
                                                    title="Edit"
                                                    style={{ cursor: 'pointer', marginRight: '0.5rem', fontSize: '1.1rem' }}
                                                >
                                                    ✏️
                                                </span>
                                                <span
                                                    onClick={() => handleDelete(log.id)}
                                                    title="Delete"
                                                    style={{ cursor: 'pointer', fontSize: '1.1rem' }}
                                                >
                                                    🗑️
                                                </span>
                                            </td>
                                        </tr>
                                    )
                                ))}
                            </tbody>
                        </table>
                    )}
                </div>

                {isCompanyAdmin && !isSystemAdmin && (
                    <div className="card">
                        <h2>Company Administration</h2>
                        <p>You are a company administrator.</p>
                        <button onClick={() => history.push(`/admin/companies/${profile.companyId}`)}>
                            Manage Company Users
                        </button>
                    </div>
                )}
            </div>
        </Layout>
    );
}

function ProfilePage() {
    const { user, profile, logout, isSystemAdmin } = useAuth();
    const [weeklyTarget, setWeeklyTarget] = React.useState('');
    const [defaultStartTime, setDefaultStartTime] = React.useState('');
    const [saving, setSaving] = React.useState(false);
    const [message, setMessage] = React.useState(null);

    React.useEffect(() => {
        if (profile?.weeklyHoursTarget != null) {
            setWeeklyTarget(profile.weeklyHoursTarget.toString());
        }
        if (profile?.defaultStartTime) {
            setDefaultStartTime(profile.defaultStartTime);
        }
    }, [profile]);

    const handleSaveSettings = async (e) => {
        e.preventDefault();
        setMessage(null);
        setSaving(true);

        try {
            const targetValue = weeklyTarget.trim() === '' ? null : parseFloat(weeklyTarget);

            if (targetValue !== null && (isNaN(targetValue) || targetValue < 0 || targetValue > 168)) {
                setMessage({ type: 'error', text: 'Please enter a valid number between 0 and 168' });
                setSaving(false);
                return;
            }

            const res = await fetch('/api/users/settings', {
                method: 'PUT',
                headers: {
                    'Authorization': `Bearer ${user.token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    weeklyHoursTarget: targetValue,
                    defaultStartTime: defaultStartTime || null
                })
            });

            if (res.ok) {
                setMessage({ type: 'success', text: 'Settings saved' });
                // Refresh the page to update profile
                window.location.reload();
            } else {
                const data = await res.json();
                setMessage({ type: 'error', text: data.error || 'Failed to save settings' });
            }
        } catch (err) {
            setMessage({ type: 'error', text: 'An error occurred' });
        } finally {
            setSaving(false);
        }
    };

    if (!profile) {
        return (
            <Layout title="Settings" showNav onLogout={logout}>
                <p>Loading...</p>
            </Layout>
        );
    }

    return (
        <Layout title="Settings" showNav onLogout={logout} isAdmin={isSystemAdmin} userName={profile.firstName}>
            <div className="profile-details">
                <div className="card">
                    <h2>Preferences</h2>
                    <Message message={message} />
                    <form onSubmit={handleSaveSettings}>
                        <div className="form-group">
                            <label>Weekly hours target</label>
                            <input
                                type="number"
                                value={weeklyTarget}
                                onChange={(e) => setWeeklyTarget(e.target.value)}
                                placeholder="e.g. 40"
                                min="0"
                                max="168"
                                step="0.5"
                                style={{ maxWidth: '150px' }}
                            />
                        </div>
                        <div className="form-group">
                            <label>Default start time (when no previous task)</label>
                            <input
                                type="time"
                                value={defaultStartTime}
                                onChange={(e) => setDefaultStartTime(e.target.value)}
                                style={{ maxWidth: '150px' }}
                            />
                        </div>
                        <button type="submit" disabled={saving}>
                            {saving ? 'Saving...' : 'Save Settings'}
                        </button>
                    </form>
                </div>

                <div className="card">
                    <h2>Account Information</h2>
                    <table className="info-table">
                        <tbody>
                            <tr>
                                <td><strong>Email</strong></td>
                                <td>{profile.email}</td>
                            </tr>
                            <tr>
                                <td><strong>First Name</strong></td>
                                <td>{profile.firstName}</td>
                            </tr>
                            <tr>
                                <td><strong>Last Name</strong></td>
                                <td>{profile.lastName}</td>
                            </tr>
                            <tr>
                                <td><strong>Account Created</strong></td>
                                <td>{new Date(profile.created).toLocaleDateString()}</td>
                            </tr>
                            <tr>
                                <td><strong>Login Count</strong></td>
                                <td>{profile.loginCount}</td>
                            </tr>
                            <tr>
                                <td><strong>Last Login</strong></td>
                                <td>{profile.lastLogin ? new Date(profile.lastLogin).toLocaleString() : 'N/A'}</td>
                            </tr>
                            {profile.companyName && (
                                <tr>
                                    <td><strong>Company</strong></td>
                                    <td>{profile.companyName}</td>
                                </tr>
                            )}
                            {profile.isCompanyAdmin && (
                                <tr>
                                    <td><strong>Company Admin</strong></td>
                                    <td>Yes</td>
                                </tr>
                            )}
                            {profile.isSystemAdmin && (
                                <tr>
                                    <td><strong>System Admin</strong></td>
                                    <td>Yes</td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>
            </div>
        </Layout>
    );
}

// ============================================
// Admin Pages
// ============================================

function UsersPage() {
    const [users, setUsers] = React.useState([]);
    const [companies, setCompanies] = React.useState([]);
    const [loading, setLoading] = React.useState(true);
    const [message, setMessage] = React.useState(null);
    const [assigningUser, setAssigningUser] = React.useState(null);
    const [selectedCompanyId, setSelectedCompanyId] = React.useState('');

    const { user, profile, logout, isSystemAdmin } = useAuth();
    const history = ReactRouterDOM.useHistory();

    React.useEffect(() => {
        fetchUsers();
        fetchCompanies();
    }, []);

    const fetchUsers = async () => {
        try {
            const res = await fetch('/api/users', {
                headers: { 'Authorization': `Bearer ${user.token}` }
            });
            if (res.ok) {
                const data = await res.json();
                setUsers(data.users);
            } else if (res.status === 403) {
                history.push('/dashboard');
            }
        } catch (err) {
            setMessage({ type: 'error', text: 'Failed to load users' });
        } finally {
            setLoading(false);
        }
    };

    const fetchCompanies = async () => {
        try {
            const res = await fetch('/api/companies', {
                headers: { 'Authorization': `Bearer ${user.token}` }
            });
            if (res.ok) {
                const data = await res.json();
                setCompanies(data.companies);
            }
        } catch (err) {
            console.error('Failed to load companies');
        }
    };

    const assignUserToCompany = async (userId) => {
        if (!selectedCompanyId) {
            setMessage({ type: 'error', text: 'Please select a company' });
            return;
        }
        try {
            const res = await fetch(`/api/companies/${selectedCompanyId}/users`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${user.token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ userId })
            });
            if (res.ok) {
                setAssigningUser(null);
                setSelectedCompanyId('');
                fetchUsers();
                setMessage({ type: 'success', text: 'User assigned to company' });
            } else {
                const data = await res.json();
                setMessage({ type: 'error', text: data.error || 'Failed to assign user' });
            }
        } catch (err) {
            setMessage({ type: 'error', text: 'An error occurred' });
        }
    };

    const removeUserFromCompany = async (userId, companyId) => {
        if (!confirm('Remove this user from their company?')) return;
        try {
            const res = await fetch(`/api/companies/${companyId}/users/${userId}`, {
                method: 'DELETE',
                headers: { 'Authorization': `Bearer ${user.token}` }
            });
            if (res.ok) {
                fetchUsers();
                setMessage({ type: 'success', text: 'User removed from company' });
            } else {
                setMessage({ type: 'error', text: 'Failed to remove user from company' });
            }
        } catch (err) {
            setMessage({ type: 'error', text: 'An error occurred' });
        }
    };

    const toggleSystemAdmin = async (userId, currentStatus) => {
        try {
            const res = await fetch(`/api/users/${userId}/system-admin`, {
                method: 'PUT',
                headers: {
                    'Authorization': `Bearer ${user.token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ isAdmin: !currentStatus })
            });
            if (res.ok) {
                fetchUsers();
                setMessage({ type: 'success', text: 'User updated successfully' });
            } else {
                setMessage({ type: 'error', text: 'Failed to update user' });
            }
        } catch (err) {
            setMessage({ type: 'error', text: 'An error occurred' });
        }
    };

    return (
        <Layout title="Manage Users" showNav onLogout={logout} isAdmin={isSystemAdmin} userName={profile?.firstName}>
            <Message message={message} />
            {loading ? (
                <p>Loading users...</p>
            ) : (
                <div className="table-container">
                    <table className="data-table">
                        <thead>
                            <tr>
                                <th>Name</th>
                                <th>Email</th>
                                <th>Company</th>
                                <th>System Admin</th>
                                <th>Company Admin</th>
                                <th>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            {users.map((u) => (
                                <tr key={u.id}>
                                    <td>{u.firstName} {u.lastName}</td>
                                    <td>{u.email}</td>
                                    <td>{u.companyName || '-'}</td>
                                    <td>{u.isSystemAdmin ? 'Yes' : 'No'}</td>
                                    <td>{u.isCompanyAdmin ? 'Yes' : 'No'}</td>
                                    <td>
                                        <button
                                            className="btn-small"
                                            onClick={() => toggleSystemAdmin(u.id, u.isSystemAdmin)}
                                        >
                                            {u.isSystemAdmin ? 'Remove Admin' : 'Make Admin'}
                                        </button>
                                        {!u.companyId ? (
                                            assigningUser === u.id ? (
                                                <>
                                                    <select
                                                        value={selectedCompanyId}
                                                        onChange={(e) => setSelectedCompanyId(e.target.value)}
                                                        style={{ marginLeft: '5px', padding: '4px' }}
                                                    >
                                                        <option value="">Select Company</option>
                                                        {companies.map((c) => (
                                                            <option key={c.id} value={c.id}>{c.name}</option>
                                                        ))}
                                                    </select>
                                                    <button
                                                        className="btn-small"
                                                        onClick={() => assignUserToCompany(u.id)}
                                                    >
                                                        Assign
                                                    </button>
                                                    <button
                                                        className="btn-small"
                                                        onClick={() => { setAssigningUser(null); setSelectedCompanyId(''); }}
                                                    >
                                                        Cancel
                                                    </button>
                                                </>
                                            ) : (
                                                <button
                                                    className="btn-small"
                                                    onClick={() => setAssigningUser(u.id)}
                                                >
                                                    Assign Company
                                                </button>
                                            )
                                        ) : (
                                            <button
                                                className="btn-small btn-danger"
                                                onClick={() => removeUserFromCompany(u.id, u.companyId)}
                                            >
                                                Remove Company
                                            </button>
                                        )}
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}
        </Layout>
    );
}

function CompaniesPage() {
    const [companies, setCompanies] = React.useState([]);
    const [loading, setLoading] = React.useState(true);
    const [message, setMessage] = React.useState(null);
    const [showForm, setShowForm] = React.useState(false);
    const [newCompanyName, setNewCompanyName] = React.useState('');

    const { user, profile, logout, isSystemAdmin } = useAuth();
    const history = ReactRouterDOM.useHistory();

    React.useEffect(() => {
        fetchCompanies();
    }, []);

    const fetchCompanies = async () => {
        try {
            const res = await fetch('/api/companies', {
                headers: { 'Authorization': `Bearer ${user.token}` }
            });
            if (res.ok) {
                const data = await res.json();
                setCompanies(data.companies);
            } else if (res.status === 403) {
                history.push('/dashboard');
            }
        } catch (err) {
            setMessage({ type: 'error', text: 'Failed to load companies' });
        } finally {
            setLoading(false);
        }
    };

    const createCompany = async (e) => {
        e.preventDefault();
        try {
            const res = await fetch('/api/companies', {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${user.token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ name: newCompanyName })
            });
            if (res.ok) {
                setNewCompanyName('');
                setShowForm(false);
                fetchCompanies();
                setMessage({ type: 'success', text: 'Company created successfully' });
            } else {
                const data = await res.json();
                setMessage({ type: 'error', text: data.error || 'Failed to create company' });
            }
        } catch (err) {
            setMessage({ type: 'error', text: 'An error occurred' });
        }
    };

    const deleteCompany = async (id) => {
        if (!confirm('Are you sure you want to delete this company?')) return;

        try {
            const res = await fetch(`/api/companies/${id}`, {
                method: 'DELETE',
                headers: { 'Authorization': `Bearer ${user.token}` }
            });
            if (res.ok) {
                fetchCompanies();
                setMessage({ type: 'success', text: 'Company deleted successfully' });
            } else {
                setMessage({ type: 'error', text: 'Failed to delete company' });
            }
        } catch (err) {
            setMessage({ type: 'error', text: 'An error occurred' });
        }
    };

    return (
        <Layout title="Manage Companies" showNav onLogout={logout} isAdmin={isSystemAdmin} userName={profile?.firstName}>
            <Message message={message} />

            <div className="actions-bar">
                <button onClick={() => setShowForm(!showForm)}>
                    {showForm ? 'Cancel' : 'Add Company'}
                </button>
            </div>

            {showForm && (
                <form onSubmit={createCompany} className="inline-form">
                    <FormInput
                        label="Company Name"
                        value={newCompanyName}
                        onChange={setNewCompanyName}
                        required
                    />
                    <button type="submit">Create Company</button>
                </form>
            )}

            {loading ? (
                <p>Loading companies...</p>
            ) : (
                <div className="table-container">
                    <table className="data-table">
                        <thead>
                            <tr>
                                <th>ID</th>
                                <th>Name</th>
                                <th>Users</th>
                                <th>Created</th>
                                <th>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            {companies.map((c) => (
                                <tr key={c.id}>
                                    <td>{c.id}</td>
                                    <td>{c.name}</td>
                                    <td>{c.userCount}</td>
                                    <td>{new Date(c.created).toLocaleDateString()}</td>
                                    <td>
                                        <button
                                            className="btn-small"
                                            onClick={() => history.push(`/admin/companies/${c.id}`)}
                                        >
                                            Manage
                                        </button>
                                        <button
                                            className="btn-small btn-danger"
                                            onClick={() => deleteCompany(c.id)}
                                        >
                                            Delete
                                        </button>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}
        </Layout>
    );
}

function CompanyDetailPage() {
    const [company, setCompany] = React.useState(null);
    const [allUsers, setAllUsers] = React.useState([]);
    const [loading, setLoading] = React.useState(true);
    const [message, setMessage] = React.useState(null);
    const [showAddUser, setShowAddUser] = React.useState(false);
    const [selectedUserId, setSelectedUserId] = React.useState('');

    const { user, profile, logout, isSystemAdmin } = useAuth();
    const history = ReactRouterDOM.useHistory();
    const { id } = ReactRouterDOM.useParams();

    React.useEffect(() => {
        fetchCompany();
        if (isSystemAdmin) {
            fetchAllUsers();
        }
    }, [id]);

    const fetchCompany = async () => {
        try {
            const res = await fetch(`/api/companies/${id}`, {
                headers: { 'Authorization': `Bearer ${user.token}` }
            });
            if (res.ok) {
                const data = await res.json();
                setCompany(data);
            } else if (res.status === 403) {
                history.push('/dashboard');
            }
        } catch (err) {
            setMessage({ type: 'error', text: 'Failed to load company' });
        } finally {
            setLoading(false);
        }
    };

    const fetchAllUsers = async () => {
        try {
            const res = await fetch('/api/users', {
                headers: { 'Authorization': `Bearer ${user.token}` }
            });
            if (res.ok) {
                const data = await res.json();
                setAllUsers(data.users.filter((u) => !u.companyId));
            }
        } catch (err) {
            console.error('Failed to load users');
        }
    };

    const addUserToCompany = async (e) => {
        e.preventDefault();
        try {
            const res = await fetch(`/api/companies/${id}/users`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${user.token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ userId: selectedUserId })
            });
            if (res.ok) {
                setSelectedUserId('');
                setShowAddUser(false);
                fetchCompany();
                fetchAllUsers();
                setMessage({ type: 'success', text: 'User added to company' });
            } else {
                const data = await res.json();
                setMessage({ type: 'error', text: data.error || 'Failed to add user' });
            }
        } catch (err) {
            setMessage({ type: 'error', text: 'An error occurred' });
        }
    };

    const removeUser = async (userId) => {
        if (!confirm('Remove this user from the company?')) return;

        try {
            const res = await fetch(`/api/companies/${id}/users/${userId}`, {
                method: 'DELETE',
                headers: { 'Authorization': `Bearer ${user.token}` }
            });
            if (res.ok) {
                fetchCompany();
                fetchAllUsers();
                setMessage({ type: 'success', text: 'User removed from company' });
            } else {
                setMessage({ type: 'error', text: 'Failed to remove user' });
            }
        } catch (err) {
            setMessage({ type: 'error', text: 'An error occurred' });
        }
    };

    const toggleCompanyAdmin = async (userId, currentStatus) => {
        try {
            const res = await fetch(`/api/companies/${id}/users/${userId}/admin`, {
                method: 'PUT',
                headers: {
                    'Authorization': `Bearer ${user.token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ isAdmin: !currentStatus })
            });
            if (res.ok) {
                fetchCompany();
                setMessage({ type: 'success', text: 'User updated' });
            } else {
                setMessage({ type: 'error', text: 'Failed to update user' });
            }
        } catch (err) {
            setMessage({ type: 'error', text: 'An error occurred' });
        }
    };

    if (loading) {
        return (
            <Layout title="Company Details" showNav onLogout={logout} isAdmin={isSystemAdmin} userName={profile?.firstName}>
                <p>Loading...</p>
            </Layout>
        );
    }

    if (!company) {
        return (
            <Layout title="Company Not Found" showNav onLogout={logout} isAdmin={isSystemAdmin} userName={profile?.firstName}>
                <p>Company not found.</p>
                <button onClick={() => history.push('/admin/companies')}>Back to Companies</button>
            </Layout>
        );
    }

    return (
        <Layout title={company.name} showNav onLogout={logout} isAdmin={isSystemAdmin} userName={profile?.firstName}>
            <Message message={message} />

            <div className="actions-bar">
                <button onClick={() => history.push('/admin/companies')}>Back to Companies</button>
                {isSystemAdmin && (
                    <button onClick={() => setShowAddUser(!showAddUser)}>
                        {showAddUser ? 'Cancel' : 'Add User'}
                    </button>
                )}
            </div>

            {showAddUser && allUsers.length > 0 && (
                <form onSubmit={addUserToCompany} className="inline-form">
                    <div className="form-group">
                        <label>Select User</label>
                        <select
                            value={selectedUserId}
                            onChange={(e) => setSelectedUserId(e.target.value)}
                            required
                        >
                            <option value="">-- Select User --</option>
                            {allUsers.map((u) => (
                                <option key={u.id} value={u.id}>
                                    {u.firstName} {u.lastName} ({u.email})
                                </option>
                            ))}
                        </select>
                    </div>
                    <button type="submit">Add to Company</button>
                </form>
            )}

            <h2>Company Users</h2>
            {company.users.length === 0 ? (
                <p>No users in this company.</p>
            ) : (
                <div className="table-container">
                    <table className="data-table">
                        <thead>
                            <tr>
                                <th>Name</th>
                                <th>Email</th>
                                <th>Company Admin</th>
                                <th>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            {company.users.map((u) => (
                                <tr key={u.id}>
                                    <td>{u.firstName} {u.lastName}</td>
                                    <td>{u.email}</td>
                                    <td>{u.isCompanyAdmin ? 'Yes' : 'No'}</td>
                                    <td>
                                        {isSystemAdmin && (
                                            <>
                                                <button
                                                    className="btn-small"
                                                    onClick={() => toggleCompanyAdmin(u.id, u.isCompanyAdmin)}
                                                >
                                                    {u.isCompanyAdmin ? 'Remove Admin' : 'Make Admin'}
                                                </button>
                                                <button
                                                    className="btn-small btn-danger"
                                                    onClick={() => removeUser(u.id)}
                                                >
                                                    Remove
                                                </button>
                                            </>
                                        )}
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}
        </Layout>
    );
}

// ============================================
// App Component with React Router v5
// ============================================

function App() {
    const { BrowserRouter, Switch, Route, Redirect } = ReactRouterDOM;

    return (
        <BrowserRouter>
            <AuthProvider>
                <Switch>
                    {/* Public routes */}
                    <Route path="/login" component={LoginPage} />
                    <Route path="/register" component={RegisterPage} />
                    <Route path="/forgot-password" component={ForgotPasswordPage} />

                    {/* Protected routes */}
                    <ProtectedRoute path="/dashboard">
                        <DashboardPage />
                    </ProtectedRoute>
                    <ProtectedRoute path="/profile">
                        <ProfilePage />
                    </ProtectedRoute>

                    {/* Admin routes */}
                    <ProtectedRoute path="/admin/users" requireSystemAdmin>
                        <UsersPage />
                    </ProtectedRoute>
                    <ProtectedRoute path="/admin/companies/:id" requireAdmin>
                        <CompanyDetailPage />
                    </ProtectedRoute>
                    <ProtectedRoute path="/admin/companies" requireSystemAdmin>
                        <CompaniesPage />
                    </ProtectedRoute>

                    {/* Default redirect */}
                    <Redirect from="/" to="/dashboard" />
                </Switch>
            </AuthProvider>
        </BrowserRouter>
    );
}

// ============================================
// Initialize
// ============================================

const rootElement = document.getElementById('root');
if (rootElement) {
    ReactDOM.render(<App />, rootElement);
}
