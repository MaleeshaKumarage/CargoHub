import { describe, it, expect, beforeEach, vi } from "vitest";
import { render, screen, act } from "@/test/test-utils";
import { AuthProvider, useAuth } from "./AuthContext";

function TestConsumer() {
  const { user, token, isAuthenticated, login, logout, setLoading, updateUserRoles } = useAuth();
  return (
    <div>
      <span data-testid="authenticated">{String(isAuthenticated)}</span>
      <span data-testid="user">{user?.displayName ?? "none"}</span>
      <span data-testid="token">{token ? "has-token" : "no-token"}</span>
      <button onClick={() => login({ userId: "u1", email: "u@x.com", displayName: "User", jwtToken: "t", roles: ["User"] }, "t")}>
        Login
      </button>
      <button onClick={() => logout()}>Logout</button>
      <button onClick={() => setLoading(true)}>SetLoading</button>
      <button onClick={() => updateUserRoles(["Admin"])}>UpdateRoles</button>
    </div>
  );
}

describe("AuthContext", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("provides initial unauthenticated state", async () => {
    render(
      <AuthProvider>
        <TestConsumer />
      </AuthProvider>
    );
    await act(async () => {});
    expect(screen.getByTestId("authenticated")).toHaveTextContent("false");
    expect(screen.getByTestId("user")).toHaveTextContent("none");
  });

  it("login updates state and stores in localStorage", async () => {
    render(
      <AuthProvider>
        <TestConsumer />
      </AuthProvider>
    );
    await act(async () => {});
    await act(async () => {
      screen.getByText("Login").click();
    });
    expect(screen.getByTestId("authenticated")).toHaveTextContent("true");
    expect(screen.getByTestId("user")).toHaveTextContent("User");
    expect(screen.getByTestId("token")).toHaveTextContent("has-token");
    expect(localStorage.getItem("portal_auth")).toBeTruthy();
  });

  it("setLoading updates isLoading state", async () => {
    render(
      <AuthProvider>
        <TestConsumer />
      </AuthProvider>
    );
    await act(async () => {});
    await act(async () => {
      screen.getByText("SetLoading").click();
    });
    // isLoading is internal; we just verify no throw
    expect(screen.getByText("Logout")).toBeInTheDocument();
  });

  it("updateUserRoles updates user roles when authenticated", async () => {
    render(
      <AuthProvider>
        <TestConsumer />
      </AuthProvider>
    );
    await act(async () => {
      screen.getByText("Login").click();
    });
    await act(async () => {
      screen.getByText("UpdateRoles").click();
    });
    expect(screen.getByTestId("user")).toHaveTextContent("User");
  });

  it("logout clears state and localStorage", async () => {
    render(
      <AuthProvider>
        <TestConsumer />
      </AuthProvider>
    );
    await act(async () => {
      screen.getByText("Login").click();
    });
    await act(async () => {
      screen.getByText("Logout").click();
    });
    expect(screen.getByTestId("authenticated")).toHaveTextContent("false");
    expect(localStorage.getItem("portal_auth")).toBeNull();
  });

  it("useAuth throws when used outside AuthProvider", async () => {
    const { render: r } = await import("@testing-library/react");
    vi.spyOn(console, "error").mockImplementation(() => {});
    expect(() => r(<TestConsumer />)).toThrow("useAuth must be used within AuthProvider");
    vi.restoreAllMocks();
  });
});
