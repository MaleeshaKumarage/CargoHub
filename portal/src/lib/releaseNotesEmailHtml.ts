/** HTML-escape plain text and wrap in pre-wrap (aligned with API ReleaseNotesEmailBodyFormatter). */
export function escapeHtmlForEmail(plain: string): string {
  return plain
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#39;");
}

export function releaseNotesEmailBodyHtml(plain: string): string {
  const encoded = escapeHtmlForEmail(plain ?? "");
  return `<div style="white-space: pre-wrap; font-family: sans-serif;">${encoded}</div>`;
}
