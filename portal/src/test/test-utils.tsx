import { ReactElement, ReactNode } from "react";
import { render, RenderOptions } from "@testing-library/react";

// Default wrapper can add providers (e.g. theme) if needed later.
function AllTheProviders({ children }: { children: ReactNode }) {
  return <>{children}</>;
}

function customRender(ui: ReactElement, options?: Omit<RenderOptions, "wrapper">) {
  return render(ui, {
    wrapper: AllTheProviders,
    ...options,
  });
}

export * from "@testing-library/react";
export { customRender as render };
