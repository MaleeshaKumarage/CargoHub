import { describe, expect, it } from "vitest";
import { escapeHtmlForEmail, releaseNotesEmailBodyHtml } from "./releaseNotesEmailHtml";

describe("releaseNotesEmailHtml", () => {
  it("escapeHtmlForEmail encodes special characters", () => {
    expect(escapeHtmlForEmail(`a <b> & c`)).toBe("a &lt;b&gt; &amp; c");
  });

  it("releaseNotesEmailBodyHtml wraps pre-wrap and preserves newlines", () => {
    const html = releaseNotesEmailBodyHtml("x\n\n  y");
    expect(html).toContain("white-space: pre-wrap");
    expect(html).toContain("x\n\n  y");
  });

  it("releaseNotesEmailBodyHtml treats null as empty body", () => {
    expect(releaseNotesEmailBodyHtml(null as unknown as string)).toBe(
      '<div style="white-space: pre-wrap; font-family: sans-serif;"></div>',
    );
  });
});
