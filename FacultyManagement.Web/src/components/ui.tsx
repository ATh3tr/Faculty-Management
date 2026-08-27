import { AlertCircle, CheckCircle2, LoaderCircle, X } from "lucide-react";
import { type FormEvent, type ReactNode, useState } from "react";
import { ApiError } from "../lib/api";
import { classNames } from "../lib/format";
import { useLanguage } from "../lib/i18n";

export function PageHeader({ eyebrow, title, description, actions }: { eyebrow?: string; title: string; description?: string; actions?: ReactNode }) {
  return <header className="page-header"><div>{eyebrow && <span className="eyebrow">{eyebrow}</span>}<h1>{title}</h1>{description && <p>{description}</p>}</div>{actions && <div className="page-actions">{actions}</div>}</header>;
}

export function Card({ children, className, title, action }: { children: ReactNode; className?: string; title?: string; action?: ReactNode }) {
  return <section className={classNames("card", className)}>{(title || action) && <header className="card-header"><h2>{title}</h2>{action}</header>}{children}</section>;
}

export function Metric({ label, value, detail, tone = "teal" }: { label: string; value: ReactNode; detail?: string; tone?: "teal" | "amber" | "blue" | "rose" }) {
  return <div className={`metric metric-${tone}`}><span>{label}</span><strong>{value}</strong>{detail && <small>{detail}</small>}</div>;
}

export function Badge({ children, tone = "neutral" }: { children: ReactNode; tone?: "neutral" | "success" | "warning" | "danger" | "info" }) {
  return <span className={`badge badge-${tone}`}>{children}</span>;
}

export function EmptyState({ title, description }: { title: string; description?: string }) {
  return <div className="empty-state"><span className="empty-mark">·</span><strong>{title}</strong>{description && <p>{description}</p>}</div>;
}

export function LoadingBlock() { const { t } = useLanguage(); return <div className="loading-block"><LoaderCircle className="spin" size={22} /> {t("loading")}</div>; }

export function FormField({ label, hint, children }: { label: string; hint?: string; children: ReactNode }) {
  return <label className="form-field"><span>{label}</span>{children}{hint && <small>{hint}</small>}</label>;
}

export function ActionForm({ children, onSubmit, submitLabel, className }: { children: ReactNode; onSubmit: () => Promise<void>; submitLabel: string; className?: string }) {
  const [busy, setBusy] = useState(false); const [message, setMessage] = useState<{ text: string; error: boolean } | null>(null);
  const submit = async (event: FormEvent) => { event.preventDefault(); setBusy(true); setMessage(null); try { await onSubmit(); setMessage({ text: "Saved successfully", error: false }); } catch (error) { setMessage({ text: error instanceof ApiError || error instanceof Error ? error.message : "Request failed", error: true }); } finally { setBusy(false); } };
  return <form className={classNames("action-form", className)} onSubmit={submit}>{children}<div className="form-footer"><button className="button button-primary" disabled={busy}>{busy ? <LoaderCircle className="spin" size={17} /> : <CheckCircle2 size={17} />}{submitLabel}</button>{message && <span className={message.error ? "form-error" : "form-success"}>{message.error ? <AlertCircle size={16} /> : <CheckCircle2 size={16} />}{message.text}<button type="button" aria-label="Dismiss" onClick={() => setMessage(null)}><X size={14} /></button></span>}</div></form>;
}

export function Tabs({ tabs, active, onChange }: { tabs: Array<{ id: string; label: string }>; active: string; onChange: (id: string) => void }) {
  return <div className="tabs" role="tablist">{tabs.map(tab => <button key={tab.id} type="button" className={tab.id === active ? "active" : ""} onClick={() => onChange(tab.id)}>{tab.label}</button>)}</div>;
}

export function DataTable({ headers, children }: { headers: string[]; children: ReactNode }) {
  return <div className="table-wrap"><table><thead><tr>{headers.map(header => <th key={header}>{header}</th>)}</tr></thead><tbody>{children}</tbody></table></div>;
}
