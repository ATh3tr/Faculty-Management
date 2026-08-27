export const formatDate = (value: string, language = "en") => new Intl.DateTimeFormat(language === "ar" ? "ar-SY" : "en-GB", { dateStyle: "medium" }).format(new Date(`${value}T12:00:00`));
export const formatDateTime = (value: string, language = "en") => new Intl.DateTimeFormat(language === "ar" ? "ar-SY" : "en-GB", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
export const shortTime = (value: string) => value.slice(0, 5);
export const isoToday = () => new Date().toISOString().slice(0, 10);
export const plusDays = (days: number) => { const d = new Date(); d.setDate(d.getDate() + days); return d.toISOString().slice(0, 10); };
export const classNames = (...values: Array<string | false | null | undefined>) => values.filter(Boolean).join(" ");
