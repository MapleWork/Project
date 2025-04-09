import React, { useEffect, useState } from 'react';
import dicomParser from 'dicom-parser';
import './DicomViewer.css';

export default function DicomInfo({ dicomTags }) {
  const [patientInfo, setPatientInfo] = useState(null);

  // 計算年齡
  const calculateAge = (birthDate) => {
    if (!birthDate || birthDate.length < 8) return 'N/A';
    const year = parseInt(birthDate.substring(0, 4));
    const month = parseInt(birthDate.substring(4, 6)) - 1;
    const day = parseInt(birthDate.substring(6, 8));
    const birth = new Date(year, month, day);
    const now = new Date();
    let age = now.getFullYear() - birth.getFullYear();
    const m = now.getMonth() - birth.getMonth();
    if (m < 0 || (m === 0 && now.getDate() < birth.getDate())) age--;
    return `${age}`;
  };

  // 格式化 DICOM 日期 (YYYYMMDD) 為 YYYY-MM-DD
  const formatDate = (dicomDate) => {
    if (dicomDate === 'N/A' || dicomDate.length < 8) return dicomDate;
    return `${dicomDate.substring(0, 4)}-${dicomDate.substring(4, 6)}-${dicomDate.substring(6, 8)}`;
  };

  // 解析 DICOM 檔案
  useEffect(() => {
    if (!dicomTags) return;

    const reader = new FileReader();
    reader.onload = function (e) {
      try {
        const arrayBuffer = e.target.result;
        const dataSet = dicomParser.parseDicom(new Uint8Array(arrayBuffer));

        // 讀取基本病患資訊
        const patientName = dataSet.string('x00100010') || 'N/A';
        const birthDate = dataSet.string('x00100030') || 'N/A';
        const sex = dataSet.string('x00100040') || 'N/A';
        const age = dataSet.string('x00101010') || calculateAge(birthDate);

        // 讀取檢查資訊
        const studyDate = dataSet.string('x00080020') || 'N/A';
        const studyDescription = dataSet.string('x00081030') || 'N/A';
        const modality = dataSet.string('x00080060') || 'N/A';

        setPatientInfo({
          name: patientName,
          birthDate,
          sex,
          age,
          studyDate,
          studyDescription,
          modality
        });
      } catch (error) {
        console.error('解析 DICOM 檔案失敗:', error);
      }
    };
    reader.readAsArrayBuffer(dicomTags);
  }, [dicomTags]);

  // 顯示簡單資訊列表
  const renderInfoList = () => {
    if (patientInfo) {
      return (
        <div className="info-list">
          <div className="info-item">
            <span className="info-label">病患姓名:</span>
            <span className="info-value">{patientInfo.name}</span>
          </div>
          <div className="info-item">
            <span className="info-label">出生日期:</span>
            <span className="info-value">{formatDate(patientInfo.birthDate)}</span>
          </div>
          <div className="info-item">
            <span className="info-label">年齡:</span>
            <span className="info-value">{patientInfo.age}</span>
          </div>
          <div className="info-item">
            <span className="info-label">性別:</span>
            <span className="info-value">{patientInfo.sex}</span>
          </div>
        </div>
      );
    } else {
      return (
        <div className="info-list empty-info">
          <div className="info-item">
            <span className="info-label">病患姓名:</span>
            <span className="info-value">-</span>
          </div>
          <div className="info-item">
            <span className="info-label">出生日期:</span>
            <span className="info-value">-</span>
          </div>
          <div className="info-item">
            <span className="info-label">年齡:</span>
            <span className="info-value">-</span>
          </div>
          <div className="info-item">
            <span className="info-label">性別:</span>
            <span className="info-value">-</span>
          </div>
        </div>
      );
    }
  };

  return (
    <div className="patient-info">
      {renderInfoList()}
    </div>
  );
}