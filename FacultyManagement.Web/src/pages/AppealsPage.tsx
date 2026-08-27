import { useEffect, useState } from "react";
import { Check, Clock3, MessageSquareText, X } from "lucide-react";
import { useAuth } from "../auth/AuthContext";
import { Badge, Card, DataTable, EmptyState, LoadingBlock, PageHeader } from "../components/ui";
import { api, json } from "../lib/api";
import { formatDateTime } from "../lib/format";
import { useLanguage } from "../lib/i18n";
import type { Appeal } from "../types";

const appealTone = (status: string) => status === "Accepted" ? "success" : status === "Rejected" ? "danger" : status === "ProfessorReviewed" ? "info" : "warning";

export function AppealsPage() {
  const { hasRole } = useAuth(); const { language, pick } = useLanguage(); const [items, setItems] = useState<Appeal[]>([]); const [loading, setLoading] = useState(true); const [comment, setComment] = useState<Record<string, string>>({});
  const load = async () => { setLoading(true); try { setItems(await api("/api/catalog/appeals")); } finally { setLoading(false); } };
  useEffect(() => { load(); }, []);
  const review = async (id: string) => { await api(`/api/appeals/${id}/professor-review`, { method: "PUT", ...json({ comment: comment[id] || "Reviewed" }) }); await load(); };
  const decide = async (id: string, accept: boolean) => { await api(`/api/appeals/${id}/decision`, { method: "PUT", ...json({ accept, comment: comment[id] || (accept ? "Accepted" : "Rejected") }) }); await load(); };
  return <><PageHeader eyebrow={pick("مسار واضح وموثق", "A clear, audited workflow")} title={pick("اعتراضات العلامات", "Mark appeals")} description={pick("يتابع الطالب اعتراضه، ويراجعه الأستاذ، ثم يصدر مسؤول الامتحانات القرار.", "Students track submissions, professors review, and exams staff make the final decision.")} />{loading ? <LoadingBlock /> : items.length ? <Card><DataTable headers={[pick("الطالب", "Student"), pick("المقرر", "Course"), pick("السبب", "Reason"), pick("الحالة", "Status"), pick("التاريخ", "Submitted"), pick("الإجراء", "Action")]}>{items.map(item => <tr key={item.id}><td><strong>{item.studentName}</strong></td><td><Badge tone="info">{item.courseCode}</Badge></td><td><span className="cell-wrap">{item.reason}</span>{item.professorComment && <small className="table-note"><MessageSquareText size={13} />{item.professorComment}</small>}{item.decisionComment && <small className="table-note">{item.decisionComment}</small>}</td><td><Badge tone={appealTone(item.status)}>{item.status}</Badge></td><td>{formatDateTime(item.submittedAtUtc, language)}</td><td>{hasRole("Professor") && item.status === "Submitted" ? <div className="table-action"><input placeholder={pick("تعليق المراجعة", "Review comment")} value={comment[item.id] || ""} onChange={e => setComment(x => ({ ...x, [item.id]: e.target.value }))} /><button className="button button-small" onClick={() => review(item.id)}><Clock3 size={15} />{pick("تمت المراجعة", "Review")}</button></div> : hasRole("ExamsOfficer", "Admin") && ["Submitted", "ProfessorReviewed"].includes(item.status) ? <div className="table-action"><input placeholder={pick("تعليق القرار", "Decision comment")} value={comment[item.id] || ""} onChange={e => setComment(x => ({ ...x, [item.id]: e.target.value }))} /><div><button className="icon-button success" title={pick("قبول", "Accept")} onClick={() => decide(item.id, true)}><Check size={17} /></button><button className="icon-button danger" title={pick("رفض", "Reject")} onClick={() => decide(item.id, false)}><X size={17} /></button></div></div> : <span>—</span>}</td></tr>)}</DataTable></Card> : <Card><EmptyState title={pick("لا توجد اعتراضات", "No appeals to show")} /></Card>}</>;
}
