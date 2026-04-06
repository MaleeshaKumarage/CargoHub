import { describe, it, expect } from "vitest";
import { render, screen } from "@/test/test-utils";
import {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardAction,
  CardContent,
  CardFooter,
} from "./card";

describe("Card", () => {
  it("renders CardAction and CardFooter with data slots", () => {
    render(
      <Card>
        <CardHeader>
          <CardTitle>Title</CardTitle>
          <CardDescription>Desc</CardDescription>
          <CardAction>
            <button type="button">Action</button>
          </CardAction>
        </CardHeader>
        <CardContent>Body</CardContent>
        <CardFooter>Foot</CardFooter>
      </Card>,
    );
    expect(screen.getByText("Title")).toBeInTheDocument();
    expect(document.querySelector('[data-slot="card-action"]')).toBeTruthy();
    expect(document.querySelector('[data-slot="card-footer"]')).toBeTruthy();
    expect(screen.getByRole("button", { name: "Action" })).toBeInTheDocument();
  });
});
