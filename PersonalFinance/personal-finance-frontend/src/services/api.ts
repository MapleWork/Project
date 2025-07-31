import axios from 'axios';
import type { AxiosInstance, AxiosResponse } from 'axios';
import type {
  ApiResponse,
  LoginRequest,
  RegisterRequest,
  AuthResponse,
  Category,
  CreateCategoryRequest,
  UpdateCategoryRequest,
  Transaction,
  CreateTransactionRequest,
  UpdateTransactionRequest,
  StatisticsResponse
} from '../types';

// API 基礎配置
const API_BASE_URL = 'http://localhost:5000/api';

// 建立 axios 實例
const apiClient: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// 請求攔截器 - 自動添加 JWT Token
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// 回應攔截器 - 處理錯誤和令牌過期
apiClient.interceptors.response.use(
  (response: AxiosResponse) => {
    return response;
  },
  (error) => {
    if (error.response?.status === 401) {
      // Token 過期或無效，清除本地存儲並重定向到登入頁面
      localStorage.removeItem('authToken');
      localStorage.removeItem('userInfo');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

// API 服務類別
class ApiService {
  // 認證相關
  async login(credentials: LoginRequest): Promise<AuthResponse> {
    const response = await apiClient.post<ApiResponse<AuthResponse>>('/Auth/login', credentials);
    return response.data.data;
  }

  async register(userData: RegisterRequest): Promise<AuthResponse> {
    const response = await apiClient.post<ApiResponse<AuthResponse>>('/Auth/register', userData);
    return response.data.data;
  }

  // 分類相關
  async getCategories(): Promise<Category[]> {
    const response = await apiClient.get<ApiResponse<Category[]>>('/Categories');
    return response.data.data;
  }

  async getCategoriesByType(type: 'Income' | 'Expense'): Promise<Category[]> {
    const response = await apiClient.get<ApiResponse<Category[]>>(`/Categories?type=${type}`);
    return response.data.data;
  }

  async createCategory(category: CreateCategoryRequest): Promise<Category> {
    const response = await apiClient.post<ApiResponse<Category>>('/Categories', category);
    return response.data.data;
  }

  async updateCategory(id: number, category: UpdateCategoryRequest): Promise<Category> {
    const response = await apiClient.put<ApiResponse<Category>>(`/Categories/${id}`, category);
    return response.data.data;
  }

  async deleteCategory(id: number): Promise<void> {
    await apiClient.delete(`/Categories/${id}`);
  }

  // 交易相關
  async getTransactions(page: number = 1, pageSize: number = 10): Promise<Transaction[]> {
    const response = await apiClient.get<ApiResponse<Transaction[]>>(`/Transactions?page=${page}&pageSize=${pageSize}`);
    return response.data.data;
  }

  async getTransaction(id: number): Promise<Transaction> {
    const response = await apiClient.get<ApiResponse<Transaction>>(`/Transactions/${id}`);
    return response.data.data;
  }

  async createTransaction(transaction: CreateTransactionRequest): Promise<Transaction> {
    const response = await apiClient.post<ApiResponse<Transaction>>('/Transactions', transaction);
    return response.data.data;
  }

  async updateTransaction(id: number, transaction: UpdateTransactionRequest): Promise<Transaction> {
    const response = await apiClient.put<ApiResponse<Transaction>>(`/Transactions/${id}`, transaction);
    return response.data.data;
  }

  async deleteTransaction(id: number): Promise<void> {
    await apiClient.delete(`/Transactions/${id}`);
  }

  async searchTransactions(keyword: string, page: number = 1, pageSize: number = 10): Promise<Transaction[]> {
    const response = await apiClient.get<ApiResponse<Transaction[]>>(`/Transactions/search?keyword=${encodeURIComponent(keyword)}&page=${page}&pageSize=${pageSize}`);
    return response.data.data;
  }

  async getStatistics(year: number, month: number): Promise<StatisticsResponse> {
    const response = await apiClient.get<ApiResponse<StatisticsResponse>>(`/Transactions/statistics?year=${year}&month=${month}`);
    return response.data.data;
  }
}

// 匯出單例實例
export const apiService = new ApiService();

// 匯出 axios 實例（供需要直接使用的情況）
export { apiClient };