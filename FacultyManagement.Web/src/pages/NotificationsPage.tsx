import { useEffect, useState } from "react";
import { Bell, CheckCheck } from "lucide-react";
import { Badge, Card, EmptyState, LoadingBlock, PageHeader } from "../components/ui";
import { api } from "../lib/api";
import { formatDateTime } from "../lib/format";
import { useLanguage } from "../lib/i18n";
import type { Notification } from "../types";

export function NotificationsPage() {
  const { language, pick } = useLanguage(); const [items, setItems] = useState<Notification[]>([]); const [loading, setLoading] = useState(true);
  const load = async () => { setLoading(true); try { setItems(await api(`/api/notifications?language=${language}`)); } finally { setLoading(false); } };
  useEffect(() => { load(); }, [language]);
  const markRead = async (item: Notification) => { if (!item.isRead) { await api(`/api/notifications/${item.id}/read`, { method: "PUT" }); setItems(xs => xs.map(x => x.id === item.id ? { ...x, isRead: true } : x)); } };
  return <><PageHeader eyebrow={pick("ابقَ على اطلاع", "Stay informed")} title={pick("مركز الإشعارات", "Notification center")} description={pick("تحديثات العلامات والجداول والإعلانات في مكان واحد.", "Marks, schedule changes, and announcements in one place.")} />{loading ? <LoadingBlock /> : <Card>{items.length ? <div className="notification-list">{items.map(item => <button key={item.id} className={!item.isRead ? "unread" : ""} onClick={() => markRead(item)}><div className="notification-icon"><Bell size={18} /></div><div><div className="notification-title"><strong>{item.title}</strong><Badge tone={item.isRead ? "neutral" : "info"}>{item.isRead ? pick("مقروء", "Read") : pick("جديد", "New")}</Badge></div><p>{item.body}</p><small>{formatDateTime(item.createdAtUtc, language)} · {item.type}</small></div>{item.isRead && <CheckCheck size={18} />}</button>)}</div> : <EmptyState title={pick("لا توجد إشعارات", "No notifications yet")} />}</Card>}</>;
}
