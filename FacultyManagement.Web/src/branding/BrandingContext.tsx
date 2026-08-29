import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { api } from "../lib/api";
import { useLanguage } from "../lib/i18n";

type BrandingSettings = {
  facultyNameArabic: string;
  facultyNameEnglish: string;
};

type BrandingContextValue = BrandingSettings & {
  facultyName: string;
  refreshBranding: () => Promise<void>;
};

const defaults: BrandingSettings = {
  facultyNameArabic: "كلية الهندسة المعلوماتية",
  facultyNameEnglish: "Faculty of Informatics Engineering"
};

const BrandingContext = createContext<BrandingContextValue | null>(null);

export function BrandingProvider({ children }: { children: ReactNode }) {
  const { language } = useLanguage();
  const [branding, setBranding] = useState(defaults);

  const refreshBranding = useCallback(async () => {
    try {
      const result = await api<BrandingSettings>("/api/settings/branding");
      setBranding({
        facultyNameArabic: result.facultyNameArabic.trim() || defaults.facultyNameArabic,
        facultyNameEnglish: result.facultyNameEnglish.trim() || defaults.facultyNameEnglish
      });
    } catch {
      setBranding(current => current);
    }
  }, []);

  useEffect(() => { void refreshBranding(); }, [refreshBranding]);

  const facultyName = language === "ar" ? branding.facultyNameArabic : branding.facultyNameEnglish;
  useEffect(() => { document.title = facultyName; }, [facultyName]);

  const value = useMemo(
    () => ({ ...branding, facultyName, refreshBranding }),
    [branding, facultyName, refreshBranding]
  );

  return <BrandingContext.Provider value={value}>{children}</BrandingContext.Provider>;
}

export function useBranding() {
  const value = useContext(BrandingContext);
  if (!value) throw new Error("BrandingProvider is missing");
  return value;
}
