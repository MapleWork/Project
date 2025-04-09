import React from 'react';
import DicomViewer from './components/DicomViewer';
import './App.css';

function App() {
  return (
    <div className="App">
      <header className="App-header">
        <h1>DICOM 檢視與標記工具</h1>
      </header>
      <main className="App-main">
        <DicomViewer />
      </main>
      <footer className="App-footer">
        <p>DICOM 檢視器 © {new Date().getFullYear()}</p>
      </footer>
    </div>
  );
}

export default App;