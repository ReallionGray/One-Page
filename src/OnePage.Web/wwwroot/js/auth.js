import { e, getStoredUser } from './utils.js';

// ===== Auth Context =====
const AuthContext = React.createContext();
function useAuth() { return React.useContext(AuthContext); }

function AuthProvider({ children }) {
  const [user, setUser] = React.useState(() => getStoredUser());
  const login = React.useCallback((userData) => {
    localStorage.setItem('onepage_user', JSON.stringify(userData));
    setUser(userData);
  }, []);
  const logout = React.useCallback(() => {
    localStorage.removeItem('onepage_user');
    setUser(null);
    window.location.hash = '#login';
  }, []);
  const value = React.useMemo(() => ({ user, login, logout, isAuthenticated: !!user }), [user, login, logout]);
  return e(AuthContext.Provider, { value }, children);
}

function useToast() {
  const [toasts, setToasts] = React.useState([]);
  const show = React.useCallback((message, type) => {
    const id = Date.now() + Math.random();
    setToasts(t => [...t, { id, message, type }]);
    setTimeout(() => setToasts(t => t.filter(x => x.id !== id)), 4000);
  }, []);
  const ToastHost = () => e(React.Fragment, null,
    toasts.map(t => e('div', {
      key: t.id, className: 'alert-toast ' + (t.type === 'err' ? 'err' : 'ok'),
      onClick: () => setToasts(x => x.filter(y => y.id !== t.id))
    }, t.message)));
  return { toast: show, ToastHost };
}

export { AuthContext, useAuth, AuthProvider, useToast };
