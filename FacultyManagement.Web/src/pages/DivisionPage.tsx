import { useEffect, useState } from "react";
import { CheckCircle2, Users } from "lucide-react";
import { Card, LoadingBlock, PageHeader } from "../components/ui";
import { api } from "../lib/api";
import { useLanguage } from "../lib/i18n";
import type { DivisionAssignment } from "../types";

export function DivisionPage() {
  const { pick } = useLanguage(); const [assignment, setAssignment] = useState<DivisionAssignment | null>(null); const [loading, setLoading] = useState(true); const [busy, setBusy] = useState(false); const [error, setError] = useState("");
  useEffect(() => { api<DivisionAssignment | null>("/api/divisions/mine").then(setAssignment).catch(e => setError(e instanceof Error ? e.message : "Failed")).finally(() => setLoading(false)); }, []);
  const register = async () => { setBusy(true); setError(""); try { setAssignment(await api("/api/divisions/register", { method: "POST" })); } catch (e) { setError(e instanceof Error ? e.message : "Failed"); } finally { setBusy(false); } };
  return <><PageHeader eyebrow={pick("المحاضرات العملية", "Practical sessions")} title={pick("تسجيل الشعبة", "Division registration")} description={pick("يختار النظام الشعبة الأقل ازدحاماً ويحافظ على الحد الأقصى المحدد.", "The system assigns the least-filled division while respecting its capacity.")} /><div className="narrow-content"><Card>{loading ? <LoadingBlock /> : assignment ? <div className="assignment-result"><div className="success-icon"><CheckCircle2 size={30} /></div><span>{pick("تم تعيينك إلى", "You are assigned to")}</span><strong>{pick("الشعبة", "Division")} {assignment.divisionNumber}</strong><p>{pick("السنة", "Year")} {assignment.studyYear} · {assignment.memberCount}/{assignment.capacity} {pick("طالب", "students")}</p></div> : <div className="division-callout"><div className="large-icon"><Users size={34} /></div><h2>{pick("جاهز للانضمام إلى شعبة؟", "Ready to join a division?")}</h2><p>{pick("سيبقى تعيينك محفوظاً وسيظهر تلقائياً عند عودتك إلى هذه الصفحة. الطلاب المعيدون لا يحتاجون إلى شعبة في هذا الإصدار.", "Your assignment is saved and will appear automatically when you return. Repeating students do not receive a division in this version.")}</p><button className="button button-primary" disabled={busy} onClick={register}>{busy ? pick("جارٍ التعيين...", "Assigning...") : pick("سجّلني الآن", "Assign me now")}</button>{error && <div className="inline-alert error">{error}</div>}</div>}</Card></div></>;
}
