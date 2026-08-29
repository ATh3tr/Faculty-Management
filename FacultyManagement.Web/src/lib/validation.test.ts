import { describe, expect, it } from "vitest";
import { validateBilingual } from "./validation";

describe("bilingual validation", () => {
  it("accepts text in the matching scripts", () => {
    expect(() => validateBilingual("هندسة البرمجيات", "Software Engineering", "name")).not.toThrow();
  });

  it("rejects English in Arabic fields and Arabic in English fields", () => {
    expect(() => validateBilingual("Software", "Engineering", "name")).toThrow(/Arabic name/);
    expect(() => validateBilingual("هندسة", "هندسة", "name")).toThrow(/English name/);
  });
});
