import React, { createContext, useContext, useState, useEffect } from 'react';
import type { ReactNode } from 'react';
import { apiService } from '../services/api';
import type { AuthResponse, LoginRequest, RegisterRequest } from '../types';

interface AuthContextType {
    user: AuthResponse | null;
    isLoading: boolean;
    isAuthenticated: boolean;
    login: (credentials: LoginRequest) => Promise<void>;
    register: (userData: RegisterRequest) => Promise<void>;
    logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

interface AuthProviderProps {
    children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
    const [user, setUser] = useState<AuthResponse | null>(null);
    const [isLoading, setIsLoading] = useState(true);

    // 檢查本地存儲中的認證資訊
    useEffect(() => {
        const initializeAuth = () => {
            try {
                const token = localStorage.getItem('authToken');
                const userInfo = localStorage.getItem('userInfo');

                if (token && userInfo) {
                    const parsedUser = JSON.parse(userInfo);
                    setUser(parsedUser);
                }
            } catch (error) {
                console.error('Failed to parse user info from localStorage:', error);
                localStorage.removeItem('authToken');
                localStorage.removeItem('userInfo');
            } finally {
                setIsLoading(false);
            }
        };

        initializeAuth();
    }, []);

    const login = async (credentials: LoginRequest): Promise<void> => {
        try {
            setIsLoading(true);
            const authResponse = await apiService.login(credentials);

            // 存儲認證資訊
            localStorage.setItem('authToken', authResponse.token);
            localStorage.setItem('userInfo', JSON.stringify(authResponse));

            setUser(authResponse);
        } catch (error) {
            throw error;
        } finally {
            setIsLoading(false);
        }
    };

    const register = async (userData: RegisterRequest): Promise<void> => {
        try {
          setIsLoading(true);
          const authResponse = await apiService.register(userData);
          
          // 存儲認證資訊
          localStorage.setItem('authToken', authResponse.token);
          localStorage.setItem('userInfo', JSON.stringify(authResponse));
          
          setUser(authResponse);
        } catch (error) {
          throw error;
        } finally {
          setIsLoading(false);
        }
      };

      const logout = (): void => {
        localStorage.removeItem('authToken');
        localStorage.removeItem('userInfo');
        setUser(null);
    };

    const value: AuthContextType = {
        user,
        isLoading,
        isAuthenticated: !!user,
        login,
        register,
        logout,
    };

    return (
        <AuthContext.Provider value={value}>
            {children}
        </AuthContext.Provider>
    );
};

export const useAuth = (): AuthContextType => {
    const context = useContext(AuthContext);
    if (context === undefined) {
        throw new Error('useAuth must be used within an AuthProvider');
    }
    return context;
};

export default AuthContext;