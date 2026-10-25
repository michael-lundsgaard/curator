import type { ComponentProps } from "react";
import { buttonClass, type ButtonStyle } from "./buttonClass";

// A type="button" unless it says otherwise, so a button in a form doesn't submit it by accident. `className` adds
// layout (self-center), not looks: two classes for the same property don't override each other in Tailwind.
export function Button({
  variant,
  size,
  type = "button",
  className,
  ...button
}: ButtonStyle & ComponentProps<"button">) {
  return (
    <button
      type={type}
      className={`${buttonClass({ variant, size })} ${className ?? ""}`}
      {...button}
    />
  );
}
