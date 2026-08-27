import { describe, expect, it } from "vitest";
import { classNames, shortTime } from "./format";

describe("format helpers", () => {
  it("formats API time values for schedule cards", () => {
    expect(shortTime("09:00:00")).toBe("09:00");
  });

  it("combines only active class names", () => {
    expect(classNames("card", false, null, "active", undefined)).toBe("card active");
  });
});
