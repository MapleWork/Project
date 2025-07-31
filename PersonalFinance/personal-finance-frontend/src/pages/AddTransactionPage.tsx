import React, { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { apiService } from '../services/api';
import type { CreateTransactionRequest, Category } from '../types';

const AddTransactionPage: React.FC = () => {
  const navigate = useNavigate();
  const [categories, setCategories] = useState<Category[]>([]);
  const [filteredCategories, setFilteredCategories] = useState<Category[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [transactionType, setTransactionType] = useState<'Income' | 'Expense'>('Expense');

  const { register, handleSubmit, formState: { errors }, watch, setValue, reset } = useForm<CreateTransactionRequest>({
    defaultValues: {
      type: 'Expense',
      date: new Date().toISOString().split('T')[0], // 今天的日期
      amount: 0,
      description: '',
      categoryId: 0
    }
  });

  const watchType = watch('type');

  // 載入分類
  useEffect(() => {
    const loadCategories = async () => {
      try {
        const categoriesData = await apiService.getCategories();
        setCategories(categoriesData);
      } catch (err) {
        console.error('Failed to load categories:', err);
        setError('載入分類失敗');
      }
    };

    loadCategories();
  }, []);

  // 根據交易類型篩選分類
  useEffect(() => {
    const filtered = categories.filter(cat => cat.type === watchType);
    setFilteredCategories(filtered);
    
    // 重設分類選擇
    if (filtered.length > 0) {
      setValue('categoryId', filtered[0].id);
    }
  }, [watchType, categories, setValue]);

  // 處理交易類型切換
  const handleTypeChange = (type: 'Income' | 'Expense') => {
    setTransactionType(type);
    setValue('type', type);
  };

  const onSubmit = async (data: CreateTransactionRequest) => {
    try {
      setIsLoading(true);
      setError(null);

      await apiService.createTransaction(data);
      
      // 成功後導向交易記錄頁面
      navigate('/transactions', { 
        state: { message: '交易記錄新增成功！' }
      });
    } catch (err: any) {
      setError(err.response?.data?.message || '新增交易失敗，請稍後再試');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* 頂部導航 */}
      <nav className="bg-white shadow-sm border-b">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex items-center">
              <Link to="/dashboard" className="flex items-center text-gray-600 hover:text-gray-900">
                <svg className="h-5 w-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
                </svg>
                返回儀表板
              </Link>
            </div>
            
            <div className="flex items-center">
              <h1 className="text-xl font-semibold text-gray-900">新增交易</h1>
            </div>
            
            <div className="flex items-center">
              <Link 
                to="/transactions"
                className="text-primary-600 hover:text-primary-700 font-medium"
              >
                查看記錄
              </Link>
            </div>
          </div>
        </div>
      </nav>

      {/* 主要內容 */}
      <main className="max-w-2xl mx-auto py-8 px-4 sm:px-6 lg:px-8">
        <div className="bg-white rounded-lg shadow-lg">
          {/* 表單標題 */}
          <div className="px-6 py-4 border-b border-gray-200">
            <h2 className="text-lg font-medium text-gray-900">記錄新交易</h2>
            <p className="text-sm text-gray-500 mt-1">請填寫交易詳細資訊</p>
          </div>

          {/* 交易表單 */}
          <form onSubmit={handleSubmit(onSubmit)} className="p-6 space-y-6">
            {/* 錯誤訊息 */}
            {error && (
              <div className="bg-red-50 border border-red-200 text-red-800 px-4 py-3 rounded-md">
                {error}
              </div>
            )}

            {/* 交易類型切換 */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-3">
                交易類型
              </label>
              <div className="flex space-x-4">
                <button
                  type="button"
                  onClick={() => handleTypeChange('Expense')}
                  className={`flex-1 py-3 px-4 rounded-lg border-2 transition-colors ${
                    watchType === 'Expense'
                      ? 'border-red-500 bg-red-50 text-red-700'
                      : 'border-gray-200 bg-white text-gray-500 hover:border-gray-300'
                  }`}
                >
                  <div className="flex items-center justify-center">
                    <svg className="h-5 w-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 14l-7 7m0 0l-7-7m7 7V3" />
                    </svg>
                    支出
                  </div>
                </button>
                <button
                  type="button"
                  onClick={() => handleTypeChange('Income')}
                  className={`flex-1 py-3 px-4 rounded-lg border-2 transition-colors ${
                    watchType === 'Income'
                      ? 'border-green-500 bg-green-50 text-green-700'
                      : 'border-gray-200 bg-white text-gray-500 hover:border-gray-300'
                  }`}
                >
                  <div className="flex items-center justify-center">
                    <svg className="h-5 w-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 10l7-7m0 0l7 7m-7-7v18" />
                    </svg>
                    收入
                  </div>
                </button>
              </div>
              <input type="hidden" {...register('type')} />
            </div>

            {/* 金額 */}
            <div>
              <label htmlFor="amount" className="block text-sm font-medium text-gray-700 mb-1">
                金額 <span className="text-red-500">*</span>
              </label>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                  <span className="text-gray-500 sm:text-sm">$</span>
                </div>
                <input
                  id="amount"
                  type="number"
                  step="0.01"
                  min="0"
                  className={`w-full pl-7 pr-3 py-2 border rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent ${
                    errors.amount ? 'border-red-300' : 'border-gray-300'
                  }`}
                  placeholder="0.00"
                  {...register('amount', {
                    required: '請輸入金額',
                    min: {
                      value: 0.01,
                      message: '金額必須大於 0'
                    },
                    max: {
                      value: 999999999,
                      message: '金額過大'
                    }
                  })}
                />
              </div>
              {errors.amount && (
                <p className="mt-1 text-sm text-red-600">{errors.amount.message}</p>
              )}
            </div>

            {/* 分類 */}
            <div>
              <label htmlFor="categoryId" className="block text-sm font-medium text-gray-700 mb-1">
                分類 <span className="text-red-500">*</span>
              </label>
              <select
                id="categoryId"
                className={`w-full px-3 py-2 border rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent ${
                  errors.categoryId ? 'border-red-300' : 'border-gray-300'
                }`}
                {...register('categoryId', {
                  required: '請選擇分類',
                  validate: value => value > 0 || '請選擇有效的分類'
                })}
              >
                <option value={0}>請選擇分類</option>
                {filteredCategories.map(category => (
                  <option key={category.id} value={category.id}>
                    <span 
                      className="inline-block w-3 h-3 rounded-full mr-2"
                      style={{ backgroundColor: category.color }}
                    ></span>
                    {category.name}
                  </option>
                ))}
              </select>
              {errors.categoryId && (
                <p className="mt-1 text-sm text-red-600">{errors.categoryId.message}</p>
              )}
            </div>

            {/* 日期 */}
            <div>
              <label htmlFor="date" className="block text-sm font-medium text-gray-700 mb-1">
                日期 <span className="text-red-500">*</span>
              </label>
              <input
                id="date"
                type="date"
                className={`w-full px-3 py-2 border rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent ${
                  errors.date ? 'border-red-300' : 'border-gray-300'
                }`}
                {...register('date', {
                  required: '請選擇日期'
                })}
              />
              {errors.date && (
                <p className="mt-1 text-sm text-red-600">{errors.date.message}</p>
              )}
            </div>

            {/* 描述 */}
            <div>
              <label htmlFor="description" className="block text-sm font-medium text-gray-700 mb-1">
                描述 <span className="text-red-500">*</span>
              </label>
              <textarea
                id="description"
                rows={3}
                className={`w-full px-3 py-2 border rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent ${
                  errors.description ? 'border-red-300' : 'border-gray-300'
                }`}
                placeholder="請輸入交易描述..."
                {...register('description', {
                  required: '請輸入交易描述',
                  minLength: {
                    value: 2,
                    message: '描述至少需要2個字元'
                  },
                  maxLength: {
                    value: 200,
                    message: '描述不可超過200個字元'
                  }
                })}
              />
              {errors.description && (
                <p className="mt-1 text-sm text-red-600">{errors.description.message}</p>
              )}
            </div>

            {/* 提交按鈕 */}
            <div className="flex space-x-4 pt-4">
              <button
                type="button"
                onClick={() => navigate('/dashboard')}
                className="flex-1 bg-gray-100 text-gray-700 py-2 px-4 rounded-md hover:bg-gray-200 transition-colors font-medium"
              >
                取消
              </button>
              <button
                type="submit"
                disabled={isLoading}
                className={`flex-1 py-2 px-4 rounded-md font-medium transition-colors ${
                  watchType === 'Income'
                    ? 'bg-green-600 hover:bg-green-700 text-white'
                    : 'bg-red-600 hover:bg-red-700 text-white'
                } disabled:opacity-50 disabled:cursor-not-allowed`}
              >
                {isLoading ? (
                  <div className="flex items-center justify-center">
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                    處理中...
                  </div>
                ) : (
                  `新增${watchType === 'Income' ? '收入' : '支出'}`
                )}
              </button>
            </div>
          </form>
        </div>
      </main>
    </div>
  );
};

export default AddTransactionPage;