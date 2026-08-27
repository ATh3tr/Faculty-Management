import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";

type Language = "ar" | "en";
type Dictionary = Record<string, { ar: string; en: string }>;
const words: Dictionary = {
  brand: { ar: "كلية الهندسة المعلوماتية", en: "Faculty of Informatics Engineering" },
  overview: { ar: "نظرة عامة", en: "Overview" }, schedule: { ar: "الجدول", en: "Schedule" },
  marks: { ar: "العلامات", en: "Marks" }, appeals: { ar: "الاعتراضات", en: "Appeals" },
  notifications: { ar: "الإشعارات", en: "Notifications" }, division: { ar: "الشعبة", en: "Division" },
  announcements: { ar: "الإعلانات", en: "Announcements" }, academic: { ar: "الشؤون الأكاديمية", en: "Academic" },
  people: { ar: "الحسابات والطلاب", en: "People" }, rooms: { ar: "القاعات والمخابر", en: "Rooms & labs" },
  timetable: { ar: "توليد الجدول", en: "Timetable generator" }, settings: { ar: "الإعدادات والسجل", en: "Settings & audit" },
  signOut: { ar: "تسجيل الخروج", en: "Sign out" }, language: { ar: "English", en: "العربية" },
  loading: { ar: "جارٍ التحميل...", en: "Loading..." }, save: { ar: "حفظ", en: "Save" }, create: { ar: "إنشاء", en: "Create" },
  cancel: { ar: "إلغاء", en: "Cancel" }, noData: { ar: "لا توجد بيانات بعد", en: "No data yet" },
  welcome: { ar: "مرحباً", en: "Welcome" }, currentWeek: { ar: "هذا الأسبوع", en: "This week" }
};

type LanguageContextValue = { language: Language; setLanguage: (value: Language) => void; toggleLanguage: () => void; t: (key: string) => string; pick: (ar: string, en: string) => string };
const LanguageContext = createContext<LanguageContextValue | null>(null);

export function LanguageProvider({ children }: { children: ReactNode }) {
  const [language, setLanguage] = useState<Language>(() => (localStorage.getItem("faculty_language") as Language) || "ar");
  useEffect(() => {
    localStorage.setItem("faculty_language", language);
    document.documentElement.lang = language;
    document.documentElement.dir = language === "ar" ? "rtl" : "ltr";
  }, [language]);
  const value = useMemo(() => ({ language, setLanguage, toggleLanguage: () => setLanguage(x => x === "ar" ? "en" : "ar"), t: (key: string) => words[key]?.[language] || key, pick: (ar: string, en: string) => language === "ar" ? ar : en }), [language]);
  return <LanguageContext.Provider value={value}>{children}</LanguageContext.Provider>;
}

export function useLanguage() { const value = useContext(LanguageContext); if (!value) throw new Error("LanguageProvider is missing"); return value; }
