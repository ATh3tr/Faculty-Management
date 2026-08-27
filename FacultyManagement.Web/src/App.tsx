import { Navigate, Route, Routes, useLocation } from "react-router-dom";
import { useAuth } from "./auth/AuthContext";
import { AppShell } from "./components/AppShell";
import { LoadingBlock } from "./components/ui";
import { LoginPage, RegisterPage } from "./pages/AuthPages";
import { DashboardPage } from "./pages/DashboardPage";
import { SchedulePage } from "./pages/SchedulePage";
import { MarksPage } from "./pages/MarksPage";
import { AppealsPage } from "./pages/AppealsPage";
import { DivisionPage } from "./pages/DivisionPage";
import { AnnouncementsPage } from "./pages/AnnouncementsPage";
import { AcademicPage } from "./pages/AcademicPage";
import { PeoplePage } from "./pages/PeoplePage";
import { RoomsPage } from "./pages/RoomsPage";
import { TimetablePage } from "./pages/TimetablePage";
import { SettingsPage } from "./pages/SettingsPage";
import { NotificationsPage } from "./pages/NotificationsPage";
import type { ReactNode } from "react";
import type { Role } from "./types";

function Protected({ children, roles }: { children: ReactNode; roles?: Role[] }) {
  const { user, loading } = useAuth(); const location = useLocation();
  if (loading) return <div className="screen-loader"><LoadingBlock /></div>;
  if (!user) return <Navigate to="/login" state={{ from: location }} replace />;
  if (roles && !user.roles.some(role => roles.includes(role))) return <Navigate to="/" replace />;
  return children;
}

export default function App() {
  return <Routes>
    <Route path="/login" element={<LoginPage />} /><Route path="/register" element={<RegisterPage />} />
    <Route element={<Protected><AppShell /></Protected>}>
      <Route index element={<DashboardPage />} /><Route path="schedule" element={<SchedulePage />} />
      <Route path="marks" element={<Protected roles={["Student", "ExamsOfficer", "Admin"]}><MarksPage /></Protected>} />
      <Route path="appeals" element={<Protected roles={["Student", "Professor", "ExamsOfficer", "Admin"]}><AppealsPage /></Protected>} />
      <Route path="division" element={<Protected roles={["Student"]}><DivisionPage /></Protected>} />
      <Route path="announcements" element={<Protected roles={["Teacher", "Professor", "ExamsOfficer", "Admin"]}><AnnouncementsPage /></Protected>} />
      <Route path="academic" element={<Protected roles={["Admin"]}><AcademicPage /></Protected>} />
      <Route path="people" element={<Protected roles={["Admin"]}><PeoplePage /></Protected>} />
      <Route path="rooms" element={<Protected roles={["Admin"]}><RoomsPage /></Protected>} />
      <Route path="timetable" element={<Protected roles={["Admin"]}><TimetablePage /></Protected>} />
      <Route path="settings" element={<Protected roles={["Admin"]}><SettingsPage /></Protected>} />
      <Route path="notifications" element={<NotificationsPage />} />
    </Route>
    <Route path="*" element={<Navigate to="/" replace />} />
  </Routes>;
}
