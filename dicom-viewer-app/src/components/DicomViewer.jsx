import React, { useState, useRef, useEffect } from 'react';
import * as dwv from 'dwv';
import DicomInfo from './DicomInfo';
import LabelTools from './LabelTools';
import LabelList from './LabelList';
import './DicomViewer.css';

const DicomViewer = () => {
  const [dicomImage, setDicomImage] = useState(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState(null);
  const [dicomTags, setDicomTags] = useState(null);
  const [dicomFile, setDicomFile] = useState(null);
  const [labels, setLabels] = useState([]);
  const [isDrawingEnabled, setIsDrawingEnabled] = useState(false);
  const [editingLabelId, setEditingLabelId] = useState(null);

  const canvasRef = useRef(null);
  const fileInputRef = useRef(null);
  const dwvApp = useRef(null);
  const canvasCtxRef = useRef(null);

  // 初始化DWV應用
  useEffect(() => {
    // 初始化DWV
    const app = new dwv.App();
    app.init({
      "dataViewConfigs": {
        "*": [{
          "divId": "layerGroup"
        }]
      }
    });

    // 設置事件監聽器
    app.addEventListener("loadstart", () => {
      setIsLoading(true);
      setError(null);
    });

    app.addEventListener("loadend", () => {
      setIsLoading(false);
      setDicomImage(true);

      try {
        // 取得DICOM標籤
        const tags = app.getMetaData();
        console.log("DWV metadata:", tags);
        setDicomTags(tags);
      } catch (error) {
        console.error("Error getting DICOM metadata:", error);
      }
    });

    app.addEventListener("error", (event) => {
      setIsLoading(false);
      setError(`錯誤: ${event.message}`);
    });

    dwvApp.current = app;

    // 清理函數
    return () => {
      if (dwvApp.current) {
        dwvApp.current.reset();
      }
    };
  }, []);

  // 設置Canvas繪圖上下文
  useEffect(() => {
    if (dicomImage && canvasRef.current) {
      const layerGroup = document.getElementById('layerGroup');
      if (layerGroup) {
        // 移除舊的標記Canvas（如果存在）
        const oldCanvas = document.getElementById('labelCanvas');
        if (oldCanvas) {
          oldCanvas.remove();
        }

        // 創建覆蓋Canvas用於繪製標記
        const canvas = document.createElement('canvas');
        canvas.id = 'labelCanvas';
        canvas.width = layerGroup.offsetWidth;
        canvas.height = layerGroup.offsetHeight;
        canvas.style.position = 'absolute';
        canvas.style.top = '0';
        canvas.style.left = '0';
        canvas.style.pointerEvents = 'none';
        layerGroup.appendChild(canvas);

        // 設置繪圖上下文
        canvasCtxRef.current = canvas.getContext('2d');
      }
    }
  }, [dicomImage]);

  // 處理文件上傳
  const handleFileUpload = (event) => {
    const file = event.target.files[0];
    if (!file) return;

    if (dwvApp.current) {
      // 重置先前的狀態
      dwvApp.current.reset();
      setDicomImage(null);
      setDicomTags(null);
      setDicomFile(null);
      setError(null);
      setLabels([]);
      setIsDrawingEnabled(false);
      setEditingLabelId(null);

      // 清除標記Canvas
      if (canvasCtxRef.current) {
        const canvas = document.getElementById('labelCanvas');
        if (canvas) {
          canvasCtxRef.current.clearRect(0, 0, canvas.width, canvas.height);
        }
      }

      // 載入文件
      try {
        dwvApp.current.loadFiles([file]);
        setDicomFile(file);
      } catch (error) {
        console.error("Error loading DICOM file:", error);
        setError(`載入DICOM文件時發生錯誤: ${error.message || '未知錯誤'}`);
      }
    }
  };

  // 處理上傳按鈕點擊
  const handleUploadClick = () => {
    if (fileInputRef.current) {
      fileInputRef.current.click();
    }
  };

  // 重置編輯狀態
  const resetDrawingState = () => {
    setIsDrawingEnabled(false);
    setEditingLabelId(null);
  };

  return (
    <div className="dicom-viewer-container">
      <h2>DICOM 檢視器</h2>

      <div className="main-container">
        <div className="left-panel">
          <div className="upload-section">
            <input
              type="file"
              ref={fileInputRef}
              onChange={handleFileUpload}
              accept=".dcm"
              style={{ display: 'none' }}
            />
            <button
              onClick={handleUploadClick}
              className="upload-button"
              disabled={isLoading}
            >
              {isLoading ? '載入中...' : '上傳 DICOM 檔案'}
            </button>
          </div>

          {error && <div className="error-message">{error}</div>}

          {/* DICOM 資訊展示 */}
          <div className="patient-info-container">
            <DicomInfo dicomTags={dicomFile} />
          </div>
        </div>

        <div className="middle-panel">
          <div className="viewer-section">
            {isLoading && <div className="loading">正在載入 DICOM 檔案...</div>}
            <div id="layerGroup" ref={canvasRef} className="dicom-canvas"></div>
          </div>
        </div>

        <div className="right-panel">
          {/* 標記工具 */}
          <LabelTools
            isDrawingEnabled={isDrawingEnabled}
            setIsDrawingEnabled={setIsDrawingEnabled}
            editingLabelId={editingLabelId}
            setEditingLabelId={setEditingLabelId}
            canvasCtxRef={canvasCtxRef}
            labels={labels}
            setLabels={setLabels}
            resetDrawingState={resetDrawingState}
          />

          {/* 標記列表 */}
          <LabelList
            labels={labels}
            setLabels={setLabels}
            setIsDrawingEnabled={setIsDrawingEnabled}
            setEditingLabelId={setEditingLabelId}
            canvasCtxRef={canvasCtxRef}
          />
        </div>
      </div>
    </div>
  );
};

export default DicomViewer;