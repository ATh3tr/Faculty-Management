import { useEffect, useState } from "react";
import { Megaphone, Search, Send } from "lucide-react";
import { ActionForm, Card, FormField, PageHeader } from "../components/ui";
import { api, json } from "../lib/api";
import { useLanguage } from "../lib/i18n";
import { arabicTextPattern, asciiDigitsPattern, englishTextPattern, validateBilingual } from "../lib/validation";
import type { AcademicYear, Division, Student } from "../types";

type StudentSearchMode = "universityNumber" | "name";

export function AnnouncementsPage() {
  const { language, pick } = useLanguage();
  const [years, setYears] = useState<AcademicYear[]>([]);
  const [divisions, setDivisions] = useState<Division[]>([]);
  const [students, setStudents] = useState<Student[]>([]);
  const [studentSearchMode, setStudentSearchMode] = useState<StudentSearchMode>("universityNumber");
  const [studentSearch, setStudentSearch] = useState("");
  const [studentLoading, setStudentLoading] = useState(false);
  const [form, setForm] = useState({
    titleArabic: "", titleEnglish: "", bodyArabic: "", bodyEnglish: "",
    audience: "Everyone", studyYear: "", divisionId: "", studentUserId: ""
  });

  useEffect(() => { api<AcademicYear[]>("/api/catalog/academic-years").then(setYears); }, []);
  useEffect(() => {
    const current = years.find(x => x.isCurrent);
    if (current) api<Division[]>(`/api/catalog/divisions?academicYearId=${current.id}`).then(setDivisions);
  }, [years]);
  useEffect(() => {
    if (form.audience !== "Student" || !studentSearch.trim()) {
      setStudents([]);
      return;
    }
    let cancelled = false;
    const timer = window.setTimeout(async () => {
      setStudentLoading(true);
      try {
        const query = new URLSearchParams({ search: studentSearch.trim(), searchBy: studentSearchMode, language });
        const matches = await api<Student[]>(`/api/catalog/students?${query}`);
        if (!cancelled) setStudents(matches);
      } catch {
        if (!cancelled) setStudents([]);
      } finally {
        if (!cancelled) setStudentLoading(false);
      }
    }, 300);
    return () => { cancelled = true; window.clearTimeout(timer); };
  }, [form.audience, studentSearch, studentSearchMode, language]);

  const set = (key: string, value: string) => setForm(current => ({ ...current, [key]: value }));
  const changeSearchMode = (mode: StudentSearchMode) => {
    setStudentSearchMode(mode); setStudentSearch(""); setStudents([]); set("studentUserId", "");
  };
  const publish = async () => {
    validateBilingual(form.titleArabic, form.titleEnglish, "title");
    validateBilingual(form.bodyArabic, form.bodyEnglish, "body");
    await api("/api/announcements", {
      method: "POST",
      ...json({ ...form, studyYear: form.studyYear ? Number(form.studyYear) : null,
        divisionId: form.divisionId || null, studentUserId: form.studentUserId || null })
    });
    setForm(current => ({ ...current, titleArabic: "", titleEnglish: "", bodyArabic: "", bodyEnglish: "" }));
  };

  return <>
    <PageHeader eyebrow={pick("اتصال مباشر", "Direct communication")} title={pick("نشر إعلان", "Publish announcement")} description={pick("اكتب الرسالة باللغتين وحدد الجمهور المستهدف بدقة.", "Write the message in both languages and choose its exact audience.")} />
    <div className="two-panel">
      <Card title={pick("محتوى الإعلان", "Announcement content")}>
        <ActionForm onSubmit={publish} submitLabel={pick("نشر الآن", "Publish now")}>
          <div className="two-columns">
            <FormField label={pick("العنوان بالعربية", "Arabic title")}><input dir="rtl" pattern={arabicTextPattern} title={pick("استخدم الأحرف العربية فقط", "Use Arabic characters only")} required value={form.titleArabic} onChange={e => set("titleArabic", e.target.value)} /></FormField>
            <FormField label={pick("العنوان بالإنكليزية", "English title")}><input dir="ltr" pattern={englishTextPattern} title={pick("استخدم الأحرف الإنكليزية فقط", "Use English characters only")} required value={form.titleEnglish} onChange={e => set("titleEnglish", e.target.value)} /></FormField>
          </div>
          <div className="two-columns">
            <FormField label={pick("النص بالعربية", "Arabic body")}><textarea dir="rtl" rows={6} required value={form.bodyArabic} onChange={e => set("bodyArabic", e.target.value)} /></FormField>
            <FormField label={pick("النص بالإنكليزية", "English body")}><textarea dir="ltr" rows={6} required value={form.bodyEnglish} onChange={e => set("bodyEnglish", e.target.value)} /></FormField>
          </div>
          <FormField label={pick("الجمهور", "Audience")}><select value={form.audience} onChange={e => set("audience", e.target.value)}><option value="Everyone">{pick("الجميع", "Everyone")}</option><option value="StudyYear">{pick("سنة دراسية", "Study year")}</option><option value="Division">{pick("شعبة", "Division")}</option><option value="Student">{pick("طالب محدد", "Specific student")}</option><option value="Staff">{pick("الكادر", "Staff")}</option></select></FormField>
          {form.audience === "StudyYear" && <FormField label={pick("السنة", "Study year")}><select required value={form.studyYear} onChange={e => set("studyYear", e.target.value)}><option value="">—</option>{[1,2,3,4,5].map(x => <option key={x} value={x}>{x}</option>)}</select></FormField>}
          {form.audience === "Division" && <FormField label={pick("الشعبة", "Division")}><select required value={form.divisionId} onChange={e => set("divisionId", e.target.value)}><option value="">—</option>{divisions.map(x => <option key={x.id} value={x.id}>{pick("سنة", "Year")} {x.studyYear} · {pick("شعبة", "Division")} {x.number}</option>)}</select></FormField>}
          {form.audience === "Student" && <div className="student-target-search">
            <FormField label={pick("طريقة البحث", "Search by")}><select value={studentSearchMode} onChange={e => changeSearchMode(e.target.value as StudentSearchMode)}><option value="universityNumber">{pick("الرقم الجامعي", "University number")}</option><option value="name">{pick("اسم الطالب", "Student name")}</option></select></FormField>
            <FormField label={pick("البحث", "Search")} hint={studentLoading ? pick("جارٍ البحث...", "Searching...") : pick("اكتب جزءاً من القيمة لعرض النتائج", "Type part of the value to see matches")}><div className="search-input"><Search size={17} /><input required pattern={studentSearchMode === "universityNumber" ? asciiDigitsPattern : undefined} inputMode={studentSearchMode === "universityNumber" ? "numeric" : "text"} value={studentSearch} onChange={e => { setStudentSearch(e.target.value); set("studentUserId", ""); }} /></div></FormField>
            <FormField label={`${pick("النتائج", "Matches")} (${students.length})`}><select required disabled={!students.length} value={form.studentUserId} onChange={e => set("studentUserId", e.target.value)}><option value="">{students.length ? pick("اختر الطالب", "Choose student") : pick("لا توجد نتائج", "No matches")}</option>{students.map(x => <option key={x.userId} value={x.userId}>{x.universityNumber} · {pick(x.nameArabic, x.nameEnglish)}</option>)}</select></FormField>
          </div>}
        </ActionForm>
      </Card>
      <Card className="announcement-preview" title={pick("معاينة", "Preview")}><div className="preview-device"><div className="preview-appbar"><Megaphone size={17} /><span>FacultyFlow</span></div><div className="preview-message"><div className="avatar accent"><Send size={16} /></div><div><strong>{form.titleArabic || form.titleEnglish || pick("عنوان الإعلان", "Announcement title")}</strong><p>{form.bodyArabic || form.bodyEnglish || pick("سيظهر نص الإعلان هنا أثناء الكتابة.", "Your announcement body will appear here as you type.")}</p><small>{pick("الآن", "Just now")}</small></div></div></div></Card>
    </div>
  </>;
}
