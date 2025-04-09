import React, { useState, useEffect } from 'react';
import './LabelTools.css';

const LabelTools = ({
  isDrawingEnabled,
  setIsDrawingEnabled,
  editingLabelId,
  setEditingLabelId,
  canvasCtxRef,
  labels,
  setLabels,
  resetDrawingState
}) => {
  const [currentPoints, setCurrentPoints] = useState([]);

  // 將十六進位顏色轉換為 RGBA 格式
  const hexToRgba = (hex, alpha = 1) => {
    let r = 0, g = 0, b = 0;

    // 3 位數格式 (#RGB)
    if (hex.length === 4) {
      r = parseInt(hex.charAt(1) + hex.charAt(1), 16);
      g = parseInt(hex.charAt(2) + hex.charAt(2), 16);
      b = parseInt(hex.charAt(3) + hex.charAt(3), 16);
    }
    // 6 位數格式 (#RRGGBB)
    else if (hex.length === 7) {
      r = parseInt(hex.substring(1, 3), 16);
      g = parseInt(hex.substring(3, 5), 16);
      b = parseInt(hex.substring(5, 7), 16);
    }

    return `rgba(${r}, ${g}, ${b}, ${alpha})`;
  };

  // 添加和移除畫布點擊事件監聽器
  useEffect(() => {
    const layerGroup = document.getElementById('layerGroup');
    if (layerGroup && isDrawingEnabled) {
      // 添加點擊事件監聽器用於繪製標記
      layerGroup.addEventListener('click', handleCanvasClick);

      // 防止DWV處理點擊事件
      layerGroup.style.pointerEvents = 'auto';

      // 當鼠標移動時顯示預覽線
      layerGroup.addEventListener('mousemove', handleMouseMove);
    }

    return () => {
      // 移除事件監聽器
      if (layerGroup) {
        layerGroup.removeEventListener('click', handleCanvasClick);
        layerGroup.removeEventListener('mousemove', handleMouseMove);

        // 恢復DWV默認行為
        if (!isDrawingEnabled) {
          layerGroup.style.pointerEvents = '';
        }
      }
    };
  }, [isDrawingEnabled, currentPoints]);

  // 處理Canvas點擊事件以繪製多邊形
  const handleCanvasClick = (event) => {
    if (!isDrawingEnabled || !canvasCtxRef.current) return;

    // 阻止事件冒泡，防止DWV處理點擊事件
    event.stopPropagation();
    event.preventDefault();

    const rect = event.target.getBoundingClientRect();
    const x = event.clientX - rect.left;
    const y = event.clientY - rect.top;

    // 添加新點
    const newPoints = [...currentPoints, { x, y }];
    setCurrentPoints(newPoints);

    // 如果是第一個點，只繪製一個點
    if (newPoints.length === 1) {
      const color = editingLabelId
        ? labels.find(l => l.id === editingLabelId)?.color || '#ff0000'
        : '#ff0000';
      drawPoint(canvasCtxRef.current, x, y, color);
      return;
    }

    // 獲取繪製顏色
    const drawColor = editingLabelId
      ? labels.find(l => l.id === editingLabelId)?.color || '#ff0000'
      : '#ff0000';

    // 繪製線段
    drawLine(
      canvasCtxRef.current,
      newPoints[newPoints.length - 2].x,
      newPoints[newPoints.length - 2].y,
      x,
      y,
      drawColor
    );

    // 檢查是否與起始點接近，如果是則完成多邊形
    if (newPoints.length > 2) {
      const startPoint = newPoints[0];
      const distance = Math.sqrt(
        Math.pow(x - startPoint.x, 2) + Math.pow(y - startPoint.y, 2)
      );

      if (distance < 20) { // 20像素距離閾值
        // 完成多邊形
        drawLine(
          canvasCtxRef.current,
          x,
          y,
          startPoint.x,
          startPoint.y,
          drawColor
        );

        // 填充多邊形（半透明）
        const fillColor = drawColor.startsWith('#')
          ? hexToRgba(drawColor, 0.3)
          : drawColor.replace(')', ', 0.3)').replace('rgb', 'rgba');
        canvasCtxRef.current.fillStyle = fillColor;
        canvasCtxRef.current.beginPath();
        canvasCtxRef.current.moveTo(newPoints[0].x, newPoints[0].y);
        for (let i = 1; i < newPoints.length; i++) {
          canvasCtxRef.current.lineTo(newPoints[i].x, newPoints[i].y);
        }
        canvasCtxRef.current.closePath();
        canvasCtxRef.current.fill();

        // 保存標記
        saveLabel(newPoints);

        // 重置狀態
        setCurrentPoints([]);
        setIsDrawingEnabled(false);
        setEditingLabelId(null);
      }
    }
  };

  // 處理鼠標移動以顯示預覽線
  const handleMouseMove = (event) => {
    if (!isDrawingEnabled || !canvasCtxRef.current || currentPoints.length === 0) return;

    const rect = event.target.getBoundingClientRect();
    const x = event.clientX - rect.left;
    const y = event.clientY - rect.top;

    // 重繪所有點和固定線段
    redrawAllLabels();

    // 繪製從最後一個點到當前鼠標位置的預覽線
    const lastPoint = currentPoints[currentPoints.length - 1];
    drawLine(canvasCtxRef.current, lastPoint.x, lastPoint.y, x, y);
  };

  // 繪製點
  const drawPoint = (ctx, x, y, color = '#ff0000') => {
    ctx.fillStyle = color;
    ctx.beginPath();
    ctx.arc(x, y, 4, 0, 2 * Math.PI);
    ctx.fill();
  };

  // 繪製線段
  const drawLine = (ctx, x1, y1, x2, y2, color = '#ff0000') => {
    ctx.strokeStyle = color;
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(x1, y1);
    ctx.lineTo(x2, y2);
    ctx.stroke();
  };

  // 保存標記
  const saveLabel = (points) => {
    // 為每個標記生成顏色
    const getRandomColor = () => {
      const colors = [
        '#FF5733', '#33FF57', '#3357FF', '#F033FF', '#FF33A8',
        '#33FFF6', '#FFFC33', '#FF9C33', '#9C33FF', '#33C9FF'
      ];
      const index = labels.length % colors.length;
      return colors[index];
    };

    const newLabel = {
      id: editingLabelId || Date.now().toString(),
      points: [...points],
      color: editingLabelId
        ? labels.find(l => l.id === editingLabelId)?.color || getRandomColor()
        : getRandomColor()
    };

    if (editingLabelId) {
      // 更新現有標記
      const updatedLabels = labels.map(label =>
        label.id === editingLabelId ? newLabel : label
      );
      setLabels(updatedLabels);
      setEditingLabelId(null);
    } else {
      // 添加新標記
      setLabels([...labels, newLabel]);
    }
  };

  // 重繪所有標記
  const redrawAllLabels = () => {
    if (!canvasCtxRef.current) return;

    // 清除Canvas
    const canvas = document.getElementById('labelCanvas');
    if (canvas) {
      canvasCtxRef.current.clearRect(0, 0, canvas.width, canvas.height);
    } else {
      console.error("Label canvas not found");
      return;
    }

    // 繪製所有已保存的標記
    labels.forEach(label => {
      const { points, color } = label;
      if (points.length < 2) return;

      const strokeColor = color || '#ff0000';
      const fillColor = color.startsWith('#')
        ? hexToRgba(color, 0.3)
        : color.replace(')', ', 0.3)').replace('rgb', 'rgba');

      // 繪製多邊形
      canvasCtxRef.current.strokeStyle = strokeColor;
      canvasCtxRef.current.lineWidth = 2;
      canvasCtxRef.current.beginPath();
      canvasCtxRef.current.moveTo(points[0].x, points[0].y);

      for (let i = 1; i < points.length; i++) {
        canvasCtxRef.current.lineTo(points[i].x, points[i].y);
      }

      // 連接回起始點
      canvasCtxRef.current.closePath();
      canvasCtxRef.current.stroke();

      // 填充多邊形（半透明）
      canvasCtxRef.current.fillStyle = fillColor;
      canvasCtxRef.current.fill();

      // 繪製頂點
      points.forEach(point => {
        drawPoint(canvasCtxRef.current, point.x, point.y, strokeColor);
      });
    });

    // 繪製正在繪製的多邊形
    if (currentPoints.length > 0) {
      const drawingColor = editingLabelId
        ? labels.find(l => l.id === editingLabelId)?.color || '#ff0000'
        : '#ff0000';

      // 繪製已有的點和線
      for (let i = 0; i < currentPoints.length; i++) {
        drawPoint(canvasCtxRef.current, currentPoints[i].x, currentPoints[i].y, drawingColor);

        if (i > 0) {
          drawLine(
            canvasCtxRef.current,
            currentPoints[i-1].x,
            currentPoints[i-1].y,
            currentPoints[i].x,
            currentPoints[i].y,
            drawingColor
          );
        }
      }
    }
  };

  // 監聽標記變化時重繪
  useEffect(() => {
    redrawAllLabels();
  }, [labels, currentPoints]);

  const handleAddLabelClick = () => {
    if (isDrawingEnabled) {
      // 如果是在編輯狀態，需要取消編輯
      if (editingLabelId) {
        resetDrawingState();
      } else {
        // 否則只是取消繪製
        setIsDrawingEnabled(false);
      }
      setCurrentPoints([]);
    } else {
      // 開始繪製
      setIsDrawingEnabled(true);
      setCurrentPoints([]);
      setEditingLabelId(null);
    }
  };

  return (
    <div className="label-tools">
      <h3>標記工具</h3>
      <button
        className={`add-label-button ${isDrawingEnabled ? 'active' : ''}`}
        onClick={handleAddLabelClick}
      >
        {isDrawingEnabled ? (editingLabelId ? '取消編輯' : '取消') : '新增標記'}
      </button>
      <div className="label-tools-info">
        {isDrawingEnabled && (
          <div className="drawing-instructions">
            {editingLabelId ? '正在編輯標記...' : '正在創建標記...'}
            <br />
            點擊影像開始繪製多邊形。
            <br />
            連續點擊添加多個點。
            <br />
            點擊接近起始點位置完成繪製。
          </div>
        )}
      </div>
    </div>
  );
};

export default LabelTools;