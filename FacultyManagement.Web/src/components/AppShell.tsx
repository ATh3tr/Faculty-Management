import { useEffect, useState, type ComponentType } from "react";
import { NavLink, Outlet, useNavigate } from "react-router-dom";
import { Bell, BookOpenCheck, Building2, CalendarDays, ChevronLeft, ClipboardCheck, FileClock, Gauge, GraduationCap, Languages, LogOut, Megaphone, Menu, PanelLeftClose, Settings, Sparkles, Users, X } from "lucide-react";
import { useAuth } from "../auth/AuthContext";
import { useBranding } from "../branding/BrandingContext";
import { useLanguage } from "../lib/i18n";
import type { Role } from "../types";

type NavItem = { to: string; key: string; icon: ComponentType<{ size?: number }>; roles?: Role[] };
const navigation: NavItem[] = [
  { to: "/", key: "overview", icon: Gauge }, { to: "/schedule", key: "schedule", icon: CalendarDays },
  { to: "/marks", key: "marks", icon: GraduationCap, roles: ["Student", "ExamsOfficer", "Admin"] },
  { to: "/appeals", key: "appeals", icon: FileClock, roles: ["Student", "Professor", "ExamsOfficer", "Admin"] },
  { to: "/division", key: "division", icon: Users, roles: ["Student"] },
  { to: "/announcements", key: "announcements", icon: Megaphone, roles: ["Teacher", "Professor", "ExamsOfficer", "Admin"] },
  { to: "/academic", key: "academic", icon: BookOpenCheck, roles: ["Admin"] },
  { to: "/people", key: "people", icon: Users, roles: ["Admin"] },
  { to: "/rooms", key: "rooms", icon: Building2, roles: ["Admin"] },
  { to: "/timetable", key: "timetable", icon: Sparkles, roles: ["Admin"] },
  { to: "/settings", key: "settings", icon: Settings, roles: ["Admin"] },
  { to: "/notifications", key: "notifications", icon: Bell }
];

export function AppShell() {
  const { user, logout, liveNotification } = useAuth(); const { facultyName } = useBranding(); const { t, language, toggleLanguage, pick } = useLanguage();
  const navigate = useNavigate(); const [open, setOpen] = useState(false); const [collapsed, setCollapsed] = useState(false); const [toast, setToast] = useState(liveNotification);
  useEffect(() => { if (liveNotification) { setToast(liveNotification); const timer = window.setTimeout(() => setToast(null), 6000); return () => window.clearTimeout(timer); } }, [liveNotification]);
  const visible = navigation.filter(item => !item.roles || user?.roles.some(role => item.roles!.includes(role)));
  const signOut = async () => { await logout(); navigate("/login"); };
  return <div className={`app-shell ${collapsed ? "is-collapsed" : ""}`}>
    <aside className={`sidebar ${open ? "is-open" : ""}`}>
      <div className="brand-block"><div className="brand-emblem"><ClipboardCheck size={24} /></div><div><strong>{facultyName}</strong></div><button className="icon-button mobile-only" onClick={() => setOpen(false)}><X size={20} /></button></div>
      <nav>{visible.map(({ to, key, icon: Icon }) => <NavLink key={to} to={to} end={to === "/"} onClick={() => setOpen(false)} title={t(key)}><Icon size={19} /><span>{t(key)}</span><ChevronLeft className="nav-chevron" size={14} /></NavLink>)}</nav>
      <button className="collapse-button desktop-only" onClick={() => setCollapsed(x => !x)}><PanelLeftClose size={18} /><span>{collapsed ? "Expand" : "Collapse"}</span></button>
    </aside>
    <main className="app-main">
      <header className="topbar"><button className="icon-button mobile-only" onClick={() => setOpen(true)}><Menu size={21} /></button><div className="topbar-spacer" /><button className="language-button" onClick={toggleLanguage}><Languages size={17} />{t("language")}</button><NavLink className="notification-button" to="/notifications"><Bell size={19} />{liveNotification && <span />}</NavLink><div className="profile-chip"><div className="avatar">{(language === "ar" ? user?.fullNameArabic : user?.fullNameEnglish)?.trim().charAt(0) || "U"}</div><div><strong>{pick(user?.fullNameArabic || "", user?.fullNameEnglish || "")}</strong><span>{user?.roles.join(" · ")}</span></div></div><button className="icon-button" title={t("signOut")} onClick={signOut}><LogOut size={19} /></button></header>
      <div className="page-container"><Outlet /></div>
    </main>
    {open && <button className="sidebar-scrim mobile-only" aria-label="Close menu" onClick={() => setOpen(false)} />}
    {toast && <button className="live-toast" onClick={() => { setToast(null); navigate("/notifications"); }}><Bell size={18} /><span><strong>{toast.title}</strong><small>{toast.body}</small></span></button>}
  </div>;
}
