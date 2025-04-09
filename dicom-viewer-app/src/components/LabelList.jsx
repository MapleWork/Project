import React from 'react';
import './LabelList.css';

const LabelList = ({
  labels,
  setLabels,
  setIsDrawingEnabled,
  setEditingLabelId,
  canvasCtxRef
}) => {
  // 處理編輯標記
  const handleEditLabel = (labelId) => {
    const labelToEdit = labels.find(label => label.id === labelId);
    if (labelToEdit) {
      // 設置編輯模式
      setEditingLabelId(labelId);
      setIsDrawingEnabled(true);
    }
  };

  // 處理刪除標記
  const handleDeleteLabel = (labelId) => {
    setLabels(labels.filter(label => label.id !== labelId));

    // 如果有Canvas上下文，重繪剩餘標記
    if (canvasCtxRef.current) {
      const canvas = document.getElementById('labelCanvas');
      if (canvas) {
        canvasCtxRef.current.clearRect(0, 0, canvas.width, canvas.height);

        // 繪製其餘標記（此處可以重用繪製功能，但為了簡化只清除畫布）
        // 完整的重繪邏輯在 LabelTools 元件中
      }
    }
  };

  return (
    <div className="label-list">
      <h3>標記列表</h3>
      {labels.length === 0 ? (
        <div className="no-labels">尚未添加標記</div>
      ) : (
        <ul className="labels">
          {labels.map((label, index) => (
            <li key={label.id} className="label-item">
              <div
                className="label-name"
                style={{ color: label.color || '#000000' }}
              >
                • 標記 {index + 1}
              </div>
              <div className="label-actions">
                <button
                  className="label-edit-btn"
                  onClick={() => handleEditLabel(label.id)}
                  title="編輯標記"
                >
                  <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path>
                    <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path>
                  </svg>
                </button>
                <button
                  className="label-delete-btn"
                  onClick={() => handleDeleteLabel(label.id)}
                  title="刪除標記"
                >
                  <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <polyline points="3 6 5 6 21 6"></polyline>
                    <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
                  </svg>
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
};

export default LabelList;