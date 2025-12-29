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

function Layout({ children, title, showNav = false, onLogout, isAdmin = false }) {
    const history = ReactRouterDOM.useHistory();

    return (
        <div className="layout">
            {showNav && (
                <nav className="navbar">
                    <div className="nav-brand" onClick={() => history.push('/dashboard')}>
                        FlexiTrack
                    </div>
                    <div className="nav-links">
                        <a onClick={() => history.push('/dashboard')}>Dashboard</a>
                        <a onClick={() => history.push('/profile')}>Profile</a>
                        {isAdmin && <a onClick={() => history.push('/admin/users')}>Users</a>}
                        {isAdmin && <a onClick={() => history.push('/admin/companies')}>Companies</a>}
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

function DashboardPage() {
    const { user, profile, logout, isSystemAdmin, isCompanyAdmin } = useAuth();
    const history = ReactRouterDOM.useHistory();

    const [taskDate, setTaskDate] = React.useState(new Date().toISOString().split('T')[0]);
    const [startTime, setStartTime] = React.useState('09:00');
    const [endTime, setEndTime] = React.useState('17:00');
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
    const [clients, setClients] = React.useState([]);
    const [showClientSuggestions, setShowClientSuggestions] = React.useState(false);
    const [filteredClients, setFilteredClients] = React.useState([]);

    const fetchClients = async () => {
        if (!user?.token) return;
        try {
            const res = await fetch('/api/tasks/clients', {
                headers: { 'Authorization': `Bearer ${user.token}` }
            });
            if (res.ok) {
                const data = await res.json();
                setClients(data.clients || []);
            }
        } catch (err) {
            console.error('Failed to fetch clients:', err);
        }
    };

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
        fetchClients();
    }, [user?.token]);

    const handleClientChange = (value) => {
        setClient(value);
        if (value.length > 0) {
            const filtered = clients.filter(c =>
                c.toLowerCase().includes(value.toLowerCase())
            );
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

    const handleLogTask = async (e) => {
        e.preventDefault();
        setMessage(null);
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
                fetchClients();
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
        >
            <div className="dashboard">
                <div className="card">
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <h2>Welcome, {profile.firstName}!</h2>
                        <button onClick={() => history.push('/profile')}>View Profile</button>
                    </div>
                </div>

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
                                        setFilteredClients(client ? clients.filter(c => c.toLowerCase().includes(client.toLowerCase())) : clients);
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
                    <h2>Recent Task Logs</h2>
                    {taskLogs.length === 0 ? (
                        <p>No task logs yet. Start logging your work above!</p>
                    ) : (
                        <table className="data-table">
                            <thead>
                                <tr>
                                    <th>Date</th>
                                    <th>Time</th>
                                    <th>Client</th>
                                    <th>Description</th>
                                    <th>Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                {taskLogs.map((log) => (
                                    editingId === log.id ? (
                                        <tr key={log.id}>
                                            <td>
                                                <input
                                                    type="date"
                                                    value={editDate}
                                                    onChange={(e) => setEditDate(e.target.value)}
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
                                            <td>{log.startTime} - {log.endTime}</td>
                                            <td>{log.client || '-'}</td>
                                            <td>{log.description}</td>
                                            <td>
                                                <button
                                                    className="btn-small"
                                                    onClick={() => startEdit(log)}
                                                >
                                                    Edit
                                                </button>
                                                <button
                                                    className="btn-small btn-danger"
                                                    onClick={() => handleDelete(log.id)}
                                                >
                                                    Delete
                                                </button>
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
    const { profile, logout, isSystemAdmin } = useAuth();

    if (!profile) {
        return (
            <Layout title="Profile" showNav onLogout={logout}>
                <p>Loading...</p>
            </Layout>
        );
    }

    return (
        <Layout title="Profile" showNav onLogout={logout} isAdmin={isSystemAdmin}>
            <div className="profile-details">
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

    const { user, logout, isSystemAdmin } = useAuth();
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
        <Layout title="Manage Users" showNav onLogout={logout} isAdmin={isSystemAdmin}>
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

    const { user, logout, isSystemAdmin } = useAuth();
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
        <Layout title="Manage Companies" showNav onLogout={logout} isAdmin={isSystemAdmin}>
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

    const { user, logout, isSystemAdmin } = useAuth();
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
            <Layout title="Company Details" showNav onLogout={logout} isAdmin={isSystemAdmin}>
                <p>Loading...</p>
            </Layout>
        );
    }

    if (!company) {
        return (
            <Layout title="Company Not Found" showNav onLogout={logout} isAdmin={isSystemAdmin}>
                <p>Company not found.</p>
                <button onClick={() => history.push('/admin/companies')}>Back to Companies</button>
            </Layout>
        );
    }

    return (
        <Layout title={company.name} showNav onLogout={logout} isAdmin={isSystemAdmin}>
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
