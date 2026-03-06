import { Routes, Route } from 'react-router-dom';
import MainLayout from './layouts/MainLayout';
import DevicesPage from './pages/DevicesPage';
import ProfilesPage from './pages/ProfilesPage';
import MacrosPage from './pages/MacrosPage';
import SettingsPage from './pages/SettingsPage';
import PresetsPage from './pages/PresetsPage';

function App() {
  return (
    <MainLayout>
      <Routes>
        <Route path="/" element={<DevicesPage />} />
        <Route path="/profiles" element={<ProfilesPage />} />
        <Route path="/macros" element={<MacrosPage />} />
        <Route path="/presets" element={<PresetsPage />} />
        <Route path="/settings" element={<SettingsPage />} />
      </Routes>
    </MainLayout>
  );
}

export default App;
