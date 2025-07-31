// API 回應格式
export interface ApiResponse<T> {
    success: boolean;
    data: T;
    message?: string;
}

// 認證相關類型
export interface LoginRequest {
  username: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  username: string;
  email: string;
}

// 使用者類型
export interface User {
  id: number;
  username: string;
  email: string;
  createdAt: string;
}

// 分類類型
export interface Category {
  id: number;
  name: string;
  color: string;
  type: 'Income' | 'Expense';
}

export interface CreateCategoryRequest {
  name: string;
  color: string;
  type: 'Income' | 'Expense';
}

export interface UpdateCategoryRequest {
  name: string;
  color: string;
}

// 交易類型
export interface Transaction {
  id: number;
  amount: number;
  description: string;
  date: string;
  type: 'Income' | 'Expense';
  categoryId: number;
  categoryName: string;
  categoryColor: string;
}

export interface CreateTransactionRequest {
  amount: number;
  description: string;
  date: string;
  type: 'Income' | 'Expense';
  categoryId: number;
}

export interface UpdateTransactionRequest {
  amount: number;
  description: string;
  date: string;
  categoryId: number;
}

// 統計類型
export interface StatisticsResponse {
  period: {
    year: number;
    month: number;
    startDate: string;
    endDate: string;
  };
  summary: {
    totalIncome: number;
    totalExpense: number;
    netAmount: number;
    transactionCount: number;
    averageTransactionAmount: number;
  };
  categoryBreakdown: Array<{
    categoryId: number;
    categoryName: string;
    categoryColor: string;
    type: string;
    amount: number;
    transactionCount: number;
    percentage: number;
  }>;
  dailyTrends: Array<{
    date: string;
    income: number;
    expense: number;
    net: number;
  }>;
}