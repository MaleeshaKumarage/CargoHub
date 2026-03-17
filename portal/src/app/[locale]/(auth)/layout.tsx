export default function AuthLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div data-slot="auth-layout" className="flex min-h-screen items-center justify-center bg-muted/30 p-4">
      {children}
    </div>
  );
}
